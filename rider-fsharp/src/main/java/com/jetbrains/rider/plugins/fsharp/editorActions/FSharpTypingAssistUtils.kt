package com.jetbrains.rider.plugins.fsharp.editorActions

import com.intellij.openapi.editor.highlighter.HighlighterIterator
import com.intellij.psi.tree.IElementType
import com.intellij.resharper.assist.BracketMatcher
import com.intellij.resharper.assist.BracketMatcher.TokenIterator
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType

val HighlighterIterator.tokenText: String
  get() = this.document.charsSequence.substring(this.start, this.end)

fun HighlighterIterator.asTokenIterator(): TokenIterator =
  object : TokenIterator {
    override val tokenType: IElementType?
      get() =
        if (this@HighlighterIterator.atEnd()) null
        else this@HighlighterIterator.tokenType

    override val tokenStart: Int
      get() = this@HighlighterIterator.start

    override fun advance() = this@HighlighterIterator.advance()
    override fun retreat() = this@HighlighterIterator.retreat()
  }


class FSharpBracketMatcher : BracketMatcher(
  arrayOf(
    Pair(FSharpTokenType.LPAREN, FSharpTokenType.RPAREN),
    Pair(FSharpTokenType.LBRACK, FSharpTokenType.RBRACK),
    Pair(FSharpTokenType.LBRACE, FSharpTokenType.RBRACE),
    Pair(FSharpTokenType.LBRACK_BAR, FSharpTokenType.BAR_RBRACK),
    Pair(FSharpTokenType.LBRACE_BAR, FSharpTokenType.BAR_RBRACE),
    Pair(FSharpTokenType.LBRACK_LESS, FSharpTokenType.GREATER_RBRACK),
    Pair(FSharpTokenType.LQUOTE_TYPED, FSharpTokenType.RQUOTE_TYPED),
    Pair(FSharpTokenType.LQUOTE_UNTYPED, FSharpTokenType.RQUOTE_UNTYPED)
  )
)
