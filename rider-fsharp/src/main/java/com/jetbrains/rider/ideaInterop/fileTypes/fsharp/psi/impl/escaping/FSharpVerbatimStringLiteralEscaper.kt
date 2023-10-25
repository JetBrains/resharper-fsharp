package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.escaping

import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpStringLiteralExpression
import com.jetbrains.rider.languages.fileTypes.clr.psi.escaping.ClrLanguageVerbatimStringLiteralEscaper

open class FSharpVerbatimStringLiteralEscaper(host: FSharpStringLiteralExpression, val isInterpolated: Boolean) :
  ClrLanguageVerbatimStringLiteralEscaper<FSharpStringLiteralExpression>(host, isInterpolated)

class FSharpTripleQuotedStringLiteralEscaper(host: FSharpStringLiteralExpression, isInterpolated: Boolean) :
    FSharpVerbatimStringLiteralEscaper(host, isInterpolated) {
    override fun isSymbolEscaped(c: Char): Boolean = when (c) {
        '{', '}' -> isInterpolated
        else -> false
    }
}
