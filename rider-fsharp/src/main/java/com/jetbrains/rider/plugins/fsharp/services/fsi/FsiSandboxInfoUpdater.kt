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
import com.jetbrains.rdclient.lang.toRdLanguageOrThrow
import com.jetbrains.rdclient.util.idea.fromOffset
import com.jetbrains.rider.editors.RiderTextControlHost
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpScriptLanguage
import com.jetbrains.rider.model.ExtraInfo
import com.jetbrains.rider.model.RdTextRange
import com.jetbrains.rider.model.SandboxInfo

class FsiSandboxInfoUpdater(
        private val project: Project, private val consoleEditor: EditorEx, private val history: CommandHistory) {

    val fsiProcessOutputListener = FsiProcessOutputListener()
    private var verifiedCommandNumber = 0

    private val correctCommandNumbers = mutableListOf<Int>()

    private fun updateSandboxInfo() {
        application.invokeLater {
            val sandboxManager = SandboxManager.getInstance()

            if (sandboxManager.getSandboxInfo(consoleEditor) == null) return@invokeLater

            val additionalTextToInsert = StringBuilder()
            for (i in correctCommandNumbers)
                additionalTextToInsert.appendln(history.entries[i - 1].visibleText)

            val sandboxInfo = genericFSharpSandboxInfoWithCustomParams(
                    additionalTextToInsert.toString().replace("\r\n", "\n"),
                    false,
                    emptyList()
            )
            sandboxManager.markAsSandbox(consoleEditor, sandboxInfo)
            FrontendTextControlHost.getInstance(project).rebindEditor(consoleEditor)
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

    class FsiProcessOutputListener : ProcessAdapter() {
        var lastOutputType : Key<*> = ProcessOutputTypes.SYSTEM

        override fun onTextAvailable(event: ProcessEvent, outputType: Key<*>) {
            if (outputType == ProcessOutputTypes.STDOUT && lastOutputType != ProcessOutputTypes.SYSTEM)
                lastOutputType = ProcessOutputTypes.STDOUT
        }
    }
}

fun withGenericSandBoxing(sandboxInfo: SandboxInfo, project: Project, block: () -> Unit) {
    application.assertIsDispatchThread()

    val textControlHost = RiderTextControlHost.getInstance(project)

    var localInfo : SandboxInfo? = sandboxInfo
    Lifetime.using { lt ->
        textControlHost.addPrioritizedEditorFactoryListener(lt, object: EditorFactoryListener {
            override fun editorReleased(event: EditorFactoryEvent) {
            }

            override fun editorCreated(event: EditorFactoryEvent) {
                if(localInfo != null)
                    SandboxManager.getInstance().markAsSandbox(event.editor, sandboxInfo)
            }
        })

        block()
        localInfo = null
    }
}

fun genericFSharpSandboxInfoWithCustomParams(additionalText: String = "", isNonUserCode: Boolean = false, disableTypingAssists : List<Char>): SandboxInfo {
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