package com.jetbrains.resharper.plugins.fsharp.services.fsi

import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.psi.PsiFile
import com.jetbrains.resharper.projectView.solution
import com.jetbrains.resharper.util.idea.ILifetimedComponent
import com.jetbrains.resharper.util.idea.LifetimedComponent
import com.jetbrains.rider.framework.RdVoid
import com.jetbrains.rider.model.RdFSharpInteractiveHost
import kotlin.properties.Delegates

class FsiHost(val project: Project) : ILifetimedComponent by LifetimedComponent(project) {
    val rdFsiHost: RdFSharpInteractiveHost get() = project.solution.fSharpInteractiveHost
    var moveCaretOnSendLine by Delegates.notNull<Boolean>()
    private var fsiConsole: FsiConsoleRunner? = null

    init {
        rdFsiHost.moveCaretOnSendLine.advise(componentLifetime) { moveCaretOnSendLine = it }
    }

    internal fun sendToFsi(editor: Editor, file: PsiFile) = getFsiConsole().sendActionExecutor.execute(editor, file)

    internal fun getFsiConsole(): FsiConsoleRunner = synchronized(this) {
        if (fsiConsole?.isValid() ?: false) return fsiConsole!!

        createConsoleRunner()
        return fsiConsole!!
    }

    internal fun resetFsiConsole() = synchronized(this) {
        if (fsiConsole?.isValid() ?: false) {
            fsiConsole!!.processHandler.destroyProcess()
        }
        createConsoleRunner()
    }

    private fun createConsoleRunner() {
        val runner = FsiConsoleRunner(rdFsiHost.requestNewFsiSessionInfo.sync(RdVoid), this)
        runner.initAndRun()

        this.fsiConsole = runner
    }
}
