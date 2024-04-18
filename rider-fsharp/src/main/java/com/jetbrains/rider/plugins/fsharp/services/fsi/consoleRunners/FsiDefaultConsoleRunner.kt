package com.jetbrains.rider.plugins.fsharp.services.fsi.consoleRunners

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.icons.AllIcons
import com.intellij.ide.DataManager
import com.intellij.notification.Notification
import com.intellij.notification.NotificationListener
import com.intellij.openapi.actionSystem.ActionPlaces
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.Separator
import com.intellij.openapi.options.ShowSettingsUtil
import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.fsharp.FSharpBundle
import com.jetbrains.rider.plugins.fsharp.services.fsi.CommandHistoryAction
import com.jetbrains.rider.plugins.fsharp.services.fsi.FsiHost
import com.jetbrains.rider.util.idea.getComponent
import javax.swing.event.HyperlinkEvent

class FsiDefaultConsoleRunner(commandLine: GeneralCommandLine, fsiHost: FsiHost, debug: Boolean) :
  FSharpConsoleRunnerWithHistory(fsiHost.project, fsiTitle, commandLine) {

  companion object {
    val fsiTitle = FSharpBundle.message("Fsi.ConsoleRunner.title")
    private val debugNotConfiguredTitle = FSharpBundle.message("Fsi.notifications.debug.not.configured.title")
    private val debugNotConfiguredDescription = FSharpBundle.message("Fsi.notifications.debug.not.configured.description")
    private val notificationLinks = "<br/>${RelaunchFsiWithDebugAction.link}&nbsp;&nbsp;&nbsp;&nbsp;${ShowFsiSettingsAction.link}"

    private val debugFsiArgs = listOf("--optimize-", "--debug+")

    private const val waitForDebugSessionTimeout = 15000L
  }

  //val optimizeForDebug = debug || sessionInfo.fixArgsForAttach
  //private val fsiArgs = sessionInfo.args + (if (optimizeForDebug) debugFsiArgs else emptyList())
  //private var cmdLine = GeneralCommandLine()
  //  .withExePath(sessionInfo.fsiPath)
  //  .withParameters(fsiArgs)

  //init {
  //  val runtimeHost = project.getComponent<RiderDotNetActiveRuntimeHost>()
  //  if (sessionInfo.runtime == RdFsiRuntime.Core) {
  //    runtimeHost.dotNetCoreRuntime.value?.patchRunCommandLine(cmdLine, listOf())
  //  }
  //  else {
  //    val runtime = runtimeHost.getCurrentClassicNetRuntime(false).runtime
  //    if (runtime != null && runtime is MonoRuntime && sessionInfo.fsiPath.endsWith(".exe", true)) {
  //      runtime.patchRunCommandLine(cmdLine, listOf())
  //    }
  //  }
  //
  //  if (project.isDirectoryBased) {
  //    val projectDir = project.baseDir
  //    val workingDir = if (projectDir.exists()) projectDir else VfsUtil.getUserHomeDir()
  //    if (workingDir != null)
  //      cmdLine.workDirectory = File(workingDir.path)
  //  }
  //}

  //private var pid by Delegates.notNull<Int>()

  //private val isAttached
  //  get() = getDebugProcessForThisFsi() != null
  //
  //private val lockObject = Object()

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

  //private fun getDebugProcessForThisFsi() = XDebuggerManager.getInstance(project).debugSessions
  //  .map { it.debugProcess }.filterIsInstance<DotNetDebugProcess>().firstOrNull()

  //fun attachToProcess(): Boolean {
  //  if (!optimizeForDebug) {
  //    val notification = Notification(
  //      fsiTitle, debugNotConfiguredTitle, debugNotConfiguredDescription + notificationLinks,
  //      NotificationType.WARNING, listener
  //    )
  //    notification.icon = FSharpIcons.FSharpConsole
  //    Notifications.Bus.notify(notification, project)
  //    return false
  //  }
  //
  //  val processInfo = OSProcessUtil.getProcessList().firstOrNull { it.pid == pid } ?: return false
  //  val dataHolder = UserDataHolderBase()
  //  val debugger = Extensions.getExtensions(XAttachDebuggerProvider.EP).flatMap { provider ->
  //    provider.getAvailableDebuggers(project, LocalAttachHost.INSTANCE, processInfo, dataHolder)
  //  }.firstOrNull() ?: return false
  //  debugger.attachDebugSession(project, LocalAttachHost.INSTANCE, processInfo)
  //  return true
  //}


  fun sendText(visibleText: String, fsiText: String, debug: Boolean) {
    //attachDebuggerIfNeeded(debug).onSuccess {
      sendText(visibleText, fsiText)
    //}.onError {
    //  sendText(visibleText, fsiText)
    //  Logger.getInstance(FsiDefaultConsoleRunner::class.java).warn(it)
    //}
  }

  //private fun attachDebuggerIfNeeded(debug: Boolean): Promise<Unit> {
  //  if (!debug || isAttached) {
  //    return resolvedPromise()
  //  }
  //  synchronized(lockObject) {
  //    val promise = AsyncPromise<Unit>()
  //    if (!attachToProcess()) {
  //      promise.setResult(Unit)
  //      return promise
  //    }
  //    //need to free dispatcher thread to pump messages over protocol for async debug runner
  //    application.executeOnPooledThread {
  //      if (!pumpMessages(Duration.ofSeconds(waitForDebugSessionTimeout)) {
  //          getDebugProcessForThisFsi() != null
  //        }) {
  //        promise.setError(IllegalStateException("Failed to get debug process for fsi.exe with pid $pid"))
  //      }
  //      else {
  //        promise.setResult(Unit)
  //      }
  //    }
  //    return promise
  //  }
  //}
  override fun getToolBarActions() =
    mutableListOf(
      ResetFsiAction(this.fsiHost),
      Separator(),
      CommandHistoryAction(this),
      Separator(),
      OpenSettings(project))

  private class ResetFsiAction(private val host: FsiHost) :
    AnAction(FSharpBundle.message("Fsi.actions.reset.fsi.title"), null, AllIcons.Actions.Restart) {
    override fun actionPerformed(e: AnActionEvent) {
      host.resetFsiDefaultConsole()
    }
  }
}

class RelaunchFsiWithDebugAction(private val currentProject: Project? = null) : AnAction() {
  override fun actionPerformed(e: AnActionEvent) {
    val project = e.project ?: currentProject ?: return
    val fsiHost = project.getComponent<FsiHost>()
    fsiHost.resetFsiDefaultConsole()
  }

  companion object {
    const val actionName = "relaunchWithDebug"
    val link = "<a href='$actionName'>${FSharpBundle.message("Fsi.actions.relaunch.fsi.with.debug.link.text")}</a>"
  }
}

class ShowFsiSettingsAction(private val currentProject: Project? = null) : AnAction() {
  override fun actionPerformed(e: AnActionEvent) {
    val project = e.project ?: currentProject ?: return
    ShowSettingsUtil.getInstance().showSettingsDialog(project, "Fsi")
  }

  companion object {
    const val actionName = "settings"
    val link = "<a href='$actionName'>${FSharpBundle.message("Fsi.actions.show.fsi.settings.link.text")}</a>"
  }
}
