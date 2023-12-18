package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer

import java.util.*

data class FSharpLexerInterpolatedStringState(
  public val Kind: FSharpInterpolatedStringKind,
  val DelimiterLength: Int?,
  val PreviousLexerContext: FSharpLexerContext,
  val InterpolatedStringStack: Stack<InterpolatedStringStackItem>
)
