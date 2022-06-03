package com.jetbrains.rider.plugins.fsharp.test

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.CapturingProcessHandler
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.SystemInfo
import com.intellij.openapi.vfs.LocalFileSystem
import com.jetbrains.rdclient.protocol.protocolHost
import com.jetbrains.rider.RiderEnvironment
import com.jetbrains.rider.inTests.TestHost
import com.jetbrains.rider.plugins.fsharp.rdFSharpModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.test.base.BaseTestWithSolution
import com.jetbrains.rider.test.framework.frameworkLogger
import com.jetbrains.rider.test.framework.waitBackend
import com.jetbrains.rider.test.scriptingApi.dumpSevereHighlighters
import java.io.PrintStream
import java.nio.file.Path

fun com.intellij.openapi.editor.Editor.dumpTypeProviders(stream: PrintStream) {
    with(stream) {
        println((project ?: return).solution.rdFSharpModel.fsharpTestHost.dumpTypeProvidersProcess.sync(Unit))
        println("\nSevereHighlighters:")
        dumpSevereHighlighters(this)
    }
}

fun withSetting(project: Project, setting: String, enterValue: String, exitValue: String, function: () -> Unit) {
    TestHost.getInstance(project.protocolHost).setSetting(setting, enterValue)
    try {
        function()
    } finally {
        TestHost.getInstance(project.protocolHost).setSetting(setting, exitValue)
    }
}

fun BaseTestWithSolution.withDisabledOutOfProcessTypeProviders(function: () -> Unit) {
    withSetting(project, "FSharp/FSharpOptions/FSharpExperimentalFeatures/OutOfProcessTypeProviders/@EntryValue", "false", "true") {
        function()
    }
}

fun withEditorConfig(project: Project, function: () -> Unit) {
    withSetting(project, "CodeStyle/EditorConfig/EnableEditorConfigSupport", "true", "false", function)
}

//TODO: move to test framework
fun runProcessWaitForExit(cmd: Path, args: List<String>, env: Map<String, String>, timeoutMinutes: Int = 10) {
    val cmdArray = mutableListOf(cmd.toAbsolutePath().toString())
    cmdArray.addAll(args)

    if (!SystemInfo.isWindows) {
        cmdArray.add(0, RiderEnvironment.getBundledFile("runtime.sh").absolutePath)
    }

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
