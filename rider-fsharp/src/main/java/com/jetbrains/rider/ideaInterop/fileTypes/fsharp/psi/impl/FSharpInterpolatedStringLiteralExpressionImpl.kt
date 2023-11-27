package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl

import com.intellij.psi.LiteralTextEscaper
import com.intellij.psi.PsiFileFactory
import com.intellij.psi.PsiLanguageInjectionHost
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpFileType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.*

class FSharpInterpolatedStringLiteralExpressionImpl(type: FSharpElementType) :
  FSharpStringLiteralExpressionBase(type),
  FSharpInterpolatedStringLiteralExpression {

  override fun isValidHost(): Boolean {
    val firstChild = firstChild
    val lastChild = lastChild
    return firstChild is FSharpInterpolatedStringLiteralExpressionPart && (
      (firstChild.tokenType in FSharpTokenType.INTERPOLATED_STRINGS_WITHOUT_INSERTIONS)
        ||
        (firstChild.tokenType in FSharpTokenType.INTERPOLATED_STRING_STARTS &&
          lastChild is FSharpInterpolatedStringLiteralExpressionPart &&
          lastChild.tokenType in FSharpTokenType.INTERPOLATED_STRING_ENDS)
      )
  }

  override fun updateText(text: String): PsiLanguageInjectionHost {
    val dummyFile =
      PsiFileFactory.getInstance(project).createFileFromText("dummy.fs", FSharpFileType, text) as FSharpFile

    val newStringExpression = dummyFile.firstChild as FSharpInterpolatedStringLiteralExpressionImpl
    return replace(newStringExpression) as PsiLanguageInjectionHost
  }

  override fun createLiteralTextEscaper(): LiteralTextEscaper<out PsiLanguageInjectionHost> =
    this.createSuitableLiteralTextEscaper()

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

            FSharpTokenType.RAW_INTERPOLATED_STRING_START,
            FSharpTokenType.RAW_INTERPOLATED_STRING ->
              FSharpStringLiteralType.RawInterpolatedString

            else -> error("invalid element type $tokenType")
          }

        else -> error("invalid first child $firstPart")
      }
}
