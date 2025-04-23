package com.jetbrains.rider.plugins.fsharp.completion

import com.intellij.lang.ASTNode
import com.intellij.psi.PsiFile
import com.intellij.psi.impl.source.tree.TreeUtil
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpFile
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpStringLiteralExpression
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpStringLiteralType

// try to get string for #r "... <caret> ..."
fun getStringInsideReferenceDirective(file: PsiFile, offset: Int): FSharpStringLiteralExpression? {
  if (file !is FSharpFile) return null

  val string = file.findElementAt(offset)?.parent
  if (string !is FSharpStringLiteralExpression) return null

  return when (string.literalType) {
    FSharpStringLiteralType.RegularString,
    FSharpStringLiteralType.VerbatimString,
    FSharpStringLiteralType.TripleQuoteString -> {
      val previousNode = TreeUtil.skipWhitespaceAndComments(string.prevSibling as? ASTNode, false)
      return if (previousNode?.elementType == FSharpTokenType.PP_REFERENCE) string else null
    }

    else -> null
  }
}

fun insideReferenceDirective(file: PsiFile, offset: Int) = getStringInsideReferenceDirective(file, offset) != null
