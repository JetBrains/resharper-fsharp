package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.escaping

import com.intellij.openapi.util.TextRange
import com.intellij.psi.LiteralTextEscaper
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpStringLiteralExpression

//TODO: move to the platform
class FSharpRawStringLiteralEscaper(host: FSharpStringLiteralExpression) : LiteralTextEscaper<FSharpStringLiteralExpression>(host) {
  override fun decode(rangeInsideHost: TextRange, outChars: StringBuilder): Boolean {
    val subText = rangeInsideHost.substring(myHost.text)
    outChars.append(subText)
    return true
  }

  override fun getOffsetInHost(offsetInDecoded: Int, rangeInsideHost: TextRange): Int {
    if (offsetInDecoded > rangeInsideHost.length) return -1
    return offsetInDecoded + rangeInsideHost.startOffset
  }

  override fun isOneLine() = false
}
