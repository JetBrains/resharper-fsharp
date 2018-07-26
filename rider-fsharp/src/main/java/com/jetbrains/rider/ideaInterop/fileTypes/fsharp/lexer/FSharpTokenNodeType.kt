package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer

import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguage

open class FSharpTokenNodeType(value: String, representation: String) : RiderElementType(value, representation, FSharpLanguage) {
    constructor(value: String) : this(value, value)
}
