package com.jetbrains.rider.plugins.fsharp.services.fsi

import com.intellij.codeInsight.intention.BaseElementAtCaretIntentionAction
import com.intellij.codeInsight.intention.HighPriorityAction
import com.intellij.openapi.actionSystem.*
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.DumbAware
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Iconable
import com.intellij.openapi.util.SystemInfo
import com.intellij.psi.PsiElement
import com.jetbrains.rd.platform.util.getComponent
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguageBase
import icons.ReSharperIcons
import javax.swing.Icon

object Fsi {
    const val sendToFsiActionId = "Rider.Plugins.FSharp.SendToFsi"
    const val debugInFsiActionId = "Rider.Plugins.FSharp.DebugInFsi"

    const val sendLineText = "Send Line to F# Interactive"
    const val debugLineText = "Debug Line in F# Interactive"

    const val sendSelectionText = "Send Selection to F# Interactive"
    const val debugSelectionText = "Debug Selection in F# Interactive"
}

class StartFsiAction : AnAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val project = CommonDataKeys.PROJECT.getData(e.dataContext) ?: return
        project.getComponent<FsiHost>().resetFsiConsole(false)
    }
}

class SendToFsiAction : SendToFsiActionBase(false, Fsi.sendLineText, Fsi.sendSelectionText)

class DebugInFsiAction : SendToFsiActionBase(true, Fsi.debugLineText, Fsi.debugSelectionText)

open class SendToFsiActionBase(private val debug: Boolean, private val sendLineText: String,
                               private val sendSelectionText: String) : AnAction(), DumbAware {

    override fun actionPerformed(e: AnActionEvent) {
        val editor = CommonDataKeys.EDITOR.getData(e.dataContext)!!
        val file = CommonDataKeys.PSI_FILE.getData(e.dataContext)!!
        val project = CommonDataKeys.PROJECT.getData(e.dataContext) ?: return
        project.getComponent(FsiHost::class.java).sendToFsi(editor, file, debug)
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

class SendLineToFsiIntentionAction : SendLineToFsiIntentionActionBase(false, Fsi.sendLineText, Fsi.sendToFsiActionId), HighPriorityAction
class DebugLineInFsiIntentionAction : SendLineToFsiIntentionActionBase(true, Fsi.debugLineText, Fsi.debugInFsiActionId)

class SendSelectionToFsiIntentionAction : SendSelectionToFsiIntentionActionBase(false, Fsi.sendSelectionText, Fsi.sendToFsiActionId), HighPriorityAction
class DebugSelectionInFsiIntentionAction : SendSelectionToFsiIntentionActionBase(true, Fsi.debugSelectionText, Fsi.debugInFsiActionId)

open class SendLineToFsiIntentionActionBase(debug: Boolean, private val titleText: String, actionId: String) : BaseSendToFsiIntentionAction(debug, actionId) {
    override fun getText() = titleText
    override fun isAvailable(project: Project, editor: Editor?, file: PsiElement) =
            super.isAvailable(project, editor, file) && !editor!!.selectionModel.hasSelection()
}

open class SendSelectionToFsiIntentionActionBase(debug: Boolean, private val titleText: String, actionId: String) : BaseSendToFsiIntentionAction(debug, actionId) {
    override fun getText() = titleText
    override fun isAvailable(project: Project, editor: Editor?, file: PsiElement) =
            super.isAvailable(project, editor, file) && editor!!.selectionModel.hasSelection()
}

abstract class BaseSendToFsiIntentionAction(private val debug: Boolean, private val actionId: String) : BaseElementAtCaretIntentionAction(), ShortcutProvider, Iconable, DumbAware {
    private val isAvailable = !debug || SystemInfo.isWindows

    override fun getFamilyName(): String = "Send to F# Interactive"
    override fun startInWriteAction() = false

    override fun isAvailable(project: Project, editor: Editor?, file: PsiElement) =
            isAvailable && file.language is FSharpLanguageBase && editor?.caretModel?.caretCount == 1

    override fun invoke(project: Project, editor: Editor, element: PsiElement) {
        project.getComponent<FsiHost>().sendToFsi(editor, element.containingFile, debug)
    }

    override fun getIcon(flags: Int): Icon = ReSharperIcons.Bulb.GhostBulb

    override fun getShortcut() =
            ActionManager.getInstance().getAction(actionId)?.shortcutSet
}
