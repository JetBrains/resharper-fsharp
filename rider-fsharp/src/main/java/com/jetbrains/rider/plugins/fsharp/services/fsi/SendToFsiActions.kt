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
import com.intellij.psi.PsiFile
import com.jetbrains.rd.platform.util.getComponent
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguageBase
import com.jetbrains.rider.plugins.fsharp.FSharpBundle
import icons.ReSharperIcons
import javax.swing.Icon

object Fsi {
  const val sendToFsiActionId = "Rider.Plugins.FSharp.SendToFsi"
  const val debugInFsiActionId = "Rider.Plugins.FSharp.DebugInFsi"

  val sendLineText = FSharpBundle.message("Fsi.actions.send.line.text")
  val debugLineText = FSharpBundle.message("Fsi.actions.debug.line.text")

  val sendSelectionText = FSharpBundle.message("Fsi.actions.send.selection.text")
  val debugSelectionText = FSharpBundle.message("Fsi.actions.debug.selection.text")
}

class StartFsiAction : AnAction() {
  override fun actionPerformed(e: AnActionEvent) {
    val project = CommonDataKeys.PROJECT.getData(e.dataContext) ?: return
    project.getComponent<FsiHost>().resetFsiConsole(false)
  }
}

class SendToFsiAction : SendToFsiActionBase(false, Fsi.sendLineText, Fsi.sendSelectionText)

class DebugInFsiAction : SendToFsiActionBase(true, Fsi.debugLineText, Fsi.debugSelectionText)

open class SendToFsiActionBase(
  private val debug: Boolean, private val sendLineText: String,
  private val sendSelectionText: String
) : AnAction(), DumbAware {
  override fun getActionUpdateThread() = ActionUpdateThread.BGT

  override fun actionPerformed(e: AnActionEvent) {
    val editor = CommonDataKeys.EDITOR.getData(e.dataContext)!!
    val file = CommonDataKeys.PSI_FILE.getData(e.dataContext)!!
    val project = CommonDataKeys.PROJECT.getData(e.dataContext) ?: return
    project.getComponent(FsiHost::class.java).sendToFsi(editor, file, debug)
  }

  override fun update(e: AnActionEvent) {
    if (debug && !SystemInfo.isWindows) {
      // todo: enable when we can read needed metadata on Mono, RIDER-7148
      e.presentation.isEnabledAndVisible = false
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

class SendLineToFsiIntentionAction : SendLineToFsiIntentionActionBase(false, Fsi.sendLineText, Fsi.sendToFsiActionId),
  HighPriorityAction

class DebugLineInFsiIntentionAction : SendLineToFsiIntentionActionBase(true, Fsi.debugLineText, Fsi.debugInFsiActionId)

class SendSelectionToFsiIntentionAction :
  SendSelectionToFsiIntentionActionBase(false, Fsi.sendSelectionText, Fsi.sendToFsiActionId), HighPriorityAction

class DebugSelectionInFsiIntentionAction :
  SendSelectionToFsiIntentionActionBase(true, Fsi.debugSelectionText, Fsi.debugInFsiActionId)

open class SendLineToFsiIntentionActionBase(debug: Boolean, private val titleText: String, actionId: String) :
  BaseSendToFsiIntentionAction(debug, actionId) {
  override fun getText() = titleText
  override fun isAvailable(project: Project, editor: Editor, file: PsiElement) =
    super.isAvailable(project, editor, file) && !editor.selectionModel.hasSelection()
}

open class SendSelectionToFsiIntentionActionBase(debug: Boolean, private val titleText: String, actionId: String) :
  BaseSendToFsiIntentionAction(debug, actionId) {
  override fun getText() = titleText
  override fun isAvailable(project: Project, editor: Editor, file: PsiElement) =
    super.isAvailable(project, editor, file) && editor.selectionModel.hasSelection()
}

abstract class BaseSendToFsiIntentionAction(private val debug: Boolean, private val actionId: String) :
  BaseElementAtCaretIntentionAction(), ShortcutProvider, Iconable, DumbAware {
  private val isAvailable = !debug || SystemInfo.isWindows

  override fun getFamilyName(): String = FSharpBundle.message("Fsi.actions.send.to.fsi.intention.action.text")
  override fun startInWriteAction() = false

  override fun isAvailable(project: Project, editor: Editor, file: PsiElement) =
    isAvailable && editor.caretModel.caretCount == 1

  override fun checkFile(file: PsiFile) = file.language is FSharpLanguageBase

  override fun invoke(project: Project, editor: Editor, element: PsiElement) {
    project.getComponent<FsiHost>().sendToFsi(editor, element.containingFile, debug)
  }

  override fun getIcon(flags: Int): Icon = ReSharperIcons.Bulb.GhostBulb

  override fun getShortcut() =
    ActionManager.getInstance().getAction(actionId)?.shortcutSet
}
