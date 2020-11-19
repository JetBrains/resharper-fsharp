package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.highlighting

import com.intellij.openapi.editor.HighlighterColors
import com.intellij.openapi.editor.colors.TextAttributesKey
import com.intellij.openapi.fileTypes.SyntaxHighlighterBase
import com.intellij.psi.TokenType
import com.intellij.psi.tree.IElementType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpLexer
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType.*

class FSharpSyntaxHighlighter : SyntaxHighlighterBase() {
    companion object {
        private val keywords = IDENT_KEYWORDS.types.map { it to FSharpTextAttributeKeys.KEYWORD }
        private val pp_keywords = PP_KEYWORDS.types.map { it to FSharpTextAttributeKeys.PREPROCESSOR_KEYWORD }
        private val strings = STRINGS.types.map { it to FSharpTextAttributeKeys.STRING }
        private val interpolated_strings = INTERPOLATED_STRINGS.types.map { it to FSharpTextAttributeKeys.STRING }
        private val comments = COMMENTS.types.map { it to FSharpTextAttributeKeys.BLOCK_COMMENT }
        private val numbers = NUMBERS.types.map { it to FSharpTextAttributeKeys.NUMBER }

        private val ourKeys = mapOf(
                CHARACTER_LITERAL to FSharpTextAttributeKeys.STRING,
                BYTECHAR to FSharpTextAttributeKeys.STRING,
                LINE_COMMENT to FSharpTextAttributeKeys.COMMENT,
                TokenType.BAD_CHARACTER to HighlighterColors.BAD_CHARACTER
        ) + keywords + pp_keywords + comments + strings + interpolated_strings + numbers
    }

    override fun getHighlightingLexer() = FSharpLexer()
    override fun getTokenHighlights(tokenType: IElementType): Array<TextAttributesKey> = pack(ourKeys[tokenType])
}
