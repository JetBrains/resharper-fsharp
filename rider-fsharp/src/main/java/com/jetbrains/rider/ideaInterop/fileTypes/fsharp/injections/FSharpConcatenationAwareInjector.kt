package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.injections

import com.intellij.openapi.util.TextRange
import com.intellij.psi.ElementManipulators
import com.intellij.psi.PsiElement
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType.*
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.*
import com.jetbrains.rider.languages.fileTypes.clr.psi.ClrLanguageInterpolatedStringLiteralExpression
import com.jetbrains.rider.languages.fileTypes.clr.psi.ClrLanguageInterpolatedStringLiteralExpressionPart
import com.jetbrains.rider.languages.fileTypes.clr.psi.ClrLanguageStringLiteralExpression
import com.jetbrains.rider.plugins.appender.database.SqlAwareClrLanguageConcatenationAwareInjector
import org.intellij.plugins.intelliLang.inject.config.BaseInjection

class FSharpConcatenationAwareInjector :
  SqlAwareClrLanguageConcatenationAwareInjector(FSharpInjectionSupport.FSHARP_SUPPORT_ID) {
  override fun getInjectionProcessor(injection: BaseInjection,
                                     host: ClrLanguageStringLiteralExpression): InjectionProcessor = FSharpInjectionProcessor(injection, host)
  override fun isInjectionHost(concatenationOperand: PsiElement) = concatenationOperand is FSharpStringLiteralExpression

  private class FSharpInjectionProcessor(injection: BaseInjection, host: ClrLanguageStringLiteralExpression) :
    ClrLanguageSqlAwareInjectionProcessor(
      injection,
      mapOf(SLASH_NEWLINE to "\\\\\\n", PLACEHOLDER_IDENTIFIER to "\\{\\d+}"),
      mapOf(SLASH_NEWLINE to "\\\\\\n", PLACEHOLDER_IDENTIFIER to createInterpolatedStringFormattingPlaceholderRegex(host))
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
      val fsharpLiteral = literal as FSharpInterpolatedStringLiteralExpression
      val interpolationBracketsCount = fsharpLiteral.getDollarsCount()
      return when (part) {
        is FSharpInterpolatedStringLiteralExpressionPart -> {
          val partLength = part.textLength
          val partText = part.text

          if (part.tokenType in INTERPOLATED_STRINGS_WITHOUT_INSERTIONS)
            return wholeLiteralRange

          val startOffsetInPart =
            if (part.tokenType in INTERPOLATED_STRING_STARTS) {
              wholeLiteralRange.startOffset
            } else part.startOffsetInParent + interpolationBracketsCount

          val endOffsetInPart =
            if (part.tokenType in INTERPOLATED_STRING_ENDS) wholeLiteralRange.endOffset
            else {
              val formatSpecifierOffset = interpolationBracketsCount + 2
              val containsFormatSpecifier =
                partLength > formatSpecifierOffset &&
                  partText[partLength - formatSpecifierOffset] == '%' &&
                  partText[partLength - formatSpecifierOffset + 1].isLetter()
              part.startOffsetInParent + partLength - interpolationBracketsCount - (if (containsFormatSpecifier) 2 else 0)
            }

          TextRange(startOffsetInPart, endOffsetInPart)
        }

        else -> error("Unexpected interpolated part type: $part")
      }
    }

    companion object {
      const val SLASH_NEWLINE = "slashNewLine"

      private fun createInterpolatedStringFormattingPlaceholderRegex(host: ClrLanguageStringLiteralExpression): String {
        if (host !is FSharpStringLiteralExpression) {
          // language=RegExp
          return "\\{\\d+}"
        }

        if (host.literalType !in rawStringTypes) {
          // language=RegExp
          return "\\{\\{\\d+}}"
        }

        // language=RegExp
        return "\\{\\d+}"
      }

      private val rawStringTypes = listOf(
        FSharpStringLiteralType.TripleQuoteString,
        FSharpStringLiteralType.TripleQuoteInterpolatedString,
        FSharpStringLiteralType.RawInterpolatedString
      )
    }
  }
}
