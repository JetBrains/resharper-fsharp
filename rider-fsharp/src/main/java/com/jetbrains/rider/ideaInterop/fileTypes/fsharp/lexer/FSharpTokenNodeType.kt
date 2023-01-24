package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer

import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguage

open class FSharpTokenNodeType @JvmOverloads constructor(
  value: String, representation: String = value
) : RiderElementType(
  value, representation, FSharpLanguage
)
