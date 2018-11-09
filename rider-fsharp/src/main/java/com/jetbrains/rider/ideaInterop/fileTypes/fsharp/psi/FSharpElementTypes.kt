package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi

import com.intellij.lang.ASTNode
import com.intellij.lang.Language
import com.intellij.psi.tree.ICompositeElementType
import com.intellij.psi.tree.IElementType
import com.intellij.psi.tree.IFileElementType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguage
import com.intellij.openapi.project.Project
import com.intellij.psi.tree.IReparseableElementType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpScriptLanguage
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.FSharpIndentationBlockImpl
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.FSharpLineImpl
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.FSharpReparseableElementBase

class FSharpFileElementType : IFileElementType("FSharpFile", FSharpLanguage)
class FSharpScriptElementType : IFileElementType("FSharpScript", FSharpScriptLanguage)

open class FSharpElementType(debugName: String, val text: String = debugName) : IElementType(debugName, FSharpLanguage)

abstract class FSharpCompositeElementType(debugName: String) : FSharpElementType(debugName), ICompositeElementType

abstract class FSharpReparseableElementType(debugName: String) : IReparseableElementType(debugName, FSharpLanguage), ICompositeElementType {
    abstract override fun createNode(text: CharSequence?): ASTNode?
}

class FSharpLineType : FSharpReparseableElementType("LINE") {
    override fun createNode(text: CharSequence?): ASTNode? {
        return FSharpLineImpl(this, text)
    }

    override fun createCompositeNode(): ASTNode {
        return FSharpLineImpl(this, null)
    }
    override fun isParsable(buffer: CharSequence, fileLanguage: Language, project: Project): Boolean {
        return FSharpLineImpl.isParseable(buffer)
    }
}

class FSharpIndentationBlockType : FSharpReparseableElementType("INDENTATION_BLOCK") {
    override fun createNode(text: CharSequence?): ASTNode? {
        return FSharpIndentationBlockImpl(this, text)
    }

    override fun createCompositeNode(): ASTNode {
        return FSharpIndentationBlockImpl(this, null)
    }
    override fun isParsable(buffer: CharSequence, fileLanguage: Language, project: Project): Boolean {
        return FSharpIndentationBlockImpl.isParseable(buffer)
    }
}


inline fun createCompositeElementType(debugName: String, crossinline elementFactory: (FSharpElementType) -> ASTNode) =
    object : FSharpCompositeElementType(debugName) {
        override fun createCompositeNode(): ASTNode {
            return elementFactory(this)
        }
    }