package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.execution.Executor
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.console.LanguageConsoleBuilder
import com.intellij.execution.console.LanguageConsoleView
import com.intellij.execution.console.ProcessBackedConsoleExecuteActionHandler
import com.intellij.execution.process.OSProcessHandler
import com.intellij.execution.process.OSProcessUtil
import com.intellij.execution.runners.AbstractConsoleRunnerWithHistory
import com.intellij.execution.ui.ConsoleViewContentType
import com.intellij.execution.ui.RunContentDescriptor
import com.intellij.execution.ui.RunContentManager
import com.intellij.icons.AllIcons
import com.intellij.ide.DataManager
import com.intellij.notification.Notification
import com.intellij.notification.NotificationListener
import com.intellij.notification.NotificationType
import com.intellij.notification.Notifications
import com.intellij.openapi.actionSystem.*
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.editor.colors.EditorColors
import com.intellij.openapi.extensions.Extensions
import com.intellij.openapi.options.ShowSettingsUtil
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.UserDataHolderBase
import com.intellij.openapi.util.text.StringUtil
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.openapi.wm.ToolWindowManager
import com.intellij.project.isDirectoryBased
import com.intellij.ui.Gray
import com.intellij.ui.JBColor
import com.intellij.util.containers.ContainerUtil
import com.intellij.util.ui.UIUtil
import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.attach.LocalAttachHost
import com.intellij.xdebugger.attach.XAttachDebuggerProvider
import com.jetbrains.rd.platform.util.application
import com.jetbrains.rdclient.util.idea.pumpMessages
import com.jetbrains.rider.debugger.DotNetDebugProcess
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpScriptLanguage
import com.jetbrains.rider.plugins.fsharp.FSharpIcons
import com.jetbrains.rider.plugins.fsharp.RdFsiRuntime
import com.jetbrains.rider.plugins.fsharp.RdFsiSessionInfo
import com.jetbrains.rider.runtime.RiderDotNetActiveRuntimeHost
import com.jetbrains.rider.runtime.mono.MonoRuntime
import com.jetbrains.rider.util.idea.getComponent
import org.jetbrains.concurrency.AsyncPromise
import org.jetbrains.concurrency.Promise
import org.jetbrains.concurrency.resolvedPromise
import java.io.File
import java.time.Duration
import javax.swing.BorderFactory
import javax.swing.event.HyperlinkEvent
import kotlin.properties.Delegates

