package com.jetbrains.resharper.plugins.fsharp.services.fsi

import com.intellij.execution.ExecutionManager
import com.intellij.execution.Executor
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.console.BasicGutterContentProvider
import com.intellij.execution.console.LanguageConsoleBuilder
import com.intellij.execution.console.LanguageConsoleView
import com.intellij.execution.console.ProcessBackedConsoleExecuteActionHandler
import com.intellij.execution.process.OSProcessHandler
import com.intellij.execution.runners.AbstractConsoleRunnerWithHistory
import com.intellij.execution.ui.RunContentDescriptor
import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.ex.util.EditorUtil
import com.intellij.openapi.fileTypes.PlainTextLanguage
import com.intellij.openapi.wm.impl.ToolWindowManagerImpl
import com.intellij.util.containers.ContainerUtil
import com.jetbrains.rider.model.RdFsiSendTextRequest
import com.jetbrains.rider.model.RdFsiSessionInfo
import java.io.File
import javax.swing.Icon
import kotlin.properties.Delegates

class FsiConsoleRunner(sessionInfo: RdFsiSessionInfo, val host: FsiHost)
    : AbstractConsoleRunnerWithHistory<LanguageConsoleView>(host.project, fsiTitle, null) {

    companion object {
        val fsiTitle = "F# Interactive"
    }

    val cmdLine = GeneralCommandLine().withExePath(sessionInfo.fsiPath).withParameters(sessionInfo.args)
    private var contentDescriptor by Delegates.notNull<RunContentDescriptor>()

    fun isValid(): Boolean = !(processHandler?.isProcessTerminated ?: true)

    fun sendText(request: RdFsiSendTextRequest) {
        val stream = processHandler.processInput ?: error("Broken Fsi stream")
        val bytes = request.fsiText.toByteArray(Charsets.UTF_8)
        stream.write(bytes)
        stream.flush()
        EditorUtil.scrollToTheEnd(consoleView.historyViewer)

        // show the window without getting focus
        ToolWindowManagerImpl.getInstance(project).getToolWindow(executor.id).show(null)
        ExecutionManager.getInstance(project).contentManager.selectRunContent(contentDescriptor)
    }

    override fun createProcess(): Process? {
        if (!File(cmdLine.exePath).exists()) return null // todo: dialog with a link to settings
        return cmdLine.createProcess()
    }

    override fun createExecuteActionHandler(): ProcessBackedConsoleExecuteActionHandler {
        return ProcessBackedConsoleExecuteActionHandler(processHandler, false)
    }

    override fun getConsoleIcon() = com.jetbrains.resharper.icons.ReSharperProjectModelIcons.Fsharp

    override fun fillToolBarActions(toolbarActions: DefaultActionGroup, defaultExecutor: Executor,
                                    contentDescriptor: RunContentDescriptor): MutableList<AnAction> {
        this.contentDescriptor = contentDescriptor
        contentDescriptor.icon
        val actionList = ContainerUtil.newArrayList<AnAction>(
                createCloseAction(defaultExecutor, contentDescriptor),
                ResetFsiAction(this.host))
        toolbarActions.addAll(actionList)
        actionList.add(createConsoleExecAction(consoleExecuteActionHandler))
        return actionList
    }

    override fun createConsoleView(): LanguageConsoleView {
        val gutterProvider = object : BasicGutterContentProvider() {
            override fun beforeEvaluate(e: Editor) = Unit
        }
        return LanguageConsoleBuilder().gutterContentProvider(gutterProvider).build(project, PlainTextLanguage.INSTANCE)
    }

    override fun createProcessHandler(process: Process): OSProcessHandler {
        return object : OSProcessHandler(process, cmdLine.commandLineString, Charsets.UTF_8) {
            override fun isSilentlyDestroyOnClose(): Boolean = true
        }
    }
}

class ResetFsiAction(private val host: FsiHost) : AnAction("Reset F# Interactive", null, AllIcons.Actions.Restart) {
    override fun actionPerformed(e: AnActionEvent) = host.resetRunner()
}