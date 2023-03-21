package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer

import com.intellij.lexer.FlexAdapter
import com.intellij.lexer.RestartableLexer
import com.intellij.lexer.TokenIterator
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer._FSharpLexer.LINE
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer._FSharpLexer.YYINITIAL

@Suppress("UnstableApiUsage")
class FSharpLexer : FlexAdapter(_FSharpLexer()), RestartableLexer {
  override fun getStartState() = YYINITIAL

  override fun isRestartableState(state: Int) =
    state == YYINITIAL || state == LINE

  override fun getState(): Int {
    val flex = flex
    return if (flex is _FSharpLexer && !flex.isRestartableState ||
      FSharpTokenType.INTERPOLATED_STRING_ENDS.contains(tokenType)
    ) -1 else super.getState()
  }

  override fun start(
    buffer: CharSequence, startOffset: Int, endOffset: Int, initialState: Int, tokenIterator: TokenIterator?
  ) {
    val flex = flex
    if (flex is _FSharpLexer) {
      flex.myInterpolatedStringStates.clear()
      flex.myNestedCommentLevel = 0
      flex.myParenLevel = 0
      flex.myTokenLength = 0
      flex.myBrackLevel = 0
    }
    super.start(buffer, startOffset, endOffset, initialState)
  }
}
