package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.execution.Executor
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.notification.Notification
import com.intellij.notification.NotificationType
import com.intellij.notification.Notifications
import com.intellij.openapi.application.EDT
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.openapi.project.guessProjectDir
import com.intellij.openapi.util.NlsSafe
import com.intellij.openapi.util.TextRange
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.project.isDirectoryBased
import com.intellij.psi.PsiFile
import com.intellij.util.execution.ParametersListUtil
import com.jetbrains.rd.platform.util.idea.LifetimedService
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rd.util.reactive.flowInto
import com.jetbrains.rider.plugins.fsharp.FSharpBundle
import com.jetbrains.rider.plugins.fsharp.FSharpIcons
import com.jetbrains.rider.plugins.fsharp.RdFsiRuntime
import com.jetbrains.rider.plugins.fsharp.rdFSharpModel
import com.jetbrains.rider.plugins.fsharp.services.fsi.consoleRunners.FsiConsoleRunnerBase
import com.jetbrains.rider.plugins.fsharp.services.fsi.consoleRunners.FsiDefaultConsoleRunner
import com.jetbrains.rider.plugins.fsharp.services.fsi.consoleRunners.FsiScriptProfileConsoleRunner
import com.jetbrains.rider.plugins.fsharp.services.fsi.runScript.FSharpScriptConfiguration
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.run.configurations.dotNetExe.DotNetExeConfigurationParameters
import com.jetbrains.rider.run.configurations.exe.ProcessExecutionDetails
import com.jetbrains.rider.run.createRunCommandLine
import com.jetbrains.rider.runtime.DotNetExecutable
import com.jetbrains.rider.runtime.DotNetRuntime
import com.jetbrains.rider.runtime.RiderDotNetActiveRuntimeHost
import com.jetbrains.rider.runtime.dotNetCore.DotNetCoreRuntimeType
import com.jetbrains.rider.runtime.mono.MonoRuntime
import com.jetbrains.rider.runtime.mono.MonoRuntimeType
import com.jetbrains.rider.runtime.msNet.MsNetRuntimeType
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.awt.event.FocusEvent
import java.awt.event.FocusListener

suspend fun getFsiRunOptions(
  project: Project,
  configuration: FSharpScriptConfiguration? = null,
  debug: Boolean = false,
): Pair<DotNetExecutable, DotNetRuntime> {
  val fsiHost = FsiHost.getInstance(project)
  val sessionInfo = fsiHost.rdFsiHost.requestNewFsiSessionInfo.startSuspending(Unit)
  val runtimeHost = RiderDotNetActiveRuntimeHost.getInstance(project)

  val exePath = when (sessionInfo.runtime) {
    RdFsiRuntime.Core -> runtimeHost.dotNetCoreRuntime.value?.cliExePath
    RdFsiRuntime.NetFramework -> sessionInfo.fsiPath
    RdFsiRuntime.Mono -> {
      val runtime = runtimeHost.getCurrentClassicNetRuntime(false).runtime as MonoRuntime
      runtime.getMonoExe().path
    }
  }

  val workingDirectory =
    if (project.isDirectoryBased) {
      val projectDir = project.guessProjectDir()
      val workingDir =
        if (projectDir != null && projectDir.exists()) projectDir
        else VfsUtil.getUserHomeDir()
      workingDir?.path ?: ""
    } else ""


  val runtimeType = when (sessionInfo.runtime) {
    RdFsiRuntime.NetFramework -> MsNetRuntimeType
    RdFsiRuntime.Mono -> MonoRuntimeType
    RdFsiRuntime.Core -> DotNetCoreRuntimeType
  }

  val args = ArrayList<String>()
  val sessionArgs = ArrayList(sessionInfo.args)

  if (sessionInfo.runtime != RdFsiRuntime.NetFramework)
    args.add(sessionInfo.fsiPath)
  if (configuration != null)
    args.add("--use:${configuration.scriptFile?.path}")
  if (debug) {
    args.addAll(listOf("--optimize-", "--debug+"))
    sessionArgs.remove("--optimize+")
  }
  args.addAll(sessionArgs)

  val parameters = DotNetExeConfigurationParameters(
    project = project,
    exePath = exePath ?: "",
    programParameters = ParametersListUtil.join(args),
    workingDirectory = workingDirectory,
    envs = configuration?.envs ?: mapOf(),
    isPassParentEnvs = true,
    useExternalConsole = false,
    executeAsIs = false,
    assemblyToDebug = null,
    runtimeType = runtimeType,
    runtimeArguments = ""
  )

  val dotNetExecutable = parameters.toDotNetExecutableSuspending(ProcessExecutionDetails.Default)
  val dotNetRuntime = DotNetRuntime.detectRuntimeForExeOrThrow(
    project,
    runtimeHost,
    dotNetExecutable.exePath,
    dotNetExecutable.runtimeType,
    dotNetExecutable.projectTfm
  )

  val patchedExecutable = dotNetExecutable.copy(usePty = false)
  return Pair(patchedExecutable, dotNetRuntime)
}

