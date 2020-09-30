package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.notification.Notification
import com.intellij.notification.NotificationType
import com.intellij.notification.Notifications
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.io.FileUtil
import com.intellij.psi.PsiFile
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.model.RdFsiSessionInfo
import com.jetbrains.rider.model.rdFSharpModel
import com.jetbrains.rider.plugins.fsharp.FSharpIcons
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rd.util.reactive.flowInto
import com.jetbrains.rider.model.RdFsiRuntime
import org.jetbrains.concurrency.AsyncPromise
import org.jetbrains.concurrency.Promise

class FsiHost(project: Project) : LifetimedProjectComponent(project) {
    private val rdFsiHost = project.solution.rdFSharpModel.fSharpInteractiveHost

    val moveCaretOnSendLine = Property(true)
    val moveCaretOnSendSelection = Property(true)
    val copyRecentToEditor = Property(false)

    init {
        rdFsiHost.moveCaretOnSendLine.flowInto(componentLifetime, moveCaretOnSendLine)
        rdFsiHost.moveCaretOnSendSelection.flowInto(componentLifetime, moveCaretOnSendSelection)
        rdFsiHost.copyRecentToEditor.flowInto(componentLifetime, copyRecentToEditor)
    }

    var consoleRunner: FsiConsoleRunner? = null
        private set

    private val lockObj = Object()

    fun sendToFsi(editor: Editor, file: PsiFile, debug: Boolean) {
        getOrCreateConsoleRunner(debug).onSuccess {
            it.sendActionExecutor.execute(editor, file, debug)
        }
    }

    fun sendToFsi(visibleText: String, fsiText: String, debug: Boolean) {
        getOrCreateConsoleRunner(debug).onSuccess {
            it.sendText(visibleText, fsiText, debug)
        }
    }

    fun resetFsiConsole(forceOptimizeForDebug: Boolean, attach: Boolean = false) {
        synchronized(lockObj) {
            if (consoleRunner?.isValid() == true) {
                consoleRunner!!.processHandler.destroyProcess()
            }
            tryCreateConsoleRunner(forceOptimizeForDebug, attach)
        }
    }

    private fun getOrCreateConsoleRunner(forceOptimizeForDebug: Boolean): Promise<FsiConsoleRunner> {
        if (consoleRunner?.isValid() == true) {
            val result = AsyncPromise<FsiConsoleRunner>()
            result.setResult(consoleRunner!!)
            return result
        }

        return tryCreateConsoleRunner(forceOptimizeForDebug)
    }

    private fun createConsoleRunner(sessionInfo: RdFsiSessionInfo, forceOptimizeForDebug: Boolean, attach: Boolean): FsiConsoleRunner? =
            synchronized(lockObj) {
                // Might have already been created.
                if (consoleRunner?.isValid() == true)
                    return consoleRunner

                val fsiPath = sessionInfo.fsiPath
                if (sessionInfo.runtime != RdFsiRuntime.Core && !FileUtil.exists(fsiPath)) {
                    notifyFsiNotFound(fsiPath)
                    return null
                }

                val runner = FsiConsoleRunner(sessionInfo, this, forceOptimizeForDebug)
                runner.initAndRun()
                this.consoleRunner = runner

                if (attach)
                    runner.attachToProcess()

                return runner
            }

    private fun tryCreateConsoleRunner(forceOptimizeForDebug: Boolean, attach: Boolean = false): Promise<FsiConsoleRunner> {
        val result = AsyncPromise<FsiConsoleRunner>()
        rdFsiHost.requestNewFsiSessionInfo.start(componentLifetime, Unit).result.advise(componentLifetime) {
            val consoleRunner = createConsoleRunner(it.unwrap(), forceOptimizeForDebug, attach)
            if (consoleRunner != null)
                result.setResult(consoleRunner)
        }
        return result
    }

    private fun notifyFsiNotFound(fsiPath: String) {
        val title = "Could not start F# Interactive"
        val content = "The file '$fsiPath' was not found."
        val notification = Notification(FsiConsoleRunner.fsiTitle, title, content, NotificationType.WARNING)
        notification.icon = FSharpIcons.FSharpConsole
        Notifications.Bus.notify(notification, project)
    }
}
