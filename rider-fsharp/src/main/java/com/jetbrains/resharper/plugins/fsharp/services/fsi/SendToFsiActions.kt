package com.jetbrains.resharper.plugins.fsharp.services.fsi

import com.intellij.codeInsight.intention.BaseElementAtCaretIntentionAction
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.components.ServiceManager
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.psi.PsiElement
import com.intellij.util.DocumentUtil
import com.jetbrains.resharper.ideaInterop.fileTypes.fsharp.FSharpLanguage
import com.jetbrains.rider.model.RdFsiSendTextRequest

class StartFsiAction : AnAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val project = CommonDataKeys.PROJECT.getData(e.dataContext) ?: return
        ServiceManager.getService(project, FsiHost::class.java).resetRunner()
    }
}

class SendToFsiAction : AnAction() {
    companion object {
        val sendLineText = "Send line to F# Interactive"
        val sendSelectionText = "Send selection to F# Interactive"
    }

    override fun actionPerformed(p0: AnActionEvent?) {
    }

    override fun update(e: AnActionEvent) {
        val file = CommonDataKeys.PSI_FILE.getData(e.dataContext)
        if (file?.language !is FSharpLanguage) {
            e.presentation.isEnabled = false
            return
        }
        val editor = CommonDataKeys.EDITOR.getData(e.dataContext)
        if (editor == null || editor.caretModel.caretCount != 1) {
            e.presentation.isEnabled = false
            return
        }
        e.presentation.isEnabled = true
        e.presentation.text = if (editor.selectionModel.hasSelection()) sendSelectionText else sendLineText
    }

}

abstract class BaseSendToFsiAction : BaseElementAtCaretIntentionAction() {
    override fun getFamilyName(): String = "Send to F# Interactive"
    override fun startInWriteAction() = false

    override fun isAvailable(project: Project, editor: Editor?, file: PsiElement) =
            file.language is FSharpLanguage && editor?.caretModel?.caretCount == 1

    override fun invoke(project: Project, editor: Editor, element: PsiElement) {
        val file = element.containingFile
        val visibleText = getTextToSend(editor)
        val fsiText = "\n" +
                "# silentCd @\"${file.containingDirectory.virtualFile.path}\" ;; \n" +
                "# ${getTextStartLine(editor)} @\"${file.virtualFile.path}\" \n" +
                visibleText + "\n" +
                "# 1 \"stdin\"\n;;\n"
        ServiceManager.getService(project, FsiHost::class.java).sendText(RdFsiSendTextRequest(visibleText, fsiText))
    }

    abstract fun getTextToSend(editor: Editor): String
    abstract fun getTextStartLine(editor: Editor): Int
}

class SendLineToFsiAction : BaseSendToFsiAction() {
    override fun getText() = "Send line to F# Interactive"
    override fun isAvailable(project: Project, editor: Editor?, file: PsiElement)
            = super.isAvailable(project, editor, file) && !editor!!.selectionModel.hasSelection()

    override fun getTextToSend(editor: Editor): String {
        val document = editor.document
        return document.getText(DocumentUtil.getLineTextRange(document, editor.caretModel.logicalPosition.line))
    }

    override fun getTextStartLine(editor: Editor) = editor.caretModel.logicalPosition.line
}

class SendSelectionToFsiAction : BaseSendToFsiAction() {
    override fun getText() = "Send selection to F# Interactive"
    override fun isAvailable(project: Project, editor: Editor?, file: PsiElement)
            = super.isAvailable(project, editor, file) && editor!!.selectionModel.hasSelection()

    override fun getTextToSend(editor: Editor) = editor.selectionModel.selectedText!!
    override fun getTextStartLine(editor: Editor) = editor.document.getLineNumber(editor.selectionModel.selectionStart)
}