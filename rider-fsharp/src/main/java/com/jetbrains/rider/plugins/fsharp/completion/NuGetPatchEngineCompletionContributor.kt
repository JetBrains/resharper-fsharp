package com.jetbrains.rider.plugins.fsharp.completion

import com.intellij.codeInsight.completion.CompletionInitializationContext
import com.intellij.openapi.editor.Editor
import com.intellij.psi.ElementManipulators
import com.intellij.psi.PsiFile
import com.intellij.psi.util.startOffset
import com.jetbrains.rdclient.document.editorSynchronizer
import com.jetbrains.rdclient.patches.isPatchEngineEnabled
import com.jetbrains.rider.completion.patchEngine.RiderPatchEngineCompletionContributor
import com.jetbrains.rider.completion.patchEngine.RiderPatchEngineProtocolProvider
import com.jetbrains.rider.completion.patchEngine.isPreemptiveCompletionEnabled
import com.jetbrains.rider.editors.startOffset

class NuGetPatchEngineCompletionContributor : RiderPatchEngineCompletionContributor() {

  override fun isAvailable(file: PsiFile, offset: Int): Boolean = insideReferenceDirective(file, offset)

  override fun beforeCompletion(context: CompletionInitializationContext) {
    if (!isPatchEngineEnabled || !isPreemptiveCompletionEnabled)
      return

    val psiElement = getStringInsideReferenceDirective(context.file, context.startOffset) ?: return

    val stringRange = ElementManipulators.getValueTextRange(psiElement)
    val stringContentRange = psiElement.startOffset + stringRange.startOffset

    val startOffset = context.editor.startOffset - stringContentRange
    context.replacementOffset = psiElement.startOffset + stringRange.endOffset

    val stringText = ElementManipulators.getValueText(psiElement)
    val match = PACKAGE_REFERENCE_REGEX.find(stringText) ?: return

    val `package` = match.groups[GROUP_PACKAGE]!!
    val packageZone = match.groups[GROUP_PACKAGE_ZONE]!!
    val version = match.groups[GROUP_VERSION]
    val versionZone = match.groups[GROUP_VERSION_ZONE]

    prepareCustomParams(stringText, startOffset, KEY_NAME, `package`, packageZone, context.editor)
    || prepareCustomParams(stringText, startOffset, "$KEY_VERSION|${`package`.value}", version!!, versionZone!!, context.editor)
  }

  private fun prepareCustomParams(
    content: String,
    cursorPosition: Int,
    host: String,
    strictGroup: MatchGroup,
    zoneGroup: MatchGroup,
    editor: Editor
  ): Boolean {
    if (containsExclusive(strictGroup.range, cursorPosition)) {
      val completionPrefix = content.substring(strictGroup.range.first, cursorPosition)
      RiderPatchEngineProtocolProvider.getInstance().customHostDocumentVersion = editor.editorSynchronizer?.version
      RiderPatchEngineProtocolProvider.getInstance().customHost = host
      RiderPatchEngineProtocolProvider.getInstance().customPrefix = completionPrefix
      return true
    }
    else if (containsExclusive(zoneGroup.range, cursorPosition)) {
      RiderPatchEngineProtocolProvider.getInstance().customHostDocumentVersion = editor.editorSynchronizer?.version
      RiderPatchEngineProtocolProvider.getInstance().customHost = host
      RiderPatchEngineProtocolProvider.getInstance().customPrefix = ""
      return true
    }

    return false
  }

  private fun containsExclusive(range: IntRange, value: Int) = value >= range.first && value <= range.last + 1
}
