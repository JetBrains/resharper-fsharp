package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer

import java.util.*

data class FSharpLexerInterpolatedStringState(
    val Kind: FSharpInterpolatedStringKind,
    val PreviousLexerContext: FSharpLexerContext,
    val InterpolatedStringStack: Stack<InterpolatedStringStackItem>
)
