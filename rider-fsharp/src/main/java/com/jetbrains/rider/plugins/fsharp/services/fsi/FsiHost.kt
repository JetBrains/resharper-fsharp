package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.execution.Executor
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.ProcessAdapter
import com.intellij.execution.process.ProcessEvent
import com.intellij.notification.Notification
import com.intellij.notification.NotificationType
import com.intellij.notification.Notifications
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.NlsSafe
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.project.isDirectoryBased
import com.intellij.psi.PsiFile
import com.intellij.util.ui.UIUtil
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rd.util.reactive.flowInto
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.plugins.fsharp.*
import com.jetbrains.rider.plugins.fsharp.services.fsi.consoleRunners.FSharpConsoleRunnerWithHistory
import com.jetbrains.rider.plugins.fsharp.services.fsi.consoleRunners.FsiDefaultConsoleRunner
import com.jetbrains.rider.plugins.fsharp.services.fsi.consoleRunners.FsiScriptProfileConsoleRunner
import com.jetbrains.rider.plugins.fsharp.services.fsi.runScript.FSharpScriptConfiguration
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.run.configurations.dotNetExe.DotNetExeConfigurationParameters
import com.jetbrains.rider.run.createRunCommandLine
import com.jetbrains.rider.runtime.DotNetExecutable
import com.jetbrains.rider.runtime.DotNetRuntime
import com.jetbrains.rider.runtime.RiderDotNetActiveRuntimeHost
import com.jetbrains.rider.runtime.dotNetCore.DotNetCoreRuntimeType
import com.jetbrains.rider.runtime.mono.MonoRuntime
import com.jetbrains.rider.runtime.mono.MonoRuntimeType
import com.jetbrains.rider.runtime.msNet.MsNetRuntimeType
import org.jetbrains.concurrency.AsyncPromise
import org.jetbrains.concurrency.Promise
import org.jetbrains.concurrency.resolvedPromise
import java.awt.event.FocusEvent
import java.awt.event.FocusListener
import java.util.*

fun getFsiRunOptions(project: Project, configuration: FSharpScriptConfiguration? = null): Pair<DotNetExecutable, DotNetRuntime> {
  val fsiHost = project.getComponent(FsiHost::class.java)
  lateinit var sessionInfo: RdFsiSessionInfo
  UIUtil.invokeLaterIfNeeded {
    sessionInfo = fsiHost.rdFsiHost.requestNewFsiSessionInfo.sync(Unit)
  }

  val runtimeHost = RiderDotNetActiveRuntimeHost.getInstance(project)

  val exePath = when (sessionInfo.runtime) {
    RdFsiRuntime.Core -> runtimeHost.dotNetCoreRuntime.value!!.cliExePath
    RdFsiRuntime.NetFramework -> sessionInfo.fsiPath
    RdFsiRuntime.Mono -> {
      val runtime = runtimeHost.getCurrentClassicNetRuntime(false).runtime as MonoRuntime
      runtime.getMonoExe().path
    }
  }

  val workingDirectory =
    if (project.isDirectoryBased) {
      val projectDir = project.baseDir
      val workingDir = if (projectDir.exists()) projectDir else VfsUtil.getUserHomeDir()
      workingDir?.path ?: ""
    }
    else ""


  val runtimeType = when (sessionInfo.runtime) {
    RdFsiRuntime.NetFramework -> MsNetRuntimeType
    RdFsiRuntime.Mono -> MonoRuntimeType
    RdFsiRuntime.Core -> DotNetCoreRuntimeType
  }

  val args = ArrayList<String>()
  if (sessionInfo.runtime != RdFsiRuntime.NetFramework)
    args.add(sessionInfo.fsiPath)
  if (configuration != null)
    args.add("--use:${configuration.scriptFile?.path}")
  args.addAll(sessionInfo.args)

  val parameters = DotNetExeConfigurationParameters(
    project = project,
    exePath = exePath,
    programParameters = args.joinToString(" "),
    workingDirectory = workingDirectory,
    envs = configuration?.envs ?: mapOf(),
    isPassParentEnvs = true,
    useExternalConsole = false,
    executeAsIs = false,
    assemblyToDebug = null,
    runtimeType = runtimeType,
    runtimeArguments = "")

  val dotNetExecutable = parameters.toDotNetExecutable()
  val dotNetRuntime = DotNetRuntime.detectRuntimeForExeOrThrow(
    project,
    RiderDotNetActiveRuntimeHost.getInstance(project),
    dotNetExecutable.exePath,
    dotNetExecutable.runtimeType,
    dotNetExecutable.projectTfm
  )

  val patchedExecutable = dotNetExecutable.copy(usePty = false)
  return Pair(patchedExecutable, dotNetRuntime)
}


