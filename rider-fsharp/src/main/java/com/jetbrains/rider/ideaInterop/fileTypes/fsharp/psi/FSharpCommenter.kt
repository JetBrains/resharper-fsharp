package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi

import com.intellij.lang.CodeDocumentationAwareCommenter
import com.intellij.psi.PsiComment
import com.intellij.psi.tree.IElementType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType

class FSharpCommenter : CodeDocumentationAwareCommenter {
  override fun getLineCommentPrefix() = "//"
  override fun getLineCommentTokenType(): IElementType = FSharpTokenType.LINE_COMMENT

  override fun getBlockCommentPrefix() = "(*"
  override fun getBlockCommentSuffix() = "*)"
  override fun getBlockCommentTokenType(): IElementType = FSharpTokenType.BLOCK_COMMENT

  override fun getCommentedBlockCommentPrefix() = null
  override fun getCommentedBlockCommentSuffix() = null

  override fun getDocumentationCommentPrefix() = "///"
  override fun getDocumentationCommentLinePrefix() = "///"
  override fun getDocumentationCommentSuffix() = null
  override fun getDocumentationCommentTokenType(): IElementType = FSharpTokenType.LINE_COMMENT
  override fun isDocumentationComment(element: PsiComment) =
    element.tokenType == FSharpTokenType.LINE_COMMENT && element.text.startsWith("///")
}
