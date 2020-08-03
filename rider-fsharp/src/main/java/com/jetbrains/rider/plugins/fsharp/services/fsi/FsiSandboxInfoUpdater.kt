package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.execution.process.ProcessAdapter
import com.intellij.execution.process.ProcessEvent
import com.intellij.execution.process.ProcessOutputTypes
import com.intellij.openapi.editor.event.EditorFactoryEvent
import com.intellij.openapi.editor.event.EditorFactoryListener
import com.intellij.openapi.editor.ex.EditorEx
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Key
import com.jetbrains.rdclient.editors.FrontendTextControlHost
import com.jetbrains.rdclient.editors.sandboxes.SandboxManager
import com.jetbrains.rd.platform.util.application
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rdclient.lang.toRdLanguageOrThrow
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rdclient.util.idea.fromOffset
import com.jetbrains.rider.editors.RiderTextControlHost
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpScriptLanguage
import com.jetbrains.rider.model.*
import com.jetbrains.rider.projectView.solution
import org.jetbrains.concurrency.AsyncPromise

class FsiSandboxInfoUpdater(project: Project, private val consoleEditor: EditorEx, private val history: CommandHistory)
    : LifetimedProjectComponent(project) {

    private val rdFsiTools = project.solution.rdFSharpModel.fSharpInteractiveHost.fsiTools

    private val lockObject = Object()

    private var processLifetimeDefinition: LifetimeDefinition? = null
    private var processLifetime: Lifetime? = null

    val fsiProcessOutputListener = FsiSandboxInfoUpdaterProcessOutputListener(this)

    private var verifiedCommandNumber = 0
    private val preparedCommands = mutableListOf<String>()
    private val correctCommandNumbers = mutableListOf<Int>()

    private fun updateSandboxInfo() {
        application.invokeLater {
            val sandboxManager = SandboxManager.getInstance()
            if (sandboxManager.getSandboxInfo(consoleEditor) == null) return@invokeLater

            val startUnpreparedCommandIndex = preparedCommands.size
            val endUnpreparedCommandIndex = correctCommandNumbers.size

            val unpreparedCommands = mutableListOf<String>()
            for (i in correctCommandNumbers.subList(startUnpreparedCommandIndex, endUnpreparedCommandIndex)) {
                unpreparedCommands.add(history.entries[i - 1].visibleText)
            }

            synchronized(lockObject)
            {
                if (processLifetime == null) return@invokeLater

                val result = AsyncPromise<List<String>>()
                rdFsiTools.prepareCommands.start(componentLifetime, RdFsiPrepareCommandsArgs(startUnpreparedCommandIndex, unpreparedCommands)).result.advise(processLifetime!!) {
                    result.setResult(it.unwrap())
                }

                result.onSuccess { preparedAdditionalCommands ->
                    preparedCommands.addAll(preparedAdditionalCommands)

                    val sandboxText = preparedCommands.joinToString("\n").replace("\r\n", "\n") + "\ndo ()\n\n"
                    val sandboxInfo = createFSharpSandbox(sandboxText, false, emptyList())

                    sandboxManager.markAsSandbox(consoleEditor, sandboxInfo)
                    FrontendTextControlHost.getInstance(project).rebindEditor(consoleEditor)
                }
            }
        }
    }

    fun onOutputEnd() {
        if (fsiProcessOutputListener.lastOutputType != ProcessOutputTypes.SYSTEM) {
            verifiedCommandNumber += 1
            if (fsiProcessOutputListener.lastOutputType == ProcessOutputTypes.STDOUT) {
                correctCommandNumbers.add(verifiedCommandNumber)

                updateSandboxInfo()
            }
        }

        fsiProcessOutputListener.lastOutputType = ProcessOutputTypes.STDERR
    }

    class FsiSandboxInfoUpdaterProcessOutputListener(private val fsiSandboxInfoUpdater: FsiSandboxInfoUpdater) : ProcessAdapter() {
        var lastOutputType: Key<*> = ProcessOutputTypes.SYSTEM

        override fun onTextAvailable(event: ProcessEvent, outputType: Key<*>) {
            if (outputType == ProcessOutputTypes.STDOUT && lastOutputType != ProcessOutputTypes.SYSTEM)
                lastOutputType = ProcessOutputTypes.STDOUT
        }

        override fun startNotified(event: ProcessEvent) {
            fsiSandboxInfoUpdater.processLifetimeDefinition = LifetimeDefinition()
            fsiSandboxInfoUpdater.processLifetime = fsiSandboxInfoUpdater.processLifetimeDefinition!!.lifetime
        }

        override fun processTerminated(event: ProcessEvent) {
            fsiSandboxInfoUpdater.processLifetimeDefinition!!.terminate()
        }
    }
}

fun withGenericSandBoxing(sandboxInfo: SandboxInfo, project: Project, block: () -> Unit) {
    application.assertIsDispatchThread()

    val textControlHost = RiderTextControlHost.getInstance(project)

    var localInfo: SandboxInfo? = sandboxInfo
    Lifetime.using { lt ->
        textControlHost.addPrioritizedEditorFactoryListener(lt, object : EditorFactoryListener {
            override fun editorReleased(event: EditorFactoryEvent) {
            }

            override fun editorCreated(event: EditorFactoryEvent) {
                if (localInfo != null)
                    SandboxManager.getInstance().markAsSandbox(event.editor, sandboxInfo)
            }
        })

        block()
        localInfo = null
    }
}

fun createFSharpSandbox(additionalText: String = "", isNonUserCode: Boolean = false, disableTypingAssists: List<Char>): SandboxInfo {
    return SandboxInfo(
            null,
            additionalText,
            RdTextRange.fromOffset(additionalText.length),
            isNonUserCode,
            ExtraInfo(emptyList(), emptyList()),
            emptyList(),
            true,
            emptyList(),
            FSharpScriptLanguage.toRdLanguageOrThrow(),
            addSemicolon = false,
            disableTypingActions = true,
            disableTypingAssists = disableTypingAssists
    )
}