class FsiConsoleRunner(sessionInfo: RdFsiSessionInfo, val fsiHost: FsiHost, debug: Boolean) :
  AbstractConsoleRunnerWithHistory<LanguageConsoleView>(fsiHost.project, fsiTitle, null) {

  companion object {
    const val fsiTitle = "F# Interactive"
    private const val debugNotConfiguredTitle = "The session is not configured for debugging"
    private const val debugNotConfiguredDescription = "F# Interactive should be relaunched."
    private const val notificationLinks =
      "<br/>${RelaunchFsiWithDebugAction.link}&nbsp;&nbsp;&nbsp;&nbsp;${ShowFsiSettingsAction.link}"

    private val debugFsiArgs = listOf("--optimize-", "--debug+")

    private const val waitForDebugSessionTimeout = 15000L
  }

  val optimizeForDebug = debug || sessionInfo.fixArgsForAttach
  private val fsiArgs = sessionInfo.args + (if (optimizeForDebug) debugFsiArgs else emptyList())
  private var cmdLine = GeneralCommandLine()
    .withExePath(sessionInfo.fsiPath)
    .withParameters(fsiArgs)
  private val inputSeparatorGutterContentProvider = InputSeparatorGutterContentProvider(true)

  private val fsiInputOutputProcessor = FsiInputOutputProcessor(this)

  init {
    val runtimeHost = project.getComponent<RiderDotNetActiveRuntimeHost>()
    if (sessionInfo.runtime == RdFsiRuntime.Core) {
      runtimeHost.dotNetCoreRuntime.value?.patchRunCommandLine(cmdLine, listOf())
    } else {
      val runtime = runtimeHost.getCurrentClassicNetRuntime(false).runtime
      if (runtime != null && runtime is MonoRuntime && sessionInfo.fsiPath.endsWith(".exe", true)) {
        runtime.patchRunCommandLine(cmdLine, listOf())
      }
    }

    if (project.isDirectoryBased) {
      val projectDir = project.baseDir
      val workingDir = if (projectDir.exists()) projectDir else VfsUtil.getUserHomeDir()
      if (workingDir != null)
        cmdLine.workDirectory = File(workingDir.path)
    }
  }

  private var contentDescriptor by Delegates.notNull<RunContentDescriptor>()
  private var pid by Delegates.notNull<Int>()

  private val isAttached
    get() = getDebugProcessForThisFsi() != null

  private val lockObject = Object()

  val sendActionExecutor = SendToFsiActionExecutor(this)
  val commandHistory = CommandHistory()

  private val listener = object : NotificationListener.Adapter() {
    override fun hyperlinkActivated(notification: Notification, e: HyperlinkEvent) {
      if (!project.isDisposed) {
        val actionEvent =
          AnActionEvent.createFromDataContext(ActionPlaces.UNKNOWN, null, DataManager.getInstance().dataContext)
        when (e.description) {
          RelaunchFsiWithDebugAction.actionName -> RelaunchFsiWithDebugAction(project).actionPerformed(actionEvent)
          ShowFsiSettingsAction.actionName -> ShowFsiSettingsAction(project).actionPerformed(actionEvent)
        }
        notification.expire()

      }
    }
  }

  fun isValid() = !processHandler.isProcessTerminated && !processHandler.isProcessTerminating

  private fun getDebugProcessForThisFsi() = XDebuggerManager.getInstance(project).debugSessions
    .map { it.debugProcess }.filterIsInstance<DotNetDebugProcess>().firstOrNull()

  fun attachToProcess(): Boolean {
    if (!optimizeForDebug) {
      val notification = Notification(
        fsiTitle, debugNotConfiguredTitle, debugNotConfiguredDescription + notificationLinks,
        NotificationType.WARNING, listener
      )
      notification.icon = FSharpIcons.FSharpConsole
      Notifications.Bus.notify(notification, project)
      return false
    }

    val processInfo = OSProcessUtil.getProcessList().firstOrNull { it.pid == pid } ?: return false
    val dataHolder = UserDataHolderBase()
    val debugger = Extensions.getExtensions(XAttachDebuggerProvider.EP).flatMap { provider ->
      provider.getAvailableDebuggers(project, LocalAttachHost.INSTANCE, processInfo, dataHolder)
    }.firstOrNull() ?: return false
    debugger.attachDebugSession(project, LocalAttachHost.INSTANCE, processInfo)
    return true
  }

  private fun sendText(visibleText: String, fsiText: String) {
    UIUtil.invokeLaterIfNeeded {
      inputSeparatorGutterContentProvider.addLineSeparator(consoleView.historyViewer.document.lineCount)

      fsiInputOutputProcessor.printInputText(visibleText, ConsoleViewContentType.USER_INPUT)

      commandHistory.addEntry(CommandHistory.Entry(visibleText, fsiText))

      // show the window without getting focus
      RunContentManager.getInstance(project).selectRunContent(contentDescriptor)
      ToolWindowManager.getInstance(project).getToolWindow(executor.id)?.show(null)

      val stream = processHandler.processInput ?: error("Broken Fsi stream")
      if (!StringUtil.isEmptyOrSpaces(visibleText)) {
        stream.write(fsiText.toByteArray(Charsets.UTF_8))
        stream.flush()
      }
    }
  }

  fun sendText(visibleText: String, fsiText: String, debug: Boolean) {
    attachDebuggerIfNeeded(debug).onSuccess {
      sendText(visibleText, fsiText)
    }.onError {
      sendText(visibleText, fsiText)
      Logger.getInstance(FsiConsoleRunner::class.java).warn(it)
    }
  }

  private fun attachDebuggerIfNeeded(debug: Boolean): Promise<Unit> {
    if (!debug || isAttached) {
      return resolvedPromise()
    }
    synchronized(lockObject) {
      val promise = AsyncPromise<Unit>()
      if (!attachToProcess()) {
        promise.setResult(Unit)
        return promise
      }
      //need to free dispatcher thread to pump messages over protocol for async debug runner
      application.executeOnPooledThread {
        if (!pumpMessages(Duration.ofSeconds(waitForDebugSessionTimeout)) {
            getDebugProcessForThisFsi() != null
          }) {
          promise.setError(IllegalStateException("Failed to get debug process for fsi.exe with pid $pid"))
        } else {
          promise.setResult(Unit)
        }
      }
      return promise
    }
  }

  override fun createProcess(): Process? {
    val process = cmdLine.createProcess()
    pid = OSProcessUtil.getProcessID(process)
    return process
  }

  override fun isAutoFocusContent() = false

  override fun createExecuteActionHandler(): ProcessBackedConsoleExecuteActionHandler {
    return object : ProcessBackedConsoleExecuteActionHandler(processHandler, false) {
      override fun runExecuteAction(consoleView: LanguageConsoleView) {
        val visibleText = consoleView.consoleEditor.document.text
        if (visibleText.isBlank()) return

        val fsiText = "\n$visibleText\n# 1 \"stdin\"\n;;\n"
        sendText(visibleText, fsiText, false)
        consoleView.setInputText("")
      }
    }
  }

  override fun getConsoleIcon() = FSharpIcons.FSharpConsole

  override fun fillToolBarActions(
    toolbarActions: DefaultActionGroup, defaultExecutor: Executor,
    contentDescriptor: RunContentDescriptor
  ): MutableList<AnAction> {
    this.contentDescriptor = contentDescriptor
    val actionList = ContainerUtil.newArrayList<AnAction>(
      createCloseAction(defaultExecutor, contentDescriptor),
      ResetFsiAction(this.fsiHost),
      Separator(),
      CommandHistoryAction(this),
      Separator(),
      OpenSettings(project)
    )
    toolbarActions.addAll(actionList)
    actionList.add(createConsoleExecAction(consoleExecuteActionHandler))
    return actionList
  }

  override fun initAndRun() {
    super.initAndRun()

    setupGutters()
  }

  private fun setupGutters() {
    val historyEditor = consoleView.historyViewer
    historyEditor.settings.isLineMarkerAreaShown = true
    historyEditor.settings.isFoldingOutlineShown = true
    historyEditor.gutterComponentEx.setPaintBackground(true)

    historyEditor.colorsScheme.setColor(EditorColors.GUTTER_BACKGROUND, JBColor(Gray.xF2, Gray.x41))
  }

  override fun createConsoleView(): LanguageConsoleView {
    var createdConsoleView: LanguageConsoleView? = null

    withGenericSandBoxing(createFSharpSandbox("do ()\n\n", false, emptyList()), project) {
      val consoleView = LanguageConsoleBuilder().gutterContentProvider(inputSeparatorGutterContentProvider)
        .build(project, FSharpScriptLanguage)

      val consoleEditorBorder = BorderFactory.createMatteBorder(
        2, 0, 0, 0, consoleView.consoleEditor.colorsScheme.getColor(EditorColors.INDENT_GUIDE_COLOR)
      )
      consoleView.consoleEditor.component.border = consoleEditorBorder

      val historyKeyListener = HistoryKeyListener(fsiHost.project, consoleView.consoleEditor, commandHistory)
      consoleView.consoleEditor.contentComponent.addKeyListener(historyKeyListener)
      commandHistory.listeners.add(historyKeyListener)

      createdConsoleView = consoleView
    }

    return createdConsoleView ?: error("Cannot create fsi")
  }

  override fun createProcessHandler(process: Process): OSProcessHandler {
    val fsiProcessHandler = FsiProcessHandler(fsiInputOutputProcessor, process, cmdLine.commandLineString)

    val sandboxInfoUpdater = FsiSandboxInfoUpdater(fsiHost.project, consoleView.consoleEditor, commandHistory)
    fsiProcessHandler.addSandboxInfoUpdater(sandboxInfoUpdater)

    return fsiProcessHandler
  }

  private class ResetFsiAction(private val host: FsiHost) :
    AnAction("Reset F# Interactive", null, AllIcons.Actions.Restart) {
    override fun actionPerformed(e: AnActionEvent) {
      host.resetFsiConsole(host.consoleRunner?.optimizeForDebug ?: false)
    }
  }

  private class OpenSettings(val project: Project) :
    AnAction("F# Interactive settings", null, AllIcons.General.Settings) {
    override fun actionPerformed(e: AnActionEvent) {
      ShowSettingsUtil.getInstance().showSettingsDialog(project, "Fsi")
    }
  }
}

class RelaunchFsiWithDebugAction(private val currentProject: Project? = null) : AnAction() {
  override fun actionPerformed(e: AnActionEvent) {
    val project = e.project ?: currentProject ?: return
    val fsiHost = project.getComponent<FsiHost>()
    fsiHost.resetFsiConsole(true, true)
  }

  companion object {
    const val actionName = "relaunchWithDebug"
    const val link = "<a href='$actionName'>Relaunch with debug enabled</a>"
  }
}

class ShowFsiSettingsAction(private val currentProject: Project? = null) : AnAction() {
  override fun actionPerformed(e: AnActionEvent) {
    val project = e.project ?: currentProject ?: return
    ShowSettingsUtil.getInstance().showSettingsDialog(project, "F# Interactive")
  }

  companion object {
    const val actionName = "settings"
    const val link = "<a href='$actionName'>Configure...</a>"
  }
}
