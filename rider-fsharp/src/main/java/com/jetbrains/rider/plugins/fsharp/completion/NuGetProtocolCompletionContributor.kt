package com.jetbrains.rider.plugins.fsharp.completion

import com.intellij.codeInsight.completion.CompletionInitializationContext
import com.intellij.codeInsight.completion.CompletionType
import com.intellij.psi.ElementManipulators
import com.intellij.psi.PsiFile
import com.intellij.psi.util.startOffset
import com.jetbrains.rdclient.document.textControlModel
import com.jetbrains.rdclient.patches.isTypingSessionEnabled
import com.jetbrains.rider.completion.FrontendCompletionHost
import com.jetbrains.rider.completion.ProtocolCompletionContributor
import com.jetbrains.rider.completion.currentOffsetSafe
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpFile
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpStringLiteralExpression

internal const val KEY_NAME = "NuGet:name"
internal const val KEY_VERSION = "NuGet:version"

internal const val GROUP_PACKAGE = "package"
internal const val GROUP_PACKAGE_ZONE = "packageZone"
internal const val GROUP_VERSION = "version"
internal const val GROUP_VERSION_ZONE = "versionZone"

internal val PACKAGE_REFERENCE_REGEX =
  Regex("""^nuget:(?<packageZone>\s*(?<package>[a-zA-Z_0-9\-\\.]*)\s*)(,(?<versionZone>\s*(?<version>[a-zA-Z_0-9\-\\.]*)\s*))?""")

class NuGetProtocolCompletionContributor : ProtocolCompletionContributor() {

  private fun containsExclusive(range: IntRange, value: Int) = value >= range.first && value <= range.last + 1
  private fun ensureCompletionIsRunning(
    helper: FrontendCompletionHost,
    context: CompletionInitializationContext,
    content: String,
    cursorPosition: Int,
    host: String,
    strictGroup: MatchGroup,
    zoneGroup: MatchGroup): Boolean {
    if (containsExclusive(strictGroup.range, cursorPosition)) {
      val completionPrefix = content.substring(strictGroup.range.first, cursorPosition)
      helper.ensureCompletionIsRunning(context.editor, CompletionType.BASIC, context.invocationCount, completionPrefix, host)
      return true
    }
    else if (containsExclusive(zoneGroup.range, cursorPosition)) {
      helper.ensureCompletionIsRunning(context.editor, CompletionType.BASIC, context.invocationCount, "", host)
      return true
    }

    return false
  }

  override val isPreemptive = false
  override fun shouldStopOnPrefix(prefix: String, isAutoPopup: Boolean) = false
  override fun isAvailable(file: PsiFile, offset: Int) =
    file is FSharpFile && insideReferenceDirective(file, offset) && isTypingSessionEnabled

  override fun beforeCompletion(context: CompletionInitializationContext) {
    if (!isTypingSessionEnabled)
      return

    val psiElement = context.file.findElementAt(context.startOffset)?.parent ?: return
    if (psiElement !is FSharpStringLiteralExpression) return

    val helper = FrontendCompletionHost.getInstance(context.file.project)
    val stringText = ElementManipulators.getValueText(psiElement)
    val stringRange = ElementManipulators.getValueTextRange(psiElement)
    val stringContentRange = psiElement.startOffset + stringRange.startOffset

    val textControlModel = context.editor.textControlModel ?: return
    val match = PACKAGE_REFERENCE_REGEX.find(stringText) ?: return

    val cursorPosition = textControlModel.currentOffsetSafe - stringContentRange

    val `package` = match.groups[GROUP_PACKAGE]!!
    val packageZone = match.groups[GROUP_PACKAGE_ZONE]!!
    val version = match.groups[GROUP_VERSION]
    val versionZone = match.groups[GROUP_VERSION_ZONE]

    context.replacementOffset = psiElement.startOffset + stringRange.endOffset

    ensureCompletionIsRunning(helper, context, stringText, cursorPosition, KEY_NAME, `package`, packageZone) ||
    versionZone != null && `package`.value.isNotEmpty() &&
      ensureCompletionIsRunning(helper, context, stringText, cursorPosition, "$KEY_VERSION|${`package`.value}", version!!, versionZone)
  }
}
