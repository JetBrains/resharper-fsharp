package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.notification.Notification
import com.intellij.notification.NotificationType
import com.intellij.notification.Notifications
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.io.FileUtil
import com.intellij.psi.PsiFile
import com.jetbrains.rider.model.RdFSharpInteractiveHost
import com.jetbrains.rider.model.rdFSharpModel
import com.jetbrains.rider.plugins.fsharp.FSharpIcons
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.util.idea.LifetimedProjectComponent
import kotlin.properties.Delegates

class FsiHost(project: Project) : LifetimedProjectComponent(project) {
    private val rdFsiHost: RdFSharpInteractiveHost get() = project.solution.rdFSharpModel.fSharpInteractiveHost
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

    private fun notifyFsiNotFound(fsiPath: String) {
        val title = "Could not start F# Interactive"
        val content = "The file '$fsiPath' was not found."
        val notification = Notification(FsiConsoleRunner.fsiTitle, title, content, NotificationType.WARNING)
        notification.icon = FSharpIcons.FSharpConsole
        Notifications.Bus.notify(notification, project)
    }

    private fun createConsoleRunner(initialAction: ((FsiConsoleRunner) -> Unit)? = null) {
        rdFsiHost.requestNewFsiSessionInfo.start(Unit).result.advise(componentLifetime) {
            val sessionInfo = it.unwrap()
            if (!FileUtil.exists(sessionInfo.fsiPath)) {
                notifyFsiNotFound(sessionInfo.fsiPath)
                return@advise
            }

            val runner = FsiConsoleRunner(sessionInfo, this)
            runner.initAndRun()
            this.fsiConsole = runner

            if (initialAction != null)
                initialAction(runner)
        }
    }
}
