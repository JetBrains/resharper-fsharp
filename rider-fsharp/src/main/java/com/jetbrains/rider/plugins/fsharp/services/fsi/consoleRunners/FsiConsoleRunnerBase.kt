package com.jetbrains.rider.plugins.fsharp.services.fsi.consoleRunners

import com.intellij.execution.Executor
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.console.LanguageConsoleBuilder
import com.intellij.execution.console.LanguageConsoleView
import com.intellij.execution.console.ProcessBackedConsoleExecuteActionHandler
import com.intellij.execution.process.OSProcessHandler
import com.intellij.execution.runners.AbstractConsoleRunnerWithHistory
import com.intellij.execution.ui.ConsoleViewContentType
import com.intellij.execution.ui.RunContentDescriptor
import com.intellij.execution.ui.RunContentManager
import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.editor.colors.EditorColors
import com.intellij.openapi.options.ShowSettingsUtil
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.util.lifetime
import com.intellij.openapi.wm.ToolWindowManager
import com.intellij.ui.Gray
import com.intellij.ui.JBColor
import com.intellij.util.ui.UIUtil
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguage
import com.jetbrains.rider.plugins.fsharp.FSharpBundle
import com.jetbrains.rider.plugins.fsharp.FSharpIcons
import com.jetbrains.rider.plugins.fsharp.services.fsi.*
import org.jetbrains.annotations.Nls
import javax.swing.BorderFactory

abstract class FsiConsoleRunnerBase(
    project: Project,
    @Nls consoleTitle: String,
    private val commandLine: GeneralCommandLine
) :
    AbstractConsoleRunnerWithHistory<LanguageConsoleView>(
        project,
        consoleTitle,
        commandLine.workDirectory.path
    ) {
    val fsiHost = FsiHost.getInstance(project)
    val commandHistory = CommandHistory()
    private val inputSeparatorGutterContentProvider = InputSeparatorGutterContentProvider(true)
    private val fsiInputOutputProcessor = FsiInputOutputProcessor(this)
    protected lateinit var contentDescriptor: RunContentDescriptor

    override fun isAutoFocusContent() = true
    override fun createProcess() = commandLine.createProcess()

    override fun createProcessHandler(process: Process): OSProcessHandler {
        val fsiProcessHandler =
            FsiProcessHandler(fsiHost.project.lifetime, fsiInputOutputProcessor, process, commandLine.commandLineString)
        val sandboxInfoUpdater = FsiSandboxInfoUpdater(fsiHost.project, consoleView.consoleEditor, commandHistory)
        fsiProcessHandler.addSandboxInfoUpdater(sandboxInfoUpdater)
        return fsiProcessHandler
    }

    override fun createConsoleView(): LanguageConsoleView {
        lateinit var consoleView: LanguageConsoleView

        withGenericSandBoxing(createFSharpSandbox("do ()\n\n", false, emptyList()), project) {
            consoleView = LanguageConsoleBuilder().gutterContentProvider(inputSeparatorGutterContentProvider)
                .build(project, FSharpLanguage)

            val consoleEditorBorder = BorderFactory.createMatteBorder(
                2, 0, 0, 0, consoleView.consoleEditor.colorsScheme.getColor(EditorColors.INDENT_GUIDE_COLOR)
            )
            consoleView.consoleEditor.component.border = consoleEditorBorder

            val historyKeyListener = HistoryKeyListener(fsiHost.project, consoleView.consoleEditor, commandHistory)
            consoleView.consoleEditor.contentComponent.addKeyListener(historyKeyListener)
            commandHistory.listeners.add(historyKeyListener)
        }
        return consoleView
    }

    override fun initAndRun() {
        super.initAndRun()
        setupGutters()
    }

    private fun setupGutters() {
        val historyEditor = consoleView.historyViewer
        historyEditor.settings.isLineMarkerAreaShown = true
        historyEditor.settings.isFoldingOutlineShown = true
        historyEditor.gutterComponentEx.isPaintBackground = true

        historyEditor.colorsScheme.setColor(EditorColors.GUTTER_BACKGROUND, JBColor(Gray.xF2, Gray.x41))
    }

    override fun createExecuteActionHandler(): ProcessBackedConsoleExecuteActionHandler {
        return object : ProcessBackedConsoleExecuteActionHandler(processHandler, false) {
            override fun runExecuteAction(consoleView: LanguageConsoleView) {
                val visibleText = consoleView.consoleEditor.document.text
                if (visibleText.isBlank()) return

                val fsiText = "\n$visibleText\n# 1 \"stdin\"\n;;\n"
                sendText(visibleText, fsiText)
                consoleView.setInputText("")
            }
        }
    }

    override fun getConsoleIcon() = FSharpIcons.FSharpConsole

    final override fun fillToolBarActions(
        toolbarActions: DefaultActionGroup, defaultExecutor: Executor,
        contentDescriptor: RunContentDescriptor
    ): MutableList<AnAction> {
        this.contentDescriptor = contentDescriptor
        val actionList = getToolBarActions()
        toolbarActions.add(createCloseAction(executor, contentDescriptor))
        toolbarActions.addAll(actionList)
        actionList.add(createConsoleExecAction(consoleExecuteActionHandler))
        return actionList
    }

    abstract fun getToolBarActions(): MutableList<AnAction>

    fun stop() = processHandler.destroyProcess()
    fun getRunContentDescriptor() = contentDescriptor
    fun isValid() = !processHandler.isProcessTerminated && !processHandler.isProcessTerminating

    fun sendText(visibleText: String, fsiText: String) {
        UIUtil.invokeLaterIfNeeded {
            inputSeparatorGutterContentProvider.addLineSeparator(consoleView.historyViewer.document.lineCount)

            fsiInputOutputProcessor.printInputText(visibleText, ConsoleViewContentType.USER_INPUT)

            commandHistory.addEntry(CommandHistory.Entry(visibleText, fsiText))

            // show the window without getting focus
            RunContentManager.getInstance(project).selectRunContent(contentDescriptor)
            ToolWindowManager.getInstance(project).getToolWindow(executor.id)?.show(null)

            val stream = processHandler.processInput ?: error("Broken Fsi stream")
            if (visibleText.isNotBlank()) {
                stream.write(fsiText.toByteArray(Charsets.UTF_8))
                stream.flush()
            }
        }
    }

    protected class OpenSettings(val project: Project) :
        AnAction(FSharpBundle.message("Fsi.actions.open.settings.title"), null, AllIcons.General.Settings) {
        override fun actionPerformed(e: AnActionEvent) {
            ShowSettingsUtil.getInstance().showSettingsDialog(project, "Fsi")
        }
    }
}
