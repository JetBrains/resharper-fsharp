package com.jetbrains.rider.ideaInterop.fileTypes.fsharp

import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageBase

object FSharpLanguage : RiderLanguageBase("F#", "FSHARP") {
    override fun isCaseSensitive() = true
}