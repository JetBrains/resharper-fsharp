package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi

import com.intellij.openapi.util.TextRange
import com.intellij.openapi.util.text.StringUtil
import com.intellij.psi.ElementManipulator

class FSharpStringElementManipulator : ElementManipulator<FSharpStringLiteralExpression> {
    override fun handleContentChange(element: FSharpStringLiteralExpression,
                                     newContent: String): FSharpStringLiteralExpression? {
        return handleContentChange(element, getRangeInElement(element), newContent)
    }

    override fun handleContentChange(element: FSharpStringLiteralExpression,
                                     range: TextRange, newContent: String): FSharpStringLiteralExpression? {
        val oldText = element.text
        var newText = when (element.literalType) {
            FSharpStringLiteralType.RegularString,
            FSharpStringLiteralType.ByteArray -> StringUtil.escapeStringCharacters(newContent)
            FSharpStringLiteralType.VerbatimString -> newContent.replace("\"\"", "\"").replace("\"", "\"\"")
            FSharpStringLiteralType.TripleQuotedString -> newContent
            else -> error("invalid literal type")
        }

        newText = oldText.substring(0, range.startOffset) + newText + oldText.substring(range.endOffset)

        return element.updateText(newText) as FSharpStringLiteralExpression
    }

    override fun getRangeInElement(element: FSharpStringLiteralExpression): TextRange {
        val end = element.textLength
        return when (element.literalType) {
            FSharpStringLiteralType.RegularString -> TextRange(1, end - 1)
            FSharpStringLiteralType.VerbatimString -> TextRange(2, end - 1)
            FSharpStringLiteralType.TripleQuotedString -> TextRange(3, end - 3)
            FSharpStringLiteralType.ByteArray -> TextRange(1, end - 2)
            else -> error("invalid literal type ${element.literalType}")
        }
    }
}
