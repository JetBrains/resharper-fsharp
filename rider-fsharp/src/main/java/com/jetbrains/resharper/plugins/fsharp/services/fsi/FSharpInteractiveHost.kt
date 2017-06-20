package com.jetbrains.resharper.plugins.fsharp.services.fsi

import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.console.BasicGutterContentProvider
import com.intellij.execution.console.LanguageConsoleBuilder
import com.intellij.execution.console.LanguageConsoleView
import com.intellij.execution.console.ProcessBackedConsoleExecuteActionHandler
import com.intellij.execution.process.OSProcessHandler
import com.intellij.execution.runners.AbstractConsoleRunnerWithHistory
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.fileTypes.PlainTextLanguage
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.SystemInfo
import com.intellij.openapi.wm.impl.ToolWindowManagerImpl
import com.jetbrains.resharper.projectView.solution
import com.jetbrains.resharper.runtime.DotNetRuntime
import com.jetbrains.resharper.runtime.RiderDotNetActiveRuntimeHost
import com.jetbrains.resharper.util.idea.ILifetimedComponent
import com.jetbrains.resharper.util.idea.LifetimedComponent
import com.jetbrains.rider.model.RdFSharpInteractiveHost

class FSharpInteractiveHost(val project: Project, runtimeHost: RiderDotNetActiveRuntimeHost)
    : ILifetimedComponent by LifetimedComponent(project) {
    val fsiTitle = "F# Interactive"
    val runtime: DotNetRuntime? = runtimeHost.getCurrentDotNetRuntime(false)

    val fsharpInteractiveHost: RdFSharpInteractiveHost
        get() = project.solution.fSharpInteractiveHost

    private var runner: FsiConsoleRunner? = null

    init {
        fsharpInteractiveHost.printOutput.advise(componentLifetime) { text ->
            sendText(text)
        }
    }

    private fun getConsoleRunner(): FsiConsoleRunner {
        synchronized(this) {
            if (runner == null) {
                runner = FsiConsoleRunner(this, fsiTitle)
                runner!!.initAndRun()
            }
            return runner as FsiConsoleRunner
        }
    }

    fun removeConsoleRunner() {
        synchronized(this) {
            runner = null
        }
    }

    fun sendText(text: String) {
        val consoleRunner = getConsoleRunner()
        val processHandler = consoleRunner.processHandler
        val stream = processHandler.processInput ?: error("Broken Fsi stream")
        val charset = Charsets.UTF_8

        consoleRunner.consoleView.setInputText("$text;;\n")
        consoleRunner.show()

//        val bytes = ().toByteArray(charset)
//        stream.write(bytes)
//        stream.flush()
    }
}

class FsiConsoleRunner(val host: FSharpInteractiveHost, title: String) : AbstractConsoleRunnerWithHistory<LanguageConsoleView>(host.project, title, null) {
    val fsiPath = if (SystemInfo.isWindows) "C:\\Program Files (x86)\\Microsoft SDKs\\F#\\4.1\\Framework\\v4.0\\fsi.exe" else "/usr/local/bin/fsharpi"
    val cmdLine = GeneralCommandLine()
            .withExePath(fsiPath)
            .withParameters("--fsi-server-input-codepage:65001", "--fsi-server-output-codepage:65001")
//    val cmdLine = host.runtime?.createRunProcessCmd(fsiPath, StringUtil.notNullize(project.basePath), emptyList(), emptyMap())

    fun show() {
        ToolWindowManagerImpl.getInstance(host.project).getToolWindow(executor.id).show(null)
    }

    override fun finishConsole() {
        host.removeConsoleRunner()
        super.finishConsole()
    }

    override fun createProcess(): Process? {
        return cmdLine.createProcess()
    }

    override fun createExecuteActionHandler(): ProcessBackedConsoleExecuteActionHandler {
        return ProcessBackedConsoleExecuteActionHandler(processHandler, false)
    }

    override fun createConsoleView(): LanguageConsoleView {
        val builder = LanguageConsoleBuilder()
        val gutterProvider = object : BasicGutterContentProvider() {
            override fun beforeEvaluate(e: Editor) = Unit
        }
        val consoleView = builder.gutterContentProvider(gutterProvider).build(project, PlainTextLanguage.INSTANCE)
        consoleView.prompt = null

        return consoleView
    }

    override fun createProcessHandler(process: Process): OSProcessHandler {
        val processHandler = object : OSProcessHandler(process, cmdLine.commandLineString, Charsets.UTF_8) {
            override fun isSilentlyDestroyOnClose(): Boolean = true
        }
        return processHandler
    }
}