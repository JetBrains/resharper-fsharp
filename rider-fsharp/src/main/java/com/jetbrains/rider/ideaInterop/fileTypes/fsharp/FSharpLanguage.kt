package com.jetbrains.rider.ideaInterop.fileTypes.fsharp

import com.intellij.lang.Language

abstract class FSharpLanguageBase internal constructor(name: String) : Language(name) {
  override fun isCaseSensitive() = true
}

object FSharpLanguage : FSharpLanguageBase("F#")
