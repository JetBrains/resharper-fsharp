package com.jetbrains.rider.ideaInterop.fileTypes.fsharp

import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageBase

abstract class FSharpLanguageBase internal constructor(name: String) : RiderLanguageBase(name, name) {
  override fun isCaseSensitive() = true
}

object FSharpLanguage : FSharpLanguageBase("F#")
