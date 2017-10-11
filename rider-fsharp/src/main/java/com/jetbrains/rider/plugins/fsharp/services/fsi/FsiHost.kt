package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.psi.PsiFile
import com.jetbrains.rider.model.RdFSharpInteractiveHost
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.util.idea.ILifetimedComponent
import com.jetbrains.rider.util.idea.LifetimedComponent
import kotlin.properties.Delegates

class FsiHost(val project: Project) : ILifetimedComponent by LifetimedComponent(project) {
    val rdFsiHost: RdFSharpInteractiveHost get() = project.solution.fSharpInteractiveHost
    var moveCaretOnSendLine by Delegates.notNull<Boolean>()
    private var fsiConsole: FsiConsoleRunner? = null

    init {
        rdFsiHost.moveCaretOnSendLine.advise(componentLifetime) { moveCaretOnSendLine = it }
    }

    internal fun sendToFsi(editor: Editor, file: PsiFile) = synchronized(this) {
        if (fsiConsole?.isValid() == true)
            fsiConsole!!.sendActionExecutor.execute(editor, file)
        else
            createConsoleRunner({ it.sendActionExecutor.execute(editor, file) })
    }

    internal fun resetFsiConsole() = synchronized(this) {
        if (fsiConsole?.isValid() == true) {
            fsiConsole!!.processHandler.destroyProcess()
        }
        createConsoleRunner()
    }

    private fun createConsoleRunner(initialAction: ((FsiConsoleRunner) -> Unit)? = null) {
        rdFsiHost.requestNewFsiSessionInfo.start(Unit).result.advise(componentLifetime, {
            val runner = FsiConsoleRunner(it.unwrap(), this)
            runner.initAndRun()
            this.fsiConsole = runner

            if (initialAction != null)
                initialAction(runner)
        })
    }
}
