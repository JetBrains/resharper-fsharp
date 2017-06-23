package com.jetbrains.resharper.plugins.fsharp.services.fsi

import com.intellij.openapi.project.Project
import com.jetbrains.resharper.projectView.solution
import com.jetbrains.resharper.util.idea.ILifetimedComponent
import com.jetbrains.resharper.util.idea.LifetimedComponent
import com.jetbrains.rider.framework.RdVoid
import com.jetbrains.rider.model.RdFSharpInteractiveHost
import com.jetbrains.rider.model.RdFsiSendTextRequest

class FsiHost(val project: Project) : ILifetimedComponent by LifetimedComponent(project) {

    val rdFsiHost: RdFSharpInteractiveHost get() = project.solution.fSharpInteractiveHost
    private var runner: FsiConsoleRunner? = null

    init {
        rdFsiHost.sendText.advise(componentLifetime) { request -> sendText(request) }
    }

    internal fun sendText(request: RdFsiSendTextRequest) = getConsoleRunner().sendText(request) // todo

    private fun getConsoleRunner(): FsiConsoleRunner = synchronized(this) {
        if (runner?.isValid() ?: false) return runner!!

        createConsoleRunner()
        return runner!!
    }

    internal fun resetRunner() = synchronized(this) {
        if (runner?.isValid() ?: false) {
            runner!!.processHandler.destroyProcess()
        }
        createConsoleRunner()
    }

    private fun createConsoleRunner() {
        val runner = FsiConsoleRunner(rdFsiHost.requestNewFsiSessionInfo.sync(RdVoid), this)
        runner.initAndRun()

        this.runner = runner
    }
}
