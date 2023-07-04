package com.jetbrains.rider.plugins.fsharp.completion

import com.intellij.lang.ASTNode
import com.intellij.psi.impl.source.tree.TreeUtil
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpFile
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpStringLiteralExpression
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpStringLiteralType

// check for #r "... <caret> ..." 
fun insideReferenceDirective(file: FSharpFile, offset: Int): Boolean {
  val string = file.findElementAt(offset)?.parent
  if (string !is FSharpStringLiteralExpression) return false

  return when (string.literalType) {
    FSharpStringLiteralType.RegularString,
    FSharpStringLiteralType.VerbatimString,
    FSharpStringLiteralType.TripleQuoteString -> {
      val previousNode = TreeUtil.skipWhitespaceAndComments(string.prevSibling as? ASTNode, false)
      previousNode?.elementType == FSharpTokenType.PP_REFERENCE
    }

    else -> false
  }
}
