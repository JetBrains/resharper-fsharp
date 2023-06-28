package com.jetbrains.rider.plugins.fsharp.completion

import com.intellij.codeInsight.completion.CompletionInitializationContext
import com.intellij.codeInsight.completion.CompletionType
import com.intellij.psi.ElementManipulators
import com.intellij.psi.PsiFile
import com.intellij.refactoring.suggested.startOffset
import com.jetbrains.rdclient.document.textControlModel
import com.jetbrains.rider.completion.FrontendCompletionHost
import com.jetbrains.rider.completion.ProtocolCompletionContributor
import com.jetbrains.rider.completion.currentOffsetSafe
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpFile
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpStringLiteralExpression

class NuGetProtocolCompletionContributor : ProtocolCompletionContributor() {
  private val packageReferenceRegex =
    Regex("""^nuget:(?<packageZone>\s*(?<package>[a-zA-Z_0-9\-\\.]*)\s*)(,(?<versionZone>\s*(?<version>[a-zA-Z_0-9\-\\.]*)\s*))?""")

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
    else if (containsExclusive(zoneGroup.range, cursorPosition) && strictGroup.value.isEmpty()) {
      helper.ensureCompletionIsRunning(context.editor, CompletionType.BASIC, context.invocationCount, "", host)
      return true
    }
    
    return false
  }

  override val isPreemptive = false
  override fun shouldStopOnPrefix(prefix: String, isAutoPopup: Boolean) = false
  override fun isAvailable(file: PsiFile, offset: Int) = file is FSharpFile && insideReferenceDirective(file, offset)

  override fun beforeCompletion(context: CompletionInitializationContext) {
    val psiElement = context.file.findElementAt(context.startOffset)?.parent ?: return
    if (psiElement !is FSharpStringLiteralExpression) return

    val helper = FrontendCompletionHost.getInstance(context.file.project)
    val stringText = ElementManipulators.getValueText(psiElement)
    val stringRange = ElementManipulators.getValueTextRange(psiElement)
    val stringContentRange = psiElement.startOffset + stringRange.startOffset

    val textControlModel = context.editor.textControlModel ?: return
    val match = packageReferenceRegex.find(stringText) ?: return

    val cursorPosition = textControlModel.currentOffsetSafe - stringContentRange

    val `package` = match.groups["package"]!!
    val packageZone = match.groups["packageZone"]!!
    val version = match.groups["version"]!!
    val versionZone = match.groups["versionZone"]!!
    
    ensureCompletionIsRunning(helper, context, stringText, cursorPosition, "NuGet:name", `package`, packageZone) ||
    ensureCompletionIsRunning(helper, context, stringText, cursorPosition, "NuGet:version|${`package`.value}", version, versionZone)
  }
}
