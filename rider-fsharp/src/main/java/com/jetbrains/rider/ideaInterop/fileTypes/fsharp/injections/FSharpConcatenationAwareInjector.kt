package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.injections

import com.intellij.openapi.util.TextRange
import com.intellij.psi.ElementManipulators
import com.intellij.psi.PsiElement
import com.jetbrains.rider.ideaInterop.fileTypes.common.psi.ClrLanguageInterpolatedStringLiteralExpression
import com.jetbrains.rider.ideaInterop.fileTypes.common.psi.ClrLanguageInterpolatedStringLiteralExpressionPart
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType.*
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpInterpolatedStringLiteralExpressionPart
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpStringLiteralExpression
import com.jetbrains.rider.plugins.appender.database.common.ClrLanguageConcatenationAwareInjector
import org.intellij.plugins.intelliLang.inject.config.BaseInjection

class FSharpConcatenationAwareInjector :
  ClrLanguageConcatenationAwareInjector(FSharpInjectionSupport.FSHARP_SUPPORT_ID) {
  override fun getInjectionProcessor(injection: BaseInjection) = FSharpInjectionProcessor(injection)
  override fun isInjectionHost(concatenationOperand: PsiElement) = concatenationOperand is FSharpStringLiteralExpression

  protected class FSharpInjectionProcessor(injection: BaseInjection) :
    InjectionProcessor(
      injection,
      mapOf(SLASH_NEWLINE to "\\\\\\n", PLACEHOLDER_IDENTIFIER to "\\{\\d+}"),
      mapOf(SLASH_NEWLINE to "\\\\\\n")
    ) {

    override fun getFragmentStartAfterTemplate(matchResult: MatchResult) =
      if (matchResult.groups[SLASH_NEWLINE] != null) matchResult.range.last
      else super.getFragmentStartAfterTemplate(matchResult)

    override fun getPlaceholderForTemplate(matchResult: MatchResult) =
      if (matchResult.groups[SLASH_NEWLINE] != null) " "
      else PLACEHOLDER_IDENTIFIER

    override fun disableInspections(matchResult: MatchResult) =
      matchResult.groups[SLASH_NEWLINE] == null

    override fun getInterpolatedStringPartTextRange(
      literal: ClrLanguageInterpolatedStringLiteralExpression,
      part: ClrLanguageInterpolatedStringLiteralExpressionPart
    ): TextRange {
      val wholeLiteralRange = ElementManipulators.getValueTextRange(literal)
      return when (part) {
        is FSharpInterpolatedStringLiteralExpressionPart -> {
          val partLength = part.textLength

          val partText = part.text

          if (part.tokenType in INTERPOLATED_STRINGS_WITHOUT_INTERPOLATIONS)
            return wholeLiteralRange

          val startOffsetInPart =
            if (part.tokenType in INTERPOLATED_STRING_STARTS) {
              // can't reliably inspect injected PSI with interpolations
              disableInspections = true
              wholeLiteralRange.startOffset
            } else part.startOffsetInParent + 1

          val endOffsetInPart =
            if (part.tokenType in INTERPOLATED_STRING_ENDS) wholeLiteralRange.endOffset
            else {
              val containsFormatSpecifier =
                partLength > 3 && partText[partLength - 3] == '%' && partText[partLength - 2].isLetter()
              part.startOffsetInParent + partLength - 1 - (if (containsFormatSpecifier) 2 else 0)
            }

          TextRange(startOffsetInPart, endOffsetInPart)
        }

        else -> error("Unexpected interpolated part type: $part")
      }
    }

    companion object {
      const val SLASH_NEWLINE = "slashNewLine"
    }
  }
}
