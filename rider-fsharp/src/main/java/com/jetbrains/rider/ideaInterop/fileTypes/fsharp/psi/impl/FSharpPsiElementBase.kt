package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl

import com.intellij.openapi.util.text.StringUtil
import com.intellij.psi.impl.source.tree.CompositePsiElement
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpElement
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpElementType

abstract class FSharpPsiElementBase(type: FSharpElementType) : CompositePsiElement(type), FSharpElement {
  override fun toString() = StringUtil.trimEnd(javaClass.simpleName, "Impl")
}
