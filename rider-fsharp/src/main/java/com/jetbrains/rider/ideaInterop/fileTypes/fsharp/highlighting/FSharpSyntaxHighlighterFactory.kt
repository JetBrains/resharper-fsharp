package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.highlighting


import com.intellij.lexer.Lexer
import com.intellij.openapi.editor.*
import com.intellij.openapi.editor.colors.TextAttributesKey
import com.intellij.openapi.fileTypes.SyntaxHighlighterBase
import com.intellij.psi.TokenType
import com.intellij.psi.tree.IElementType

import com.intellij.openapi.editor.colors.TextAttributesKey.createTextAttributesKey
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpLexer
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.openapi.fileTypes.SyntaxHighlighter
import com.intellij.openapi.fileTypes.SyntaxHighlighterFactory

import com.intellij.openapi.project.Project
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpElementTypes.*

class FSharpSyntaxHighlighter : SyntaxHighlighterBase() {

    override fun getHighlightingLexer(): Lexer {
        return FSharpLexer()
    }

    override fun getTokenHighlights(tokenType: IElementType): Array<TextAttributesKey> {
        return if (tokenType == STRING || tokenType == UNFINISHED_STRING || tokenType == UNFINISHED_VERBATIM_STRING ||
                tokenType == UNFINISHED_TRIPLE_QUOTED_STRING) {
            STRING_KEYS
        } else if (tokenType == END_OF_LINE_COMMENT || tokenType == BLOCK_COMMENT || tokenType == UNFINISHED_BLOCK_COMMENT ||
                tokenType == UNFINISHED_STRING_IN_COMMENT || tokenType == UNFINISHED_VERBATIM_STRING_IN_COMMENT ||
                tokenType == UNFINISHED_TRIPLE_QUOTED_STRING_IN_COMMENT) {
            COMMENT_KEYS
        } else if (tokenType == IDENT_KEYWORD) {
            IDENT_KEYWORD_KEYS
        } else if (tokenType == TokenType.BAD_CHARACTER) {
            BAD_CHAR_KEYS
        } else {
            EMPTY_KEYS
        }
    }

    companion object {
        val LITERAL_STRING = createTextAttributesKey("FSHARP_STRING", DefaultLanguageHighlighterColors.STRING)
        val COMMENT = createTextAttributesKey("FSHARP_COMMENT", DefaultLanguageHighlighterColors.LINE_COMMENT)
        val IDENT_KEYWORDS = createTextAttributesKey("FSHARP_KEYWORD", DefaultLanguageHighlighterColors.KEYWORD)
        val BAD_CHARACTER = createTextAttributesKey("FSHARP_BAD_CHARACTER", HighlighterColors.BAD_CHARACTER)

        private val BAD_CHAR_KEYS = arrayOf(BAD_CHARACTER)
        private val STRING_KEYS = arrayOf(LITERAL_STRING)
        private val COMMENT_KEYS = arrayOf(COMMENT)
        private val IDENT_KEYWORD_KEYS = arrayOf(IDENT_KEYWORDS)
        private val EMPTY_KEYS = arrayOf<TextAttributesKey>()
    }
}

class FSharpSyntaxHighlighterFactory : SyntaxHighlighterFactory() {
    override fun getSyntaxHighlighter(project: Project?, virtualFile: VirtualFile?): SyntaxHighlighter {
        return FSharpSyntaxHighlighter()
    }
}