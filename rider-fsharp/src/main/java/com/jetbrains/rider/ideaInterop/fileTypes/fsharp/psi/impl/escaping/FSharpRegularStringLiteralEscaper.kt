package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.escaping

import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpStringLiteralExpression
import com.jetbrains.rider.languages.fileTypes.clr.psi.escaping.ClrLanguageRegularStringLiteralEscaper

class FSharpRegularStringLiteralEscaper(host: FSharpStringLiteralExpression, isInterpolated: Boolean) :
  ClrLanguageRegularStringLiteralEscaper<FSharpStringLiteralExpression>(host, isInterpolated, true) {
  override fun isOneLine() = false

  override fun parseEscapedChar(c: Char, out: StringBuilder) =
    when (c) {
      ' ' -> {
        out.append("\\ ")
        true
      }

      else -> super.parseEscapedChar(c, out)
    }
}
