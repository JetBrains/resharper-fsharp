package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl

import com.intellij.openapi.util.TextRange
import com.intellij.psi.LiteralTextEscaper
import com.intellij.psi.PsiLanguageInjectionHost
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpStringLiteralExpression

class RegularStringEscaper(host: FSharpStringLiteralExpression) : StringLiteralEscaper<FSharpStringLiteralExpression>(host)
class VerbatimStringEscaper(host: FSharpStringLiteralExpression) : StringLiteralEscaper<FSharpStringLiteralExpression>(host)
class TripleQuotedStringEscaper(host: FSharpStringLiteralExpression) : StringLiteralEscaper<FSharpStringLiteralExpression>(host)
class ByteArrayEscaper(host: FSharpStringLiteralExpression) : StringLiteralEscaper<FSharpStringLiteralExpression>(host)

open class StringLiteralEscaper<T : PsiLanguageInjectionHost> (host: T) : LiteralTextEscaper<T>(host) {
    private lateinit var offsets: IntArray

    override fun decode(rangeInsideHost: TextRange, outChars: StringBuilder): Boolean {
        val subText = rangeInsideHost.substring(myHost.text)
        offsets = IntArray(subText.length + 1)
        return FSharpStringLiteralExpressionImpl.parseStringCharacters(subText, outChars, offsets)
    }

    override fun getOffsetInHost(offsetInDecoded: Int, rangeInsideHost: TextRange): Int {
        if (offsetInDecoded >= offsets.size) return -1
        val result = offsets[offsetInDecoded]
        return minOf(result, rangeInsideHost.length) + rangeInsideHost.startOffset
    }

    override fun isOneLine() = true
}