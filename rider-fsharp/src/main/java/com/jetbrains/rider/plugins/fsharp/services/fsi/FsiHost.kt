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
import com.intellij.openapi.project.guessProjectDir
import com.intellij.openapi.util.NlsSafe
import com.intellij.openapi.util.TextRange
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.project.isDirectoryBased
import com.intellij.psi.PsiFile
import com.intellij.util.concurrency.annotations.RequiresEdt
import com.intellij.util.execution.ParametersListUtil
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rd.util.reactive.flowInto
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
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
import com.jetbrains.rider.run.createRunCommandLine
import com.jetbrains.rider.runtime.DotNetExecutable
import com.jetbrains.rider.runtime.DotNetRuntime
import com.jetbrains.rider.runtime.RiderDotNetActiveRuntimeHost
import com.jetbrains.rider.runtime.dotNetCore.DotNetCoreRuntimeType
import com.jetbrains.rider.runtime.mono.MonoRuntime
import com.jetbrains.rider.runtime.mono.MonoRuntimeType
import com.jetbrains.rider.runtime.msNet.MsNetRuntimeType
import java.awt.event.FocusEvent
import java.awt.event.FocusListener

@RequiresEdt
fun getFsiRunOptions(
  project: Project,
  configuration: FSharpScriptConfiguration? = null
): Pair<DotNetExecutable, DotNetRuntime> {
  val fsiHost = project.getComponent(FsiHost::class.java)
  val sessionInfo = fsiHost.rdFsiHost.requestNewFsiSessionInfo.sync(Unit)
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
  if (sessionInfo.runtime != RdFsiRuntime.NetFramework)
    args.add(sessionInfo.fsiPath)
  if (configuration != null)
    args.add("--use:${configuration.scriptFile?.path}")
  args.addAll(sessionInfo.args)

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

  val dotNetExecutable = parameters.toDotNetExecutable()
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


class FsiHost(project: Project) : LifetimedProjectComponent(project) {
  val rdFsiHost = project.solution.rdFSharpModel.fSharpInteractiveHost

  private val moveCaretOnSendLine = Property(true)
  private val moveCaretOnSendSelection = Property(true)
  val copyRecentToEditor = Property(false)

  init {
    rdFsiHost.moveCaretOnSendLine.flowInto(componentLifetime, moveCaretOnSendLine)
    rdFsiHost.moveCaretOnSendSelection.flowInto(componentLifetime, moveCaretOnSendSelection)
    rdFsiHost.copyRecentToEditor.flowInto(componentLifetime, copyRecentToEditor)
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

    if (visibleText.isNotEmpty()) {
      val textLineStart =
        if (hasSelection) editor.document.getLineNumber(editor.selectionModel.selectionStart)
        else editor.caretModel.logicalPosition.line
      val fsiText = "\n" +
        "# silentCd @\"${file.containingDirectory.virtualFile.path}\" ;; \n" +
        (if (debug) "# dbgbreak\n" else "") +
        "# ${textLineStart + 1} @\"${file.virtualFile.path}\" \n" +
        visibleText + "\n" +
        "# 1 \"stdin\"\n;;\n"
      sendToFsi(visibleText, fsiText, debug)
    }

    if (!hasSelection && moveCaretOnSendLine.value)
      editor.caretModel.moveCaretRelatively(0, 1, false, false, true)

    if (hasSelection && moveCaretOnSendSelection.value) {
      editor.caretModel.moveToOffset(selectionModel.selectionEnd)
      editor.caretModel.currentCaret.removeSelection()
    }
  }

  fun sendToFsi(visibleText: String, fsiText: String, debug: Boolean) {
    synchronized(lockObject) {
      val fsiRunner =
        if (lastFocusedSession?.isValid() == true) lastFocusedSession
        else tryCreateDefaultConsoleRunner()
      fsiRunner?.sendText(visibleText, fsiText)
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

  private val sessions = mutableMapOf<String?, FsiConsoleRunnerBase>()
  private val lockObject = Object()
  var lastFocusedSession: FsiConsoleRunnerBase? = null

  private fun getOrCreateConsoleRunner(
    sessionId: String,
    factory: () -> FsiConsoleRunnerBase
  ): FsiConsoleRunnerBase {
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
          synchronized(lockObject) {
            lastFocusedSession = session
          }
        }

        override fun focusLost(e: FocusEvent?) {}
      })
      session
    }
    lastFocusedSession = session
    return session
  }

  private fun tryCreateDefaultConsoleRunner(): FsiConsoleRunnerBase? {
    val (executable, runtime) = getFsiRunOptions(project)
    try {
      executable.validate()
    } catch (t: Throwable) {
      notifyFsiNotFound(t.message!!)
      return null
    }
    val cmdLine = executable.createRunCommandLine(runtime)
    return getOrCreateConsoleRunner("") { FsiDefaultConsoleRunner(cmdLine, this) }
  }

  fun createConsoleRunner(
    title: String,
    scriptPath: String,
    project: Project,
    executor: Executor,
    commandLine: GeneralCommandLine
  ) =
    getOrCreateConsoleRunner(scriptPath) { FsiScriptProfileConsoleRunner(title, project, executor, commandLine) }

  private fun notifyFsiNotFound(@NlsSafe content: String) {
    val title = FSharpBundle.message("Fsi.notifications.fsi.not.found.title")
    val notification = Notification(FsiDefaultConsoleRunner.fsiTitle, title, content, NotificationType.WARNING)
    notification.icon = FSharpIcons.FSharpConsole
    Notifications.Bus.notify(notification, project)
  }
}
