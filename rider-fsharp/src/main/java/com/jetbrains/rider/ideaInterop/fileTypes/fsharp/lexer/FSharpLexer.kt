package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer

import com.intellij.lexer.FlexAdapter
import com.intellij.lexer.RestartableLexer
import com.intellij.lexer.TokenIterator
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer._FSharpLexer.YYINITIAL

@Suppress("UnstableApiUsage")
class FSharpLexer : FlexAdapter(_FSharpLexer()), RestartableLexer {
    override fun getStartState() = YYINITIAL

    override fun isRestartableState(state: Int): Boolean {
        val flex = flex
        return flex is _FSharpLexer && flex.isRestartableState
    }

    override fun start(
        buffer: CharSequence, startOffset: Int, endOffset: Int, initialState: Int, tokenIterator: TokenIterator?
    ) = super.start(buffer, startOffset, endOffset, initialState)
}
