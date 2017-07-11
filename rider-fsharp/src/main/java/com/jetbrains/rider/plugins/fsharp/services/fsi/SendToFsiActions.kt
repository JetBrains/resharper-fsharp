package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.codeInsight.intention.BaseElementAtCaretIntentionAction
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.components.ServiceManager
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.psi.PsiElement
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguage

class StartFsiAction : AnAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val project = CommonDataKeys.PROJECT.getData(e.dataContext) ?: return
        ServiceManager.getService(project, FsiHost::class.java).resetFsiConsole()
    }
}

class SendToFsiAction : AnAction() {
    companion object {
        val sendLineText = "Send Line to F# Interactive"
        val sendSelectionText = "Send Selection to F# Interactive"
    }

    override fun actionPerformed(e: AnActionEvent) {
        val editor = CommonDataKeys.EDITOR.getData(e.dataContext)!!
        val file = CommonDataKeys.PSI_FILE.getData(e.dataContext)!!
        val project = CommonDataKeys.PROJECT.getData(e.dataContext) ?: return
        ServiceManager.getService(project, FsiHost::class.java).sendToFsi(editor, file)
    }

    override fun update(e: AnActionEvent) {
        val file = CommonDataKeys.PSI_FILE.getData(e.dataContext)
        val editor = CommonDataKeys.EDITOR.getData(e.dataContext)
        if (file?.language !is FSharpLanguage || editor?.caretModel?.caretCount != 1) {
            e.presentation.isEnabled = false
            return
        }
        e.presentation.isEnabled = true
        e.presentation.text = if (editor.selectionModel.hasSelection()) sendSelectionText else sendLineText
    }
}

abstract class BaseSendToFsiIntentionAction : BaseElementAtCaretIntentionAction() {
    override fun getFamilyName(): String = "Send to F# Interactive"
    override fun startInWriteAction() = false

    override fun isAvailable(project: Project, editor: Editor?, file: PsiElement) =
            file.language is FSharpLanguage && editor?.caretModel?.caretCount == 1

    override fun invoke(project: Project, editor: Editor, element: PsiElement) {
        ServiceManager.getService(project, FsiHost::class.java).sendToFsi(editor, element.containingFile)
    }
}

class SendLineToFsiIntentionAction : BaseSendToFsiIntentionAction() {
    override fun getText() = "Send Line to F# Interactive"
    override fun isAvailable(project: Project, editor: Editor?, file: PsiElement)
            = super.isAvailable(project, editor, file) && !editor!!.selectionModel.hasSelection()
}

class SendSelectionToFsiIntentionAction : BaseSendToFsiIntentionAction() {
    override fun getText() = "Send Selection to F# Interactive"
    override fun isAvailable(project: Project, editor: Editor?, file: PsiElement)
            = super.isAvailable(project, editor, file) && editor!!.selectionModel.hasSelection()
}
