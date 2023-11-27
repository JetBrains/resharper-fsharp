package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi

import com.intellij.openapi.util.TextRange
import com.intellij.psi.LiteralTextEscaper
import com.intellij.psi.PsiLanguageInjectionHost
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.escaping.FSharpRawStringLiteralEscaper
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.escaping.FSharpRegularStringLiteralEscaper
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.escaping.FSharpTripleQuotedStringLiteralEscaper
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.escaping.FSharpVerbatimStringLiteralEscaper

fun FSharpStringLiteralExpression.getRangeTrimQuotes(): TextRange {
  val literalType = literalType
  val startQuotesLength = when (literalType) {
    FSharpStringLiteralType.RegularString,
    FSharpStringLiteralType.ByteArray -> 1

    FSharpStringLiteralType.VerbatimString,
    FSharpStringLiteralType.RegularInterpolatedString -> 2

    FSharpStringLiteralType.TripleQuoteString,
    FSharpStringLiteralType.VerbatimInterpolatedString -> 3

    FSharpStringLiteralType.TripleQuoteInterpolatedString -> 4
    FSharpStringLiteralType.RawInterpolatedString ->
      (this as FSharpInterpolatedStringLiteralExpression).getDollarsCount() + 3
  }

  val endQuotesLength = when (literalType) {
    FSharpStringLiteralType.RegularString,
    FSharpStringLiteralType.VerbatimString,
    FSharpStringLiteralType.RegularInterpolatedString,
    FSharpStringLiteralType.VerbatimInterpolatedString -> 1

    FSharpStringLiteralType.ByteArray -> 2

    FSharpStringLiteralType.TripleQuoteString,
    FSharpStringLiteralType.RawInterpolatedString,
    FSharpStringLiteralType.TripleQuoteInterpolatedString -> 3
  }

  val start = (textRange.startOffset + startQuotesLength).coerceAtMost(textRange.endOffset)
  val end = (textRange.endOffset - endQuotesLength).coerceAtLeast(textRange.startOffset)
  return TextRange(start, end)
}

fun FSharpStringLiteralExpression.getRelativeRangeTrimQuotes() =
  getRangeTrimQuotes().shiftLeft(textRange.startOffset)

fun FSharpInterpolatedStringLiteralExpression.getDollarsCount() =
  if (literalType == FSharpStringLiteralType.RawInterpolatedString)
    firstChild.text.indexOfFirst { it == '"' }
  else 1

fun FSharpStringLiteralExpression.createSuitableLiteralTextEscaper(): LiteralTextEscaper<out PsiLanguageInjectionHost> {
  return when (literalType) {
    FSharpStringLiteralType.RegularString -> FSharpRegularStringLiteralEscaper(this, isInterpolated = false)
    FSharpStringLiteralType.RegularInterpolatedString -> FSharpRegularStringLiteralEscaper(
      this,
      isInterpolated = true
    )

    FSharpStringLiteralType.VerbatimString -> FSharpVerbatimStringLiteralEscaper(this, isInterpolated = false)
    FSharpStringLiteralType.VerbatimInterpolatedString -> FSharpVerbatimStringLiteralEscaper(
      this,
      isInterpolated = true
    )

    FSharpStringLiteralType.TripleQuoteInterpolatedString -> FSharpTripleQuotedStringLiteralEscaper(
      this,
      isInterpolated = true
    )

    FSharpStringLiteralType.TripleQuoteString -> FSharpTripleQuotedStringLiteralEscaper(
      this,
      isInterpolated = false
    )

    FSharpStringLiteralType.RawInterpolatedString -> FSharpRawStringLiteralEscaper(this)
    FSharpStringLiteralType.ByteArray -> error("Unexpected escaping call on ByteArray string")
  }
}
