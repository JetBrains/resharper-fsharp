package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.highlighting


import com.intellij.lexer.Lexer
import com.intellij.openapi.editor.HighlighterColors
import com.intellij.openapi.editor.colors.TextAttributesKey
import com.intellij.openapi.fileTypes.SyntaxHighlighter
import com.intellij.openapi.fileTypes.SyntaxHighlighterBase
import com.intellij.openapi.fileTypes.SyntaxHighlighterFactory
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.psi.TokenType
import com.intellij.psi.tree.IElementType
import com.jetbrains.rider.ideaInterop.RiderTextAttributeKeys
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpLexer
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType.*

class FSharpSyntaxHighlighter : SyntaxHighlighterBase() {
    companion object {
        private val keywords =
                IDENT_KEYWORDS.types.map { it to RiderTextAttributeKeys.KEYWORD }
        private val pp_keywords =
                PP_KEYWORDS.types.map { it to RiderTextAttributeKeys.KEYWORD }
        private val strings =
                STRINGS.types.map { it to RiderTextAttributeKeys.STRING }
        private val comments =
                COMMENTS.types.map { it to RiderTextAttributeKeys.BLOCK_COMMENT }
        private val numbers =
                NUMBERS.types.map { it to RiderTextAttributeKeys.NUMBER }
        private val ourKeys = mapOf(
                CHARACTER_LITERAL to RiderTextAttributeKeys.STRING,
                BYTECHAR to RiderTextAttributeKeys.STRING,
                LINE_COMMENT to RiderTextAttributeKeys.COMMENT,
                TokenType.BAD_CHARACTER to HighlighterColors.BAD_CHARACTER
        ) + keywords + pp_keywords + comments + strings + numbers
    }

    override fun getHighlightingLexer(): Lexer {
        return FSharpLexer()
    }

    override fun getTokenHighlights(tokenType: IElementType): Array<TextAttributesKey> {
        return pack(FSharpSyntaxHighlighter.ourKeys[tokenType])
    }
}

class FSharpSyntaxHighlighterFactory : SyntaxHighlighterFactory() {
    override fun getSyntaxHighlighter(project: Project?, virtualFile: VirtualFile?): SyntaxHighlighter {
        return FSharpSyntaxHighlighter()
    }
}
