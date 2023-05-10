package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl

import com.intellij.openapi.fileEditor.FileDocumentManager
import com.intellij.psi.LiteralTextEscaper
import com.intellij.psi.PsiLanguageInjectionHost
import com.intellij.refactoring.suggested.endOffset
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
    FileDocumentManager.getInstance()
      .getDocument(containingFile.virtualFile)
      ?.replaceString(startOffset, endOffset, text)
    return this
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

            else -> error("invalid element type $tokenType")
          }

        else -> error("invalid first child $firstPart")
      }
}
