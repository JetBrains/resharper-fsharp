package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer

class FSharpKeywordTokenNodeType(value: String, representation: String, val isContextual: Boolean) :
  FSharpTokenNodeType(value, representation)
