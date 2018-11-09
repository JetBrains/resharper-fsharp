package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi

import com.intellij.openapi.util.TextRange

fun FSharpStringLiteralExpression.getRangeTrimQuotes(): TextRange {
    val offset = when(literalType) {
        FSharpStringLiteralType.RegularString,
        FSharpStringLiteralType.ByteArray -> 1
        FSharpStringLiteralType.VerbatimString -> 2
        FSharpStringLiteralType.TripleQuotedString -> 3
        else -> 1
    }
    val endOffset = when(literalType) {
        FSharpStringLiteralType.RegularString,
        FSharpStringLiteralType.VerbatimString -> 1
        FSharpStringLiteralType.ByteArray -> 2
        FSharpStringLiteralType.TripleQuotedString -> 3
        else -> 1
    }

    val start = Math.min(textRange.startOffset + offset, textRange.endOffset)
    val end = Math.max(textRange.endOffset - endOffset, textRange.startOffset)
    return TextRange(start, end)
}

fun FSharpStringLiteralExpression.getRelativeRangeTrimQuotes(): TextRange {
    return getRangeTrimQuotes().shiftLeft(textRange.startOffset)
}