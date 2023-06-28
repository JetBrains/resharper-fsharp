package com.jetbrains.rider.plugins.fsharp.completion

import com.intellij.psi.PsiWhiteSpace
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpFile
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpStringLiteralExpression
import com.intellij.psi.util.elementType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpStringLiteralType

// check for #r "... <caret> ..." 
fun insideReferenceDirective(file: FSharpFile, offset: Int): Boolean {
  val string = file.findElementAt(offset)?.parent
  if (string !is FSharpStringLiteralExpression) return false

  return when (string.literalType) {
    FSharpStringLiteralType.RegularString,
    FSharpStringLiteralType.VerbatimString,
    FSharpStringLiteralType.TripleQuoteString -> {
      val whiteSpace = string.prevSibling
      if (whiteSpace !is PsiWhiteSpace) return false

      whiteSpace.prevSibling?.elementType == FSharpTokenType.PP_REFERENCE
    }

    else -> false
  }
}
