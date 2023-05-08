package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.escaping

import com.jetbrains.rider.ideaInterop.fileTypes.common.psi.escaping.ClrLanguageVerbatimStringLiteralEscaper
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpStringLiteralExpression

open class FSharpVerbatimStringLiteralEscaper(host: FSharpStringLiteralExpression, val isInterpolated: Boolean) :
    ClrLanguageVerbatimStringLiteralEscaper<FSharpStringLiteralExpression>(host, isInterpolated)

class FSharpTripleQuotedStringLiteralEscaper(host: FSharpStringLiteralExpression, isInterpolated: Boolean) :
    FSharpVerbatimStringLiteralEscaper(host, isInterpolated) {
    override fun isSymbolEscaped(c: Char): Boolean = when (c) {
        '{', '}' -> isInterpolated
        else -> false
    }
}
