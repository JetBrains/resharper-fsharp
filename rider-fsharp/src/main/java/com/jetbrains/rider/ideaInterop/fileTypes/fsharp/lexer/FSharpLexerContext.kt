package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer

data class FSharpLexerContext(
    val LexerState: Int,
    val ParenLevel: Int,
    val BrackLevel: Int,
    val NestedCommentLevel: Int,
)
