package com.jetbrains.rider.plugins.fsharp.editorActions

import com.intellij.codeInsight.editorActions.BackspaceHandler
import com.intellij.openapi.actionSystem.DataContext
import com.intellij.openapi.editor.Caret
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.ScrollType
import com.intellij.openapi.editor.actionSystem.EditorActionHandler
import com.intellij.openapi.editor.highlighter.HighlighterIterator
import com.intellij.psi.tree.IElementType
import com.jetbrains.rider.editors.offsetToDocCoordinates
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType

class FSharpBackspaceHandlerDelegate(val originalHandler: EditorActionHandler) : BackspaceHandler(originalHandler) {
  override fun executeWriteAction(editor: Editor, caret: Caret, dataContext: DataContext?) {
// If we have not only whitespaces on the left of caret, execute base handler.
    if (dataContext == null || editor.caretModel.caretCount != 1 || !handleBackspace(editor, caret)) {
      myOriginalHandler.execute(editor, caret, dataContext)
    }
  }

  private fun handleBackspace(editor: Editor, caret: Caret): Boolean {
    if (handleBackspaceInInterpolatedString(editor, caret)) return true
    if (handleBackspaceInTripleQuotedString(editor, caret)) return true
    return doHandleBackspacePressed(
      editor, caret,
      { tokenType ->
        tokenType == FSharpTokenType.STRING ||
          tokenType == FSharpTokenType.REGULAR_INTERPOLATED_STRING ||
          tokenType == FSharpTokenType.VERBATIM_STRING ||
          tokenType == FSharpTokenType.VERBATIM_INTERPOLATED_STRING
      },
      { FSharpBracketMatcher() }
    )
  }

  fun handleBackspaceInInterpolatedString(editor: Editor, caret: Caret): Boolean {

    val caretOffset = caret.offset
    val iterator = editor.highlighter.createIterator(caretOffset)
    if (caretOffset <= 0 || iterator.tokenType == null) return false

    if (iterator.start != caretOffset) return false
    val tokenType = iterator.tokenType
    if (tokenType != FSharpTokenType.REGULAR_INTERPOLATED_STRING_MIDDLE &&
      tokenType != FSharpTokenType.REGULAR_INTERPOLATED_STRING_END &&
      tokenType != FSharpTokenType.VERBATIM_INTERPOLATED_STRING_MIDDLE &&
      tokenType != FSharpTokenType.VERBATIM_INTERPOLATED_STRING_END &&
      tokenType != FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_MIDDLE &&
      tokenType != FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_END
    ) {
      return false
    }

    iterator.retreat()
    val prevTokenType = iterator.tokenType
    if (prevTokenType != FSharpTokenType.REGULAR_INTERPOLATED_STRING_START &&
      prevTokenType != FSharpTokenType.REGULAR_INTERPOLATED_STRING_MIDDLE &&
      prevTokenType != FSharpTokenType.VERBATIM_INTERPOLATED_STRING_START &&
      prevTokenType != FSharpTokenType.VERBATIM_INTERPOLATED_STRING_MIDDLE &&
      prevTokenType != FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_START &&
      prevTokenType != FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_MIDDLE
    ) {
      return false
    }

    caret.moveToOffset(caretOffset - 1)
    editor.scrollingModel.scrollToCaret(ScrollType.MAKE_VISIBLE)
    editor.document.deleteString(caretOffset - 1, caretOffset + 1)
    return true
  }

