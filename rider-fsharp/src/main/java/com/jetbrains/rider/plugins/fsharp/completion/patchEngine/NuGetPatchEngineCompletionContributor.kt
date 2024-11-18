package com.jetbrains.rider.plugins.fsharp.completion.patchEngine

import com.intellij.codeInsight.completion.CompletionInitializationContext
import com.intellij.openapi.diagnostic.trace
import com.intellij.psi.ElementManipulators
import com.intellij.psi.PsiFile
import com.intellij.psi.util.startOffset
import com.jetbrains.rdclient.document.textControlId
import com.jetbrains.rdclient.document.textControlModel
import com.jetbrains.rdclient.patches.isPatchEngineEnabled
import com.jetbrains.rider.completion.patchEngine.RiderPatchEngineCompletionContributor
import com.jetbrains.rider.completion.patchEngine.RiderPatchEngineProtocolProvider
import com.jetbrains.rider.completion.patchEngine.RiderPatchEngineProtocolProvider.Companion.logger
import com.jetbrains.rider.completion.patchEngine.isPreemptiveCompletionEnabled
import com.jetbrains.rider.editors.startOffset
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpFile
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpStringLiteralExpression
import com.jetbrains.rider.plugins.fsharp.completion.PACKAGE_REFERENCE_REGEX
import com.jetbrains.rider.plugins.fsharp.completion.insideReferenceDirective

class NuGetPatchEngineCompletionContributor : RiderPatchEngineCompletionContributor() {

  override fun isAvailable(file: PsiFile, offset: Int): Boolean = file is FSharpFile && insideReferenceDirective(file, offset)

  override fun beforeCompletion(context: CompletionInitializationContext) {
    if (!isPatchEngineEnabled || !isPreemptiveCompletionEnabled)
      return

    val psiElement = context.file.findElementAt(context.startOffset)?.parent ?: return
    if (psiElement !is FSharpStringLiteralExpression) return

    val stringRange = ElementManipulators.getValueTextRange(psiElement)
    val stringContentRange = psiElement.startOffset + stringRange.startOffset

    context.offsetMap.addOffset(CompletionInitializationContext.START_OFFSET, context.editor.startOffset - stringContentRange)
    context.replacementOffset = psiElement.startOffset + stringRange.endOffset

    val stringText = ElementManipulators.getValueText(psiElement)
    val match = PACKAGE_REFERENCE_REGEX.find(stringText) ?: return

    val `package` = match.groups["package"]!!
    val packageZone = match.groups["packageZone"]!!
    val version = match.groups["version"]
    val versionZone = match.groups["versionZone"]

    prepareCustomParams(stringText, context.startOffset, "NuGet:name", `package`, packageZone, context)
    || prepareCustomParams(stringText, context.startOffset, "NuGet:version|${`package`.value}", version!!, versionZone!!, context)

    RiderPatchEngineProtocolProvider.getInstance().isSuppress = true
  }

  private fun prepareCustomParams(
    content: String,
    cursorPosition: Int,
    host: String,
    strictGroup: MatchGroup,
    zoneGroup: MatchGroup,
    initContext: CompletionInitializationContext,
  ): Boolean {
    val textControlModel = initContext.editor.textControlModel ?: run {
      logger.trace { "TextControlModel is null during starting completion :: editor=${initContext.editor}" }
      return false
    }

    val textControlId = initContext.editor.textControlId ?: run {
      logger.trace { "TextControlId is null during starting completion :: editor=${initContext.editor}" }
      return false
    }

    if (containsExclusive(strictGroup.range, cursorPosition)) {
      val completionPrefix = content.substring(strictGroup.range.first, cursorPosition)
      RiderPatchEngineProtocolProvider.getInstance().triggerCustomCompletion(initContext.project, textControlId, textControlModel,
                                                                             initContext.completionType, 1, completionPrefix, host)
      return true
    }
    else if (containsExclusive(zoneGroup.range, cursorPosition)) {
      RiderPatchEngineProtocolProvider.getInstance().triggerCustomCompletion(initContext.project, textControlId, textControlModel,
                                                                             initContext.completionType, 1, "", host)
      return true
    }

    return false
  }

  private fun containsExclusive(range: IntRange, value: Int) = value >= range.first && value <= range.last + 1
}
