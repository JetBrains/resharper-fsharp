package com.jetbrains.rider.plugins.fsharp.test

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.CapturingProcessHandler
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.LocalFileSystem
import com.jetbrains.rider.plugins.fsharp.rdFSharpModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.protocol.IProtocolHost
import com.jetbrains.rider.protocol.protocolHost
import com.jetbrains.rider.settings.RiderSettingMaintenanceController
import com.jetbrains.rider.test.facades.solution.SolutionApiFacade
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.framework.waitBackend
import com.jetbrains.rider.test.scriptingApi.BackendSettingsEditorBase
import com.jetbrains.rider.test.scriptingApi.dumpSevereHighlighters
import com.jetbrains.rider.test.scriptingApi.editBackendSettingsSuspending
import com.jetbrains.rider.test.scriptingApi.runBlockingWithFlushing
import java.io.PrintStream
import java.nio.file.Path
import kotlin.time.Duration.Companion.minutes

fun com.intellij.openapi.editor.Editor.dumpTypeProviders(stream: PrintStream) {
  with(stream) {
    println((project ?: return).solution.rdFSharpModel.fsharpTestHost.dumpTypeProvidersProcess.sync(Unit))
    println("\nSevereHighlighters:")
    dumpSevereHighlighters(this)
  }
}

fun withSetting(project: Project, setting: String, enterValue: String, exitValue: String, function: () -> Unit) {
  val settingController = RiderSettingMaintenanceController.getInstance(project.protocolHost)
  settingController.setSetting(setting, enterValue)
  try {
    function()
  } finally {
    settingController.setSetting(setting, exitValue)
  }
}

fun SolutionApiFacade.withDisabledOutOfProcessTypeProviders(function: () -> Unit) {
  withSetting(
    project,
    "FSharp/FSharpOptions/FSharpExperimentalFeatures/OutOfProcessTypeProviders/@EntryValue",
    "false",
    "true"
  ) {
    function()
  }
}


private fun SolutionApiFacade.withNonFSharpProjectReferencesSetting(enable: Boolean, function: () -> Unit) {
  val (enterValue, exitValue) = if (enable) Pair("true", "false") else Pair("false", "true");
  withSetting(project, "FSharp/FSharpOptions/NonFSharpProjectInMemoryReferences/@EntryValue", enterValue, exitValue) {
    project.fcsHost.updateAssemblyReaderSettings.sync(Unit)
    function()
  }
  project.fcsHost.updateAssemblyReaderSettings.sync(Unit)
}

fun SolutionApiFacade.withNonFSharpProjectReferences(function: () -> Unit) {
  withNonFSharpProjectReferencesSetting(true, function)
}

fun SolutionApiFacade.withoutNonFSharpProjectReferences(function: () -> Unit) {
  withNonFSharpProjectReferencesSetting(false, function)
}


fun withEditorConfig(project: Project, function: () -> Unit) {
  withSetting(project, "CodeStyle/EditorConfig/EnableEditorConfigSupport", "true", "false", function)
}

//TODO: move to test framework
fun runProcessWaitForExit(cmd: Path, args: List<String>, env: Map<String, String>, timeoutMinutes: Int = 10) {
  val cmdArray = mutableListOf(cmd.toAbsolutePath().toString())
  cmdArray.addAll(args)

  val cmdline = GeneralCommandLine()
    .withExePath(cmdArray.first().toString())
    .withEnvironment(env)
    .withParameters(cmdArray.drop(1))

  frameworkLogger.info("Starting $cmdline")

  val processOutput = CapturingProcessHandler(cmdline).runProcess(timeoutMinutes * 60 * 1000, true)

  frameworkLogger.debug("$cmdline stdout: ${processOutput.stdout}")
  frameworkLogger.debug("$cmdline stderr: ${processOutput.stderr}")

  if (processOutput.checkSuccess(Logger.getInstance("com.jetbrains.rider.test.framework"))) {
    frameworkLogger.info("$cmdline was successfully executed")
  } else {
    throw Exception("Unable to run process $cmdline: cancelled: ${processOutput.isCancelled} timeout: ${processOutput.isTimeout} exitcode: ${processOutput.exitCode} stderr: [[[${processOutput.stderr}]]] stdout: [[[${processOutput.stdout}]]]")
  }
}

fun withCultureInfo(project: Project, culture: String, function: () -> Unit) {
  val getCultureInfoAndSetNew = project.fcsHost.getCultureInfoAndSetNew
  val oldCulture = getCultureInfoAndSetNew.sync(culture)
  try {
    function()
  } finally {
    getCultureInfoAndSetNew.sync(oldCulture)
  }
}

fun flushFileChanges(project: Project) {
  val fs = LocalFileSystem.getInstance()
  fs.refresh(false)
  waitBackend(project.protocolHost)
}

val Project.fcsHost get() = this.solution.rdFSharpModel.fsharpTestHost

class FsharpBackendSettingsEditor(host: IProtocolHost) : BackendSettingsEditorBase(host.session) {
  var dotnetCliHomeEnvVar by StringEnvVar("DOTNET_CLI_HOME")
}

fun editFSharpBackendSettings(host: IProtocolHost, action: FsharpBackendSettingsEditor.() -> Unit) =
  runBlockingWithFlushing("editFSharpBackendSettings", 1.minutes) {
    editBackendSettingsSuspending(FsharpBackendSettingsEditor(host), action)
  }


