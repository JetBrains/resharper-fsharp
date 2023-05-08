package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi

import com.intellij.openapi.util.TextRange
import com.intellij.psi.LiteralTextEscaper
import com.intellij.psi.PsiLanguageInjectionHost
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.escaping.FSharpRegularStringLiteralEscaper
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.escaping.FSharpTripleQuotedStringLiteralEscaper
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.escaping.FSharpVerbatimStringLiteralEscaper

fun FSharpStringLiteralExpression.getRangeTrimQuotes(): TextRange {
  val literalType = literalType
  val startOffset = when (literalType) {
    FSharpStringLiteralType.RegularString,
    FSharpStringLiteralType.ByteArray -> 1

    FSharpStringLiteralType.VerbatimString,
    FSharpStringLiteralType.RegularInterpolatedString -> 2

    FSharpStringLiteralType.TripleQuoteString,
    FSharpStringLiteralType.VerbatimInterpolatedString -> 3

    FSharpStringLiteralType.TripleQuoteInterpolatedString -> 4
  }

  val endOffset = when (literalType) {
    FSharpStringLiteralType.RegularString,
    FSharpStringLiteralType.VerbatimString,
    FSharpStringLiteralType.RegularInterpolatedString,
    FSharpStringLiteralType.VerbatimInterpolatedString -> 1

    FSharpStringLiteralType.ByteArray -> 2

    FSharpStringLiteralType.TripleQuoteString,
    FSharpStringLiteralType.TripleQuoteInterpolatedString -> 3
  }

  val start = (textRange.startOffset + startOffset).coerceAtMost(textRange.endOffset)
  val end = (textRange.endOffset - endOffset).coerceAtLeast(textRange.startOffset)
  return TextRange(start, end)
}

fun FSharpStringLiteralExpression.getRelativeRangeTrimQuotes() =
  getRangeTrimQuotes().shiftLeft(textRange.startOffset)

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

    FSharpStringLiteralType.ByteArray -> error("Unexpected escaping call on ByteArray string")
  }
}
