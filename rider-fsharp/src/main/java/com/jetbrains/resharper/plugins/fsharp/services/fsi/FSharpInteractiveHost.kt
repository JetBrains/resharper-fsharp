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
import com.intellij.openapi.wm.impl.ToolWindowManagerImpl
import com.jetbrains.resharper.projectView.solution
import com.jetbrains.resharper.runtime.RiderDotNetActiveRuntimeHost
import com.jetbrains.resharper.util.idea.ILifetimedComponent
import com.jetbrains.resharper.util.idea.LifetimedComponent
import com.jetbrains.rider.framework.RdVoid
import com.jetbrains.rider.model.RdFSharpInteractiveHost
import com.jetbrains.rider.model.RdFsiSendTextRequest
import com.jetbrains.rider.model.RdFsiSessionInfo

class FSharpInteractiveHost(val project: Project, val runtimeHost: RiderDotNetActiveRuntimeHost)
    : ILifetimedComponent by LifetimedComponent(project) {

    val rdFsiHost: RdFSharpInteractiveHost get() = project.solution.fSharpInteractiveHost
    private var runner: FsiConsoleRunner? = null

    init {
        rdFsiHost.sendText.advise(componentLifetime) { request -> getConsoleRunner().sendText(request) }
    }

    private fun getConsoleRunner(): FsiConsoleRunner {
        synchronized(this) {
            if (runner == null) {
                val sessionInfo = rdFsiHost.requestNewFsiSessionInfo.sync(RdVoid)
                runner = FsiConsoleRunner(sessionInfo, this)
                runner!!.initAndRun()
            }
            return runner!!
        }
    }

    fun removeConsoleRunner() {
        synchronized(this) {
            runner = null
        }
    }
}

class FsiConsoleRunner(sessionInfo: RdFsiSessionInfo, val host: FSharpInteractiveHost)
    : AbstractConsoleRunnerWithHistory<LanguageConsoleView>(host.project, fsiTitle, null) {

    companion object {
        val fsiTitle = "F# Interactive"
    }

    val cmdLine = GeneralCommandLine().withExePath(sessionInfo.fsiPath).withParameters(sessionInfo.args)

    fun sendText(request: RdFsiSendTextRequest) {
        val stream = processHandler.processInput ?: error("Broken Fsi stream")
        val charset = Charsets.UTF_8

        consoleView.setInputText("${request.visibleText};;\n")
        ToolWindowManagerImpl.getInstance(host.project).getToolWindow(executor.id).show(null)
//        val bytes = ().toByteArray(charset)
//        stream.write(bytes)
//        stream.flush()
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
        val consoleView = builder.gutterContentProvider(gutterProvider).build(project, PlainTextLanguage.INSTANCE) // todo: Use F# language
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