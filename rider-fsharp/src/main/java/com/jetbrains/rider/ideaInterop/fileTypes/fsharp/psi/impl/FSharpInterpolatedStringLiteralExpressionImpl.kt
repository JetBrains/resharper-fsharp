package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl

import com.intellij.psi.LiteralTextEscaper
import com.intellij.psi.PsiLanguageInjectionHost
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpElementType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpInterpolatedStringLiteralExpression
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpInterpolatedStringLiteralExpressionPart
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpStringLiteralType

class FSharpInterpolatedStringLiteralExpressionImpl(type: FSharpElementType) :
  FSharpStringLiteralExpressionBase(type),
  FSharpInterpolatedStringLiteralExpression {

  override fun isValidHost(): Boolean {
    return true //TODO
  }

  override fun updateText(newText: String): PsiLanguageInjectionHost {
    return this //TODO
  }

  override fun createLiteralTextEscaper(): LiteralTextEscaper<out PsiLanguageInjectionHost> {
    return LiteralTextEscaper.createSimple(this) //TODO
  }

  override val literalType: FSharpStringLiteralType
    get() =
      when (val firstPart = firstChild) {
        is FSharpInterpolatedStringLiteralExpressionPart ->
          when (val tokenType = firstPart.tokenType) {
            FSharpTokenType.REGULAR_INTERPOLATED_STRING_START,
            FSharpTokenType.REGULAR_INTERPOLATED_STRING ->
              FSharpStringLiteralType.RegularInterpolatedString

            FSharpTokenType.VERBATIM_INTERPOLATED_STRING_START,
            FSharpTokenType.VERBATIM_INTERPOLATED_STRING ->
              FSharpStringLiteralType.VerbatimInterpolatedString

            FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_START,
            FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING,
            FSharpTokenType.TRIPLE_QUOTED_STRING ->
              FSharpStringLiteralType.TripleQuoteInterpolatedString

            else -> error("invalid element type $tokenType")
          }

        else -> error("invalid first child $firstPart")
      }
}