@Service(Service.Level.PROJECT)
class FsiHost(val project: Project, val coroutineScope: CoroutineScope) : LifetimedService() {
  companion object {
    fun getInstance(project: Project) = project.service<FsiHost>()
  }

  val rdFsiHost = project.solution.rdFSharpModel.fSharpInteractiveHost

  private val moveCaretOnSendLine = Property(true)
  private val moveCaretOnSendSelection = Property(true)
  val copyRecentToEditor = Property(false)

  init {
    rdFsiHost.moveCaretOnSendLine.flowInto(serviceLifetime, moveCaretOnSendLine)
    rdFsiHost.moveCaretOnSendSelection.flowInto(serviceLifetime, moveCaretOnSendSelection)
    rdFsiHost.copyRecentToEditor.flowInto(serviceLifetime, copyRecentToEditor)
  }

  fun sendToFsi(editor: Editor, file: PsiFile, debug: Boolean) {
    val selectionModel = editor.selectionModel
    val hasSelection = selectionModel.hasSelection()

    val visibleText =
      if (hasSelection) editor.selectionModel.selectedText!!
      else {
        val caretModel = editor.caretModel
        editor.document.getText(TextRange(caretModel.visualLineStart, caretModel.visualLineEnd))
          .substringBeforeLast("\n")
      }

    if (visibleText.isEmpty()) return

    val textLineStart =
      if (hasSelection) editor.document.getLineNumber(editor.selectionModel.selectionStart)
      else editor.caretModel.logicalPosition.line
    val fsiText = "\n" +
      "# silentCd @\"${file.containingDirectory.virtualFile.path}\" ;; \n" +
      (if (debug) "# dbgbreak\n" else "") +
      "# ${textLineStart + 1} @\"${file.virtualFile.path}\" \n" +
      visibleText + "\n" +
      "# 1 \"stdin\"\n;;\n"

    coroutineScope.launch {
      sendToFsi(visibleText, fsiText, textLineStart + 1)
      withContext(Dispatchers.EDT) {
        if (!hasSelection && moveCaretOnSendLine.value)
          editor.caretModel.moveCaretRelatively(0, 1, false, false, true)

        if (hasSelection && moveCaretOnSendSelection.value) {
          editor.caretModel.moveToOffset(selectionModel.selectionEnd)
          editor.caretModel.currentCaret.removeSelection()
        }
      }
    }
  }

  suspend fun sendToFsi(visibleText: String, fsiText: String, lineStart: Int = 1) {
    val fsiRunner = synchronized(lockObject) {
      if (lastFocusedSession?.isValid() == true) lastFocusedSession
      else null
    } ?: tryCreateDefaultConsoleRunner()
    fsiRunner?.sendText(visibleText, fsiText, lineStart)
  }

  fun resetFsiDefaultConsole() {
    coroutineScope.launch {
      synchronized(lockObject) {
        val session = defaultFsiSession
        if (session?.isValid() == true) session.stop()
        defaultFsiSession = null
      }
      tryCreateDefaultConsoleRunner()
    }
  }

  private val lockObject = Object()
  var lastFocusedSession: FsiConsoleRunnerBase? = null
  private var defaultFsiSession: FsiDefaultConsoleRunner? = null

  private suspend fun <T : FsiConsoleRunnerBase> getOrCreateConsoleRunner(factory: () -> T): T {
    val session = factory()
    withContext(Dispatchers.EDT) {
      session.initAndRun()
      session.getRunContentDescriptor().preferredFocusComputable.compute().addFocusListener(object : FocusListener {
        override fun focusGained(e: FocusEvent?) {
          synchronized(lockObject) {
            lastFocusedSession = session
          }
        }

        override fun focusLost(e: FocusEvent?) {}
      })
    }
    synchronized(lockObject) {
      lastFocusedSession = session
    }
    return session
  }

  private suspend fun tryCreateDefaultConsoleRunner(): FsiDefaultConsoleRunner? {
    synchronized(lockObject) {
      val session = defaultFsiSession
      if (session?.isValid() == true) return session
    }

    val (executable, runtime) = getFsiRunOptions(project)
    try {
      executable.validate()
    } catch (t: Throwable) {
      notifyFsiNotFound(t.message!!)
      return null
    }
    val cmdLine = executable.createRunCommandLine(runtime)
    val session = getOrCreateConsoleRunner { FsiDefaultConsoleRunner(cmdLine, this) }
    synchronized(lockObject) {
      defaultFsiSession = session
    }
    return session
  }

  suspend fun createConsoleRunner(
    title: String,
    project: Project,
    executor: Executor,
    commandLine: GeneralCommandLine,
    presentableCommandLineString: String? = null
  ) =
    getOrCreateConsoleRunner {
      FsiScriptProfileConsoleRunner(
        title,
        project,
        executor,
        commandLine,
        presentableCommandLineString
      )
    }

  private fun notifyFsiNotFound(@NlsSafe content: String) {
    val title = FSharpBundle.message("Fsi.notifications.fsi.not.found.title")
    val notification = Notification(FsiDefaultConsoleRunner.fsiTitle, title, content, NotificationType.WARNING)
    notification.icon = FSharpIcons.FSharpConsole
    Notifications.Bus.notify(notification, project)
  }
}
