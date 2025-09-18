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
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguage
import com.jetbrains.rider.plugins.fsharp.FSharpBundle
import icons.ReSharperIcons
import org.jetbrains.annotations.Nls
import javax.swing.Icon

object Fsi {
  const val sendToFsiActionId: String = "Rider.Plugins.FSharp.SendToFsi"
  const val debugInFsiActionId: String = "Rider.Plugins.FSharp.DebugInFsi"

  val sendLineText: String = FSharpBundle.message("Fsi.actions.send.line.text")
  val debugLineText: String = FSharpBundle.message("Fsi.actions.debug.line.text")

  val sendSelectionText: String = FSharpBundle.message("Fsi.actions.send.selection.text")
  val debugSelectionText: String = FSharpBundle.message("Fsi.actions.debug.selection.text")
}

class StartFsiAction : AnAction() {
  override fun actionPerformed(e: AnActionEvent) {
    val project = CommonDataKeys.PROJECT.getData(e.dataContext) ?: return
    val fsiHost = FsiHost.getInstance(project)
    fsiHost.resetFsiDefaultConsole()
  }
}

class SendToFsiAction : SendToFsiActionBase(false, Fsi.sendLineText, Fsi.sendSelectionText)

open class SendToFsiActionBase(
  private val debug: Boolean, private val sendLineText: @Nls String,
  private val sendSelectionText: @Nls String
) : AnAction(), DumbAware {
  override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT

  override fun actionPerformed(e: AnActionEvent) {
    val editor = CommonDataKeys.EDITOR.getData(e.dataContext)!!
    val file = CommonDataKeys.PSI_FILE.getData(e.dataContext)!!
    val project = CommonDataKeys.PROJECT.getData(e.dataContext) ?: return
    val fsiHost = FsiHost.getInstance(project)
    fsiHost.sendToFsi(editor, file, debug)
  }

  override fun update(e: AnActionEvent) {
    if (debug && !SystemInfo.isWindows) {
      // todo: enable when we can read needed metadata on Mono, RIDER-7148
      e.presentation.isEnabledAndVisible = false
      return
    }
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

class SendLineToFsiIntentionAction : SendLineToFsiIntentionActionBase(false, Fsi.sendLineText, Fsi.sendToFsiActionId),
  HighPriorityAction

class SendSelectionToFsiIntentionAction :
  SendSelectionToFsiIntentionActionBase(false, Fsi.sendSelectionText, Fsi.sendToFsiActionId), HighPriorityAction

open class SendLineToFsiIntentionActionBase(debug: Boolean, private val titleText: @Nls String, actionId: String) :
  BaseSendToFsiIntentionAction(debug, actionId) {
  override fun getText(): String = titleText
  override fun isAvailable(project: Project, editor: Editor, file: PsiElement): Boolean =
    super.isAvailable(project, editor, file) && !editor.selectionModel.hasSelection()
}

open class SendSelectionToFsiIntentionActionBase(debug: Boolean, private val titleText: @Nls String, actionId: String) :
  BaseSendToFsiIntentionAction(debug, actionId) {
  override fun getText(): String = titleText
  override fun isAvailable(project: Project, editor: Editor, file: PsiElement): Boolean =
    super.isAvailable(project, editor, file) && editor.selectionModel.hasSelection()
}

abstract class BaseSendToFsiIntentionAction(private val debug: Boolean, private val actionId: String) :
  BaseElementAtCaretIntentionAction(), ShortcutProvider, Iconable, DumbAware {
  private val isAvailable = !debug || SystemInfo.isWindows

  override fun getFamilyName(): String = FSharpBundle.message("Fsi.actions.send.to.fsi.intention.action.text")
  override fun startInWriteAction(): Boolean = false

  override fun isAvailable(project: Project, editor: Editor, file: PsiElement): Boolean =
    isAvailable && editor.caretModel.caretCount == 1

  override fun checkFile(file: PsiFile): Boolean = file.language is FSharpLanguage

  override fun invoke(project: Project, editor: Editor, element: PsiElement) {
    FsiHost.getInstance(project).sendToFsi(editor, element.containingFile, debug)
  }

  override fun getIcon(flags: Int): Icon = ReSharperIcons.Bulb.GhostBulb

  override fun getShortcut(): ShortcutSet? =
    ActionManager.getInstance().getAction(actionId)?.shortcutSet
}
