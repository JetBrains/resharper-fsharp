package com.jetbrains.rider.ideaInterop.fileTypes.fsharp

import com.intellij.lang.ASTNode
import com.intellij.openapi.project.Project
import com.intellij.psi.tree.IElementType
import com.intellij.lang.PsiParser
import com.intellij.lang.PsiBuilder
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl.FSharpElementTypes

/**
 * Warning:
 * This parser does not construct correct F# tree.
 * Instead it builds tree with blocks needed for frontend features only.
 */
class FSharpDummyParser(project: Project) : PsiParser {
    override fun parse(root: IElementType, builder: PsiBuilder): ASTNode {
        when (root) {
            FSharpElementTypes.FILE -> parseFile(builder, root)
            FSharpElementTypes.INDENTATION_BLOCK -> tryParseIndentationBlock(builder)
            FSharpElementTypes.LINE -> parseLine(builder)
            else -> {
                error("unknown root $root in parsing request")
            }
        }
        return builder.treeBuilt
    }

    private fun parseFile(builder: PsiBuilder, fileType : IElementType) {
        val marker = builder.mark()

        tryParseIndentationBlock(builder)

        assert(builder.eof()) { "Parsing finished before the file end" }
        marker.done(fileType)
    }

    private fun tryParseIndentationBlock(builder: PsiBuilder, parentIndentation : Int = -1) : PsiBuilder.Marker? {
        val myIndentation = getCurrentIndentation(builder)
        if (myIndentation <= parentIndentation) return null

        val blockMarker = if (parentIndentation != -1) builder.mark() else null
        // used to eliminate extra empty lines and new_lines at block end
        var lastEndOfLineMarker : PsiBuilder.Marker? = null

        fun finishBlock() : PsiBuilder.Marker?  {
            if (blockMarker == null) {
                lastEndOfLineMarker?.drop()
                return null
            }
            return if (lastEndOfLineMarker == null) {
                blockMarker.done(FSharpElementTypes.INDENTATION_BLOCK)
                builder.mark()
            } else {
                blockMarker.doneBefore(FSharpElementTypes.INDENTATION_BLOCK, lastEndOfLineMarker!!)
                lastEndOfLineMarker
            }
        }

        while (!builder.eof()) {
            lastEndOfLineMarker = tryParseIndentationBlock(builder, myIndentation)

            if (lastEndOfLineMarker == null) {
                parseLine(builder)
                if (builder.eof()) return finishBlock()

                lastEndOfLineMarker = builder.mark()
                builder.advanceLexer()
            }

            if (trySkipEmptyLines(builder) && builder.eof()) {
                return finishBlock()
            }

            val nextLineIndentation = getCurrentIndentation(builder)
            if (myIndentation > nextLineIndentation) return finishBlock()

            lastEndOfLineMarker.drop()
            lastEndOfLineMarker = null
        }
        return finishBlock()
    }

    private fun getCurrentIndentation(builder: PsiBuilder) : Int {
        return if (builder.tokenType == FSharpTokenType.WHITESPACE) builder.tokenText!!.length else 0
    }

    private fun parseLine(builder: PsiBuilder) {
        val lineMarker = builder.mark()
        while (!builder.eof() && builder.tokenType != FSharpTokenType.NEW_LINE) {
            if (parseStringExpression(builder) == null)
                builder.advanceLexer()
        }

        lineMarker.done(FSharpElementTypes.LINE)
    }

    private fun parseStringExpression(builder: PsiBuilder): PsiBuilder.Marker? {
        if (FSharpTokenType.STRINGS.contains(builder.tokenType)) {
            val string = builder.mark()
            builder.advanceLexer()
            string.done(FSharpElementTypes.STRING_LITERAL_EXPRESSION)
            return string
        }
        return null
    }

    private fun tryMoveToNextLine(builder: PsiBuilder) : Boolean {
        while (!builder.eof() && builder.tokenType != FSharpTokenType.NEW_LINE) {
            builder.advanceLexer()
        }
        if (builder.eof()) return false
        builder.advanceLexer()
        return true
    }

    private fun trySkipEmptyLines(builder: PsiBuilder) : Boolean {
        var wasSkipped = false
        while (isLineEmpty(builder)) {
            wasSkipped = true
            if (!tryMoveToNextLine(builder)) break
        }
        return wasSkipped
    }

    private fun isLineEmpty(builder: PsiBuilder): Boolean {
        var tokenType = builder.tokenType
        if (builder.tokenType == FSharpTokenType.WHITESPACE) {
            tokenType = builder.lookAhead(1)
        }
        return tokenType == FSharpTokenType.NEW_LINE
    }
}