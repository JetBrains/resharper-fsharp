package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi

import com.intellij.openapi.util.TextRange

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
