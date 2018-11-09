package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl

import com.intellij.psi.impl.source.tree.LazyParseablePsiElement
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpReparseableElement
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpReparseableElementType

abstract class FSharpReparseableElementBase(blockType: FSharpReparseableElementType, buffer: CharSequence?) : LazyParseablePsiElement(blockType, buffer), FSharpReparseableElement
