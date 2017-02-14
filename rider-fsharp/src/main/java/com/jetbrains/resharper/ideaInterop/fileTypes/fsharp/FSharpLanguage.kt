package com.jetbrains.resharper.ideaInterop.fileTypes.fsharp

import com.jetbrains.resharper.ideaInterop.fileTypes.RiderLanguageBase

object FSharpLanguage : RiderLanguageBase("F#", "FSHARP") {
    override fun isCaseSensitive() = true
}