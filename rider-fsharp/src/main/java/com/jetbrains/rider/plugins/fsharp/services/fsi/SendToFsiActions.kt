package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.codeInsight.intention.BaseElementAtCaretIntentionAction
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.components.ServiceManager
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.SystemInfo
import com.intellij.psi.PsiElement
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguageBase

object Fsi {
    const val sendLineText = "Send Line to F# Interactive"
    const val debugLineText = "Debug Line in F# Interactive"

    const val sendSelectionText = "Send Selection to F# Interactive"
    const val debugSelectionText = "Debug Selection in F# Interactive"
}

class StartFsiAction : AnAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val project = CommonDataKeys.PROJECT.getData(e.dataContext) ?: return
        ServiceManager.getService(project, FsiHost::class.java).resetFsiConsole(false)
    }
}

class SendToFsiAction : SendToFsiActionBase(false, Fsi.sendLineText, Fsi.sendSelectionText)

class DebugInFsiAction : SendToFsiActionBase(true, Fsi.debugLineText, Fsi.debugSelectionText)

open class SendToFsiActionBase(private val debug: Boolean, private val sendLineText: String,
                               private val sendSelectionText: String) : AnAction() {

    override fun actionPerformed(e: AnActionEvent) {
        val editor = CommonDataKeys.EDITOR.getData(e.dataContext)!!
        val file = CommonDataKeys.PSI_FILE.getData(e.dataContext)!!
        val project = CommonDataKeys.PROJECT.getData(e.dataContext) ?: return
        ServiceManager.getService(project, FsiHost::class.java).sendToFsi(editor, file, debug)
    }

    override fun update(e: AnActionEvent) {
        if (debug && !SystemInfo.isWindows) {
            // todo: enable when we can read needed metadata on Mono, RIDER-7148
            e.presentation.isEnabled = false
            e.presentation.isVisible = false
            return
        }
        val file = CommonDataKeys.PSI_FILE.getData(e.dataContext)
        val editor = CommonDataKeys.EDITOR.getData(e.dataContext)
        if (file?.language !is FSharpLanguageBase || editor?.caretModel?.caretCount != 1) {
            e.presentation.isEnabled = false
            return
        }
        e.presentation.isEnabled = true
        e.presentation.text = if (editor.selectionModel.hasSelection()) sendSelectionText else sendLineText
    }
}

class SendLineToFsiIntentionAction : SendLineToFsiIntentionActionBase(false, Fsi.sendLineText)
class DebugLineInFsiIntentionAction : SendLineToFsiIntentionActionBase(true, Fsi.debugLineText)

class SendSelectionToFsiIntentionAction : SendSelectionToFsiIntentionActionBase(false, Fsi.sendSelectionText)
class DebugSelectionInFsiIntentionAction : SendSelectionToFsiIntentionActionBase(true, Fsi.debugSelectionText)

open class SendLineToFsiIntentionActionBase(debug: Boolean, private val titleText: String) : BaseSendToFsiIntentionAction(debug) {
    override fun getText() = titleText
    override fun isAvailable(project: Project, editor: Editor?, file: PsiElement) =
            super.isAvailable(project, editor, file) && !editor!!.selectionModel.hasSelection()
}

open class SendSelectionToFsiIntentionActionBase(debug: Boolean, private val titleText: String) : BaseSendToFsiIntentionAction(debug) {
    override fun getText() = titleText
    override fun isAvailable(project: Project, editor: Editor?, file: PsiElement) =
            super.isAvailable(project, editor, file) && editor!!.selectionModel.hasSelection()
}

abstract class BaseSendToFsiIntentionAction(private val debug: Boolean) : BaseElementAtCaretIntentionAction() {
    override fun getFamilyName(): String = "Send to F# Interactive"
    override fun startInWriteAction() = false

    override fun isAvailable(project: Project, editor: Editor?, file: PsiElement) =
            file.language is FSharpLanguageBase && editor?.caretModel?.caretCount == 1

    override fun invoke(project: Project, editor: Editor, element: PsiElement) {
        ServiceManager.getService(project, FsiHost::class.java).sendToFsi(editor, element.containingFile, debug)
    }
}
