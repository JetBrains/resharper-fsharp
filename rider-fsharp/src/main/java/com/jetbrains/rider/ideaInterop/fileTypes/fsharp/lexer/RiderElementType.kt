package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer

import com.intellij.psi.tree.IElementType
import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageBase

open class RiderElementType(value: String, val representation: String, language: RiderLanguageBase) :
  IElementType(value, language)
