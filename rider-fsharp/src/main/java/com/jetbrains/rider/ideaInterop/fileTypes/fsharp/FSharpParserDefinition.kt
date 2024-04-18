package com.jetbrains.rider.ideaInterop.fileTypes.fsharp

import com.intellij.lang.ASTNode
import com.intellij.lang.ParserDefinition
import com.intellij.lang.PsiParser
import com.intellij.openapi.project.Project
import com.intellij.psi.FileViewProvider
import com.intellij.psi.PsiElement
import com.intellij.psi.tree.IFileElementType
import com.intellij.psi.tree.TokenSet
import com.intellij.psi.util.PsiUtilCore
import com.jetbrains.rd.platform.util.getLogger
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpLexer
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpFile
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.FSharpElementTypes
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.FSharpFileImpl
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.FSharpScriptFileImpl

class FSharpParserDefinition : ParserDefinition {
    private val logger = getLogger<FSharpParserDefinition>()
    override fun getWhitespaceTokens() = TokenSet.create(FSharpTokenType.NEW_LINE, FSharpTokenType.WHITESPACE)
    override fun createLexer(project: Project?) = FSharpLexer()
    override fun createParser(project: Project): PsiParser = FSharpDummyParser()
    override fun getCommentTokens(): TokenSet = FSharpTokenType.COMMENTS
    override fun getStringLiteralElements(): TokenSet = FSharpTokenType.ALL_STRINGS
    override fun createElement(node: ASTNode): PsiElement {
        if (node is PsiElement) {
            logger.error("Dummy blocks should be lazy and not parsed like this")
            return node
        }

        logger.error("An attempt to parse unexpected element")
        return PsiUtilCore.NULL_PSI_ELEMENT
    }

    override fun createFile(viewProvider: FileViewProvider) =
        when (val fileType = viewProvider.fileType) {
            FSharpFileType -> FSharpFileImpl(viewProvider)
            FSharpScriptFileType -> FSharpScriptFileImpl(viewProvider)
            else -> error("Unexpected file type $fileType")
      }

    override fun getFileNodeType(): IFileElementType = FSharpElementTypes.FILE
}
