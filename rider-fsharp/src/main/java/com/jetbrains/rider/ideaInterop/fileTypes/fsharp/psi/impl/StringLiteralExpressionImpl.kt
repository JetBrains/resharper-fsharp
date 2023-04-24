package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl

import com.intellij.psi.LiteralTextEscaper
import com.intellij.psi.PsiLanguageInjectionHost
import com.intellij.psi.util.elementType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpElementType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpStringLiteralType

class FSharpFSharpStringLiteralExpressionImpl(type: FSharpElementType) : FSharpStringLiteralExpressionBase(type) {
  override fun isValidHost(): Boolean {
    return when (stringTokenType) {
      FSharpTokenType.UNFINISHED_STRING,
      FSharpTokenType.UNFINISHED_VERBATIM_STRING,
      FSharpTokenType.UNFINISHED_TRIPLE_QUOTED_STRING,
      FSharpTokenType.SBYTE -> false

      else -> true
    }
  }

  override fun updateText(newText: String): PsiLanguageInjectionHost {
    return this //TODO
  }

  override fun createLiteralTextEscaper(): LiteralTextEscaper<out PsiLanguageInjectionHost> {
    return LiteralTextEscaper.createSimple(this) //TODO
  }

  override val literalType: FSharpStringLiteralType
    get() {
      return when (stringTokenType) {
        FSharpTokenType.STRING,
        FSharpTokenType.UNFINISHED_STRING -> FSharpStringLiteralType.RegularString

        FSharpTokenType.VERBATIM_STRING,
        FSharpTokenType.VERBATIM_BYTEARRAY,
        FSharpTokenType.UNFINISHED_VERBATIM_STRING -> FSharpStringLiteralType.VerbatimString

        FSharpTokenType.TRIPLE_QUOTED_STRING,
        FSharpTokenType.UNFINISHED_TRIPLE_QUOTED_STRING -> FSharpStringLiteralType.TripleQuoteString

        FSharpTokenType.BYTEARRAY -> FSharpStringLiteralType.ByteArray
        else -> error("invalid element type $stringTokenType")
      }
    }

  private val stringTokenType get() = firstChild?.elementType
}
