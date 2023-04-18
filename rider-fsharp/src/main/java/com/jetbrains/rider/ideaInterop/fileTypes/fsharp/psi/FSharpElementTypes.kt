package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi

import com.intellij.lang.ASTNode
import com.intellij.psi.tree.ICompositeElementType
import com.intellij.psi.tree.IElementType
import com.intellij.psi.tree.IFileElementType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguage

class FSharpFileElementType : IFileElementType("FSharpFile", FSharpLanguage)

open class FSharpElementType(debugName: String, val text: String = debugName) : IElementType(debugName, FSharpLanguage)
abstract class FSharpCompositeElementType(debugName: String) : FSharpElementType(debugName), ICompositeElementType

inline fun createCompositeElementType(debugName: String, crossinline elementFactory: (FSharpElementType) -> ASTNode) =
  object : FSharpCompositeElementType(debugName) {
    override fun createCompositeNode() = elementFactory(this)
  }