class FsiHost(project: Project) : LifetimedProjectComponent(project) {
  val rdFsiHost = project.solution.rdFSharpModel.fSharpInteractiveHost

  val moveCaretOnSendLine = Property(true)
  val moveCaretOnSendSelection = Property(true)
  val copyRecentToEditor = Property(false)

  init {
    rdFsiHost.moveCaretOnSendLine.flowInto(componentLifetime, moveCaretOnSendLine)
    rdFsiHost.moveCaretOnSendSelection.flowInto(componentLifetime, moveCaretOnSendSelection)
    rdFsiHost.copyRecentToEditor.flowInto(componentLifetime, copyRecentToEditor)
  }

  fun sendToFsi(editor: Editor, file: PsiFile, debug: Boolean) {
    synchronized(lockObject) {
      (lastFocusedSession?.let { if (it.isValid()) resolvedPromise(it) else null } ?: tryCreateDefaultConsoleRunner()).onSuccess {
        it.sendActionExecutor.execute(editor, file, debug)
      }
    }
  }

  fun sendToFsi(visibleText: String, fsiText: String, debug: Boolean) {
    synchronized(lockObject) {
      (lastFocusedSession?.let { if (it.isValid()) resolvedPromise(it) else null } ?: tryCreateDefaultConsoleRunner()).onSuccess {
        it.sendText(visibleText, fsiText) //WHY NOT SENDER
      }
    }
  }

  fun resetFsiDefaultConsole() {
    synchronized(lockObject) {
      val session = sessions.getOrDefault("", null)
      if (session != null) {
        if (session.isValid()) session.stop()
        sessions.remove("")
      }
      tryCreateDefaultConsoleRunner()
    }
  }

  private val sessions = mutableMapOf<String?, FSharpConsoleRunnerWithHistory>()
  private val lockObject = Object()
  var lastFocusedSession: FSharpConsoleRunnerWithHistory? = null

  private fun getOrCreateConsoleRunner(
    sessionId: String,
    factory: () -> FSharpConsoleRunnerWithHistory
  ): FSharpConsoleRunnerWithHistory {
    val session = sessions.getOrPut(sessionId) {
      val session = factory()
      session.initAndRun()
      session.processHandler.addProcessListener(object : ProcessAdapter() {
        override fun processTerminated(event: ProcessEvent) {
          synchronized(lockObject) {
            sessions.remove(sessionId)
          }
        }
      })
      session.getRunContentDescriptor().preferredFocusComputable.compute().addFocusListener(object : FocusListener {
        override fun focusGained(e: FocusEvent?) {
          lastFocusedSession = session
        }

        override fun focusLost(e: FocusEvent?) {}
      })
      session
    }
    lastFocusedSession = session
    return session
  }

  private fun tryCreateDefaultConsoleRunner(): Promise<FSharpConsoleRunnerWithHistory> {
    val result = AsyncPromise<FSharpConsoleRunnerWithHistory>()
    val (executable, runtime) = getFsiRunOptions(project)
    try {
      executable.validate()
    }
    catch (t: Throwable) {
      notifyFsiNotFound(t.message!!)
      return result
    }
    val cmdLine = executable.createRunCommandLine(runtime)
    val consoleRunner = getOrCreateConsoleRunner("") { FsiDefaultConsoleRunner(cmdLine, this, false) }
    result.setResult(consoleRunner)

    return result
  }

  fun createConsoleRunner(title: String,
                          scriptPath: String,
                          project: Project,
                          executor: Executor,
                          commandLine: GeneralCommandLine) =
    getOrCreateConsoleRunner(scriptPath) { FsiScriptProfileConsoleRunner(title, project, executor, commandLine) }

  private fun notifyFsiNotFound(@NlsSafe content: String) {
    val title = FSharpBundle.message("Fsi.notifications.fsi.not.found.title")
    val notification = Notification(FsiDefaultConsoleRunner.fsiTitle, title, content, NotificationType.WARNING)
    notification.icon = FSharpIcons.FSharpConsole
    Notifications.Bus.notify(notification, project)
  }
}
