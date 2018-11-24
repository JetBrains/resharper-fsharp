package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.psi.PsiFile
import com.jetbrains.rider.model.RdFSharpInteractiveHost
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.util.idea.LifetimedProjectComponent
import kotlin.properties.Delegates

class FsiHost(project: Project) : LifetimedProjectComponent(project) {
    private val rdFsiHost: RdFSharpInteractiveHost get() = project.solution.fSharpInteractiveHost
    var moveCaretOnSendLine by Delegates.notNull<Boolean>()
    var copyRecentToEditor by Delegates.notNull<Boolean>()
    private var fsiConsole: FsiConsoleRunner? = null

    init {
        rdFsiHost.moveCaretOnSendLine.advise(componentLifetime) { moveCaretOnSendLine = it }
        rdFsiHost.copyRecentToEditor.advise(componentLifetime) { copyRecentToEditor = it }
    }

    internal fun sendToFsi(editor: Editor, file: PsiFile, debug: Boolean) = synchronized(this) {
        execute { it.sendActionExecutor.execute(editor, file, debug) }
    }

    internal fun sendToFsi(visibleText: String, fsiText: String, debug: Boolean) = synchronized(this) {
        execute { it.sendText(visibleText, fsiText, debug) }
    }

    private fun execute(action: (FsiConsoleRunner) -> Unit) {
        if (fsiConsole?.isValid() == true) action(fsiConsole!!)
        else createConsoleRunner { action(it) }
    }

    internal fun resetFsiConsole() = synchronized(this) {
        if (fsiConsole?.isValid() == true) fsiConsole!!.processHandler.destroyProcess()
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
