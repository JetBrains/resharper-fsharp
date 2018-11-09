package com.jetbrains.rider.ideaInterop.fileTypes.fsharp

import com.intellij.lang.ASTNode
import com.intellij.lang.ParserDefinition
import com.intellij.lang.PsiParser
import com.intellij.lexer.Lexer
import com.intellij.openapi.project.Project
import com.intellij.psi.FileViewProvider
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiFile
import com.intellij.psi.tree.IFileElementType
import com.intellij.psi.tree.TokenSet
import com.intellij.psi.util.PsiUtilCore
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpLexer
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.FSharpElementTypes
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.FSharpFileImpl
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.FSharpScriptImpl
import com.jetbrains.rider.util.idea.getLogger

open class FSharpParserDefinition : ParserDefinition {
    override fun createLexer(project: Project?): Lexer = FSharpLexer()

    override fun createParser(project: Project): PsiParser = FSharpDummyParser(project)

    override fun getFileNodeType(): IFileElementType = FSharpElementTypes.FILE

    override fun getCommentTokens(): TokenSet = FSharpTokenType.COMMENTS

    override fun getStringLiteralElements(): TokenSet = FSharpTokenType.STRINGS

    override fun createElement(p0: ASTNode?): PsiElement {
        getLogger<FSharpParserDefinition>().error("createElement is not expected to be called!")
        return PsiUtilCore.NULL_PSI_ELEMENT
    }

    override fun createFile(viewProvider: FileViewProvider): PsiFile {
        return FSharpFileImpl(viewProvider)
    }
}

class FSharpScriptParserDefinition : FSharpParserDefinition() {
    override fun createFile(viewProvider: FileViewProvider): PsiFile {
        return FSharpScriptImpl(viewProvider)
    }
}