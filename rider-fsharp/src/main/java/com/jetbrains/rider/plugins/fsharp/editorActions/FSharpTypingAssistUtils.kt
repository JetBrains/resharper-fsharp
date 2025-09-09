package com.jetbrains.rider.plugins.fsharp.editorActions

import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.highlighter.HighlighterIterator
import com.intellij.psi.tree.IElementType
import com.intellij.resharper.assist.BracketMatcher
import com.intellij.resharper.assist.BracketMatcher.TokenIterator
import com.jetbrains.rider.editors.getPsiFile
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType
import com.jetbrains.rider.languages.fileTypes.csharp.kotoparser.session.RiderIndentingEditorSettings

val HighlighterIterator.tokenText: String
  get() = this.document.charsSequence.substring(this.start, this.end)

val HighlighterIterator.tokenTypeSafe: IElementType?
  get() = if (this.atEnd()) null else this.tokenType

fun HighlighterIterator.asTokenIterator(): TokenIterator = object : TokenIterator {
  override val tokenType: IElementType?
    get() = this@asTokenIterator.tokenTypeSafe

  override val tokenStart: Int
    get() = this@asTokenIterator.start

  override fun advance() = this@asTokenIterator.advance()
  override fun retreat() = this@asTokenIterator.retreat()
}

fun getIndentSettings(editor: Editor) =
  RiderIndentingEditorSettings.getUserDefinedSettings(editor.getPsiFile(), editor.getPsiFile()?.language)

val IElementType.isComment: Boolean
  get() = FSharpTokenType.COMMENTS.contains(this)


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
