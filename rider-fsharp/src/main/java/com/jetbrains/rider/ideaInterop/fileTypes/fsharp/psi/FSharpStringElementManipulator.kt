package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi

import com.intellij.openapi.util.TextRange
import com.intellij.openapi.util.text.StringUtil
import com.intellij.psi.ElementManipulator

class FSharpStringElementManipulator : ElementManipulator<FSharpStringLiteralExpression> {
  override fun handleContentChange(
    element: FSharpStringLiteralExpression,
    newContent: String
  ): FSharpStringLiteralExpression = handleContentChange(element, getRangeInElement(element), newContent)

  private fun escapeRegularString(str: String): String {
    val buffer = StringBuilder()
    val lastCharIndex = str.length - 1
    var isEscaped = false
    str.forEachIndexed { i, ch ->
      when (ch) {
        '"' -> buffer.append("\\\"")
        '\b' -> buffer.append("\\b")
        '\t' -> buffer.append("\\t")
        '\n' -> buffer.append('\n')
        '\u000c' -> buffer.append("\\f")
        '\r' -> buffer.append("\\r")
        '\\' -> buffer.append(if (!isEscaped && i == lastCharIndex) "\\\\" else "\\")
        else -> if (!StringUtil.isPrintableUnicode(ch)) {
          val hexCode: CharSequence = StringUtil.toUpperCase(Integer.toHexString(ch.code))
          buffer.append("\\u")
          var paddingCount = 4 - hexCode.length
          while (paddingCount-- > 0) {
            buffer.append(0)
          }
          buffer.append(hexCode)
        } else {
          buffer.append(ch)
        }
      }
      isEscaped = ch == '\\' && !isEscaped
    }
    return buffer.toString()
  }

  override fun handleContentChange(
    element: FSharpStringLiteralExpression, range: TextRange, newContent: String
  ): FSharpStringLiteralExpression {
    val oldText = element.text
    var newText = newContent
    val elementType = element.literalType

    if (elementType.isRegular) newText = escapeRegularString(newText)

    if (elementType.isInterpolated) newText =
      newText
        .replace("{{", "{").replace("}}", "}")
        .replace("{", "{{").replace("}", "}}")

    if (elementType.isVerbatim) newText =
      newText.replace("\"\"", "\"").replace("\"", "\"\"")

    newText = oldText.substring(0, range.startOffset) + newText + oldText.substring(range.endOffset)
    return element.updateText(newText) as FSharpStringLiteralExpression
  }

  override fun getRangeInElement(element: FSharpStringLiteralExpression) = element.getRelativeRangeTrimQuotes()
}
