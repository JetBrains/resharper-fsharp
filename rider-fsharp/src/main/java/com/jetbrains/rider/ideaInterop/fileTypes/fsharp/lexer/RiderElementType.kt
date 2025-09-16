package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer

import com.intellij.lang.Language
import com.intellij.psi.tree.IElementType

open class RiderElementType(value: String, val representation: String, language: Language) :
  IElementType(value, language)