  fun handleBackspaceInTripleQuotedString(editor: Editor, caret: Caret): Boolean {
    val offset = caret.offset

    //TODO
    fun isTripleQuoteString(tokenType: IElementType?) =
      tokenType == FSharpTokenType.TRIPLE_QUOTED_STRING ||
        tokenType == FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING

    val iterator = editor.highlighter.createIterator(offset)
    if (iterator.tokenType == null) return false
    if (!isTripleQuoteString(iterator.tokenType) || iterator.start == offset) return false

    //TODO
    fun getStrStart(tokenType: IElementType): String {
      return when (tokenType) {
        FSharpTokenType.TRIPLE_QUOTED_STRING -> "\"\"\""
        FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING -> "$\"\"\""
        else -> throw IllegalArgumentException("Unexpected token type: $tokenType")
      }
    }

    val strStart = getStrStart(iterator.tokenType)
    val strEnd = "\"\"\""
    val newLineLength = "\n".length

    // """{caret}"""
    if (iterator.start == offset - strStart.length && iterator.end == offset + strEnd.length) {
      editor.document.deleteString(offset - 1, offset + 3)
      caret.moveToOffset(offset - 1)
      editor.scrollingModel.scrollToCaret(ScrollType.MAKE_VISIBLE)
      return true
    }

    // """\n{caret} text here \n"""
    if (iterator.start + strStart.length + newLineLength == offset) {
      val document = editor.document
      val strStartCoords = document.offsetToDocCoordinates(iterator.start)
      val strEndCoords = document.offsetToDocCoordinates(iterator.end)

      val caretLine = document.getLineNumber(offset)

      if (caretLine != strStartCoords.line + 1) return false
      if (offset != document.getLineStartOffset(caretLine)) return false
      if (strStartCoords.line + 2 != strEndCoords.line) return false
      if (strEndCoords.column != 3) return false

      val lastNewLineOffset = iterator.end - strEnd.length - newLineLength
      editor.document.deleteString(lastNewLineOffset, lastNewLineOffset + newLineLength)
      editor.document.deleteString(offset - newLineLength, offset)
      editor.scrollingModel.scrollToCaret(ScrollType.MAKE_VISIBLE)
      return true
    }

    return false
  }

  fun doHandleBackspacePressed(
    editor: Editor,
    caret: Caret,
    isStringLiteralToken: (IElementType) -> Boolean,
    createBracketMatcher: () -> BracketMatcher
  ): Boolean {

    val caretOffset = caret.offset
    //TODO
    if (caretOffset <= 0) return false

    val iterator = editor.highlighter.createIterator(caretOffset)
    if (iterator.tokenType == null) return false

    if (isStringLiteralToken(iterator.tokenType)) return handleBackspaceInString(editor, caret, iterator)

    if (iterator.start != caretOffset) return false

    val rightBracketPos = iterator.start
    val bracketMatcher = createBracketMatcher()

    iterator.retreat()
    val prevTokenType = iterator.tokenType
    if (bracketMatcher.findMatchingBracket(iterator) == null || iterator.start != rightBracketPos) {
      return false
    }

    // Найти самый левый не закрытый скобочный символ текущего типа (включая перед кареткой), чтобы не было открытых скобок другого типа
    var leftBracketPos = iterator.start
    do {
      if (iterator.tokenType == prevTokenType && bracketMatcher.isStackEmpty()) {
        leftBracketPos = iterator.start
      } else if (!bracketMatcher.proceedStack(iterator.tokenType)) {
        break
      }
      iterator.retreat()
    } while (!iterator.atEnd())

    val iterator2 = editor.highlighter.createIterator(leftBracketPos)

    // Попытка найти подходящую закрывающую скобку
    val caretOffset2 = caretOffset - 1
    caret.moveToOffset(caretOffset2)
    editor.scrollingModel.scrollToCaret(ScrollType.MAKE_VISIBLE)
    editor.document.deleteString(
      caretOffset2,
      caretOffset2 + if (bracketMatcher.findMatchingBracket(iterator2) != null) 2 else 1
    )

    return true
  }

  fun handleBackspaceInString(editor: Editor, caret: Caret, iterator: HighlighterIterator): Boolean {
    val caretOffset = caret.offset

    val buffer = editor.document.charsSequence
    val prevChar = buffer[caretOffset - 1]
    if (prevChar != '\'' && prevChar != '\"') {
      return false
    }

    if (caretOffset != iterator.end - 1 || prevChar != buffer[caretOffset]) return false

    val escaped = caretOffset > 1 && buffer[caretOffset - 2] == '\\'

    if (!escaped) {
      caret.moveToOffset(caretOffset - 1)
      editor.scrollingModel.scrollToCaret(ScrollType.MAKE_VISIBLE)
      editor.document.deleteString(caretOffset - 1, caretOffset + 1)
    } else {
      caret.moveToOffset(caretOffset - 2)
      editor.scrollingModel.scrollToCaret(ScrollType.MAKE_VISIBLE)
      editor.document.deleteString(caretOffset - 2, caretOffset)
    }

    return true
  }

}
