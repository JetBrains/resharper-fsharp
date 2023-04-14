package com.jetbrains.rider.ideaInterop.fileTypes.fsharp

import com.intellij.lang.ASTNode
import com.intellij.lang.PsiBuilder
import com.intellij.lang.PsiParser
import com.intellij.psi.tree.IElementType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.FSharpElementTypes
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.parse
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.scanOrRollback
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.whileMakingProgress

class FSharpDummyParser : PsiParser {
  override fun parse(root: IElementType, builder: PsiBuilder): ASTNode {
    builder.setWhitespaceSkippedCallback { elementType, _, end ->
      if (elementType == FSharpTokenType.NEW_LINE) currentLineStart = end
    }
    builder.parseFile(root)
    return builder.treeBuilt
  }

  private fun PsiBuilder.parseFile(fileElementType: IElementType) {
    parse(fileElementType) {
      whileMakingProgress {
        if (!parseDummyExpression()) advanceLexerWithNewLineCounting()
        true
      }
    }
  }

  private fun PsiBuilder.getCurrentLineOffset() = currentOffset - currentLineStart
  private fun PsiBuilder.parseDummyExpression() = parseConcatenation()

  private fun PsiBuilder.parseConcatenation() =
    parse {
      //val currentIndent = getCurrentLineOffset()

      if (!parseStringExpression()) null
      else if (!scanOrRollback { tryParseConcatenationPartAhead() }) null
      else {
        whileMakingProgress {
          scanOrRollback { tryParseConcatenationPartAhead() }
        }
        FSharpElementTypes.DUMMY_EXPRESSION
      }
    }

  private fun PsiBuilder.tryParseConcatenationPartAhead(): Boolean {
    val hasSpaceBeforePlus = rawLookup(-1)?.let { isWhitespaceOrComment(it) } ?: false

    if (tokenType != FSharpTokenType.PLUS) return false
    val afterPlusTokenOffset = getCurrentLineOffset() + 1
    advanceLexer() // eat plus token

    // since "123" +"123" is not allowed
    if (hasSpaceBeforePlus && currentOffset - afterPlusTokenOffset == 0) return false

    //val secondStringOperandIndent = getCurrentLineOffset()
    // since
    //    "123"
    // + "123"
    // is not allowed
    //if (secondStringOperandIndent < requiredStringIndent) return false

    // since
    //    "123"
    // more than one space after plus
    // +  "123"
    // is not allowed
    //if (secondStringOperandIndent == requiredStringIndent &&
    //  secondStringOperandIndent - afterPlusTokenIndent > 1
    //) return false

    return parseStringExpression()
  }

  private fun PsiBuilder.parseStringExpression() =
    parseInterpolatedStringExpression() || parseAnyStringExpression()

  private fun PsiBuilder.parseAnyStringExpression() =
    if (tokenType !in FSharpTokenType.ALL_STRINGS) false
    else parse {
      val interpolated = tokenType in FSharpTokenType.INTERPOLATED_STRINGS
      advanceLexerWithNewLineCounting()
      if (interpolated) FSharpElementTypes.INTERPOLATED_STRING_LITERAL_EXPRESSION_PART
      else FSharpElementTypes.STRING_LITERAL_EXPRESSION
    }

  private fun PsiBuilder.parseInterpolatedStringExpression() =
    if (tokenType !in FSharpTokenType.INTERPOLATED_STRINGS) false
    else parse(FSharpElementTypes.INTERPOLATED_STRING_LITERAL_EXPRESSION) {
      var nestingDepth = 0
      whileMakingProgress {
        if (tokenType in FSharpTokenType.INTERPOLATED_STRING_STARTS) nestingDepth += 1
        if (tokenType in FSharpTokenType.INTERPOLATED_STRING_ENDS) nestingDepth -= 1
        if (!parseAnyStringExpression()) advanceLexerWithNewLineCounting()
        nestingDepth != 0
      }
    }

  private fun PsiBuilder.advanceLexerWithNewLineCounting() {
    when (tokenType) {
      in FSharpTokenType.STRINGS -> {
        val lastEndOfLineIndex = tokenText!!.lastIndexOf('\n')
        val stringStartOffset = currentOffset
        advanceLexer()
        if (lastEndOfLineIndex != -1) currentLineStart = stringStartOffset + lastEndOfLineIndex + 1
      }

      else -> advanceLexer()
    }
  }

  private var currentLineStart = 0
}
