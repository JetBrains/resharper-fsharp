package com.jetbrains.rider.plugins.fsharp.completion

import com.intellij.codeInsight.completion.CompletionInitializationContext
import com.intellij.codeInsight.completion.CompletionParameters
import com.intellij.codeInsight.completion.CompletionResultSet
import com.intellij.psi.ElementManipulators
import com.intellij.psi.PsiFile
import com.intellij.psi.util.startOffset
import com.jetbrains.rdclient.patches.isPatchEngineEnabled
import com.jetbrains.rider.completion.patchEngine.RiderPatchEngineCompletionContributor
import com.jetbrains.rider.completion.patchEngine.RiderPatchEngineProtocolProvider
import com.jetbrains.rider.editors.startOffset
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpFile
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpStringLiteralExpression

class NuGetPatchEngineCompletionContributor : RiderPatchEngineCompletionContributor() {

  override fun isAvailable(file: PsiFile, offset: Int): Boolean = file is FSharpFile && insideReferenceDirective(file, offset)

  override fun fillCompletionVariants(parameters: CompletionParameters, result: CompletionResultSet) {
    if (!isPatchEngineEnabled) {
      super.fillCompletionVariants(parameters, result)
      return
    }

    val psiElement = parameters.originalFile.findElementAt(parameters.offset)?.parent ?: return
    if (psiElement !is FSharpStringLiteralExpression) return

    val stringText = ElementManipulators.getValueText(psiElement)
    val match = PACKAGE_REFERENCE_REGEX.find(stringText) ?: return

    val `package` = match.groups["package"]!!
    val packageZone = match.groups["packageZone"]!!
    val version = match.groups["version"]
    val versionZone = match.groups["versionZone"]

    prepareCustomParams(stringText, parameters.offset, "NuGet:name", `package`, packageZone)
    || prepareCustomParams(stringText, parameters.offset, "NuGet:version|${`package`.value}", version!!, versionZone!!)

    try {
      super.fillCompletionVariants(parameters, result)
    }
    finally {
      // Not sure is there a better way to provide params into provider with current architecture
      RiderPatchEngineProtocolProvider.getInstance().customPrefixThreadLocal.remove()
      RiderPatchEngineProtocolProvider.getInstance().nuGetCustomParamThreadLocal.remove()
    }
  }

  override fun beforeCompletion(context: CompletionInitializationContext) {
    if (!isPatchEngineEnabled)
      return

    val psiElement = context.file.findElementAt(context.startOffset)?.parent ?: return
    if (psiElement !is FSharpStringLiteralExpression) return

    val stringRange = ElementManipulators.getValueTextRange(psiElement)
    val stringContentRange = psiElement.startOffset + stringRange.startOffset

    context.offsetMap.addOffset(CompletionInitializationContext.START_OFFSET, context.editor.startOffset - stringContentRange)
    context.replacementOffset = psiElement.startOffset + stringRange.endOffset
  }

  private fun prepareCustomParams(
    content: String,
    cursorPosition: Int,
    host: String,
    strictGroup: MatchGroup,
    zoneGroup: MatchGroup,
  ): Boolean {
    if (containsExclusive(strictGroup.range, cursorPosition)) {
      val completionPrefix = content.substring(strictGroup.range.first, cursorPosition)
      RiderPatchEngineProtocolProvider.getInstance().customPrefixThreadLocal.set(completionPrefix)
      RiderPatchEngineProtocolProvider.getInstance().nuGetCustomParamThreadLocal.set(host)
      return true
    }
    else if (containsExclusive(zoneGroup.range, cursorPosition)) {
      RiderPatchEngineProtocolProvider.getInstance().customPrefixThreadLocal.set("")
      RiderPatchEngineProtocolProvider.getInstance().nuGetCustomParamThreadLocal.set(host)
      return true
    }

    return false
  }

  private fun containsExclusive(range: IntRange, value: Int) = value >= range.first && value <= range.last + 1
}
