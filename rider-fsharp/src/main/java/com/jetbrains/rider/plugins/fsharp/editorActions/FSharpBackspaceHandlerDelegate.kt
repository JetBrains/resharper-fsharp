package com.jetbrains.rider.plugins.fsharp.editorActions

import com.intellij.codeInsight.editorActions.BackspaceHandler
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.actionSystem.DataContext
import com.intellij.openapi.editor.Caret
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.ScrollType
import com.intellij.openapi.editor.actionSystem.EditorActionHandler
import com.intellij.openapi.editor.highlighter.HighlighterIterator
import com.intellij.psi.tree.IElementType
import com.jetbrains.rdclient.patches.isPatchEngineEnabled
import com.jetbrains.rider.editors.offsetToDocCoordinates
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguage
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType

class FSharpBackspaceHandlerDelegate(originalHandler: EditorActionHandler) : BackspaceHandler(originalHandler) {
  override fun executeWriteAction(editor: Editor, caret: Caret, dataContext: DataContext?) {
    if (!isPatchEngineEnabled ||
      dataContext == null ||
      dataContext.getData(CommonDataKeys.PSI_FILE)?.language !is FSharpLanguage ||
      editor.caretModel.caretCount != 1 ||
      caret.offset <= 0 ||
      !handleBackspace(editor, caret)
    ) {
      myOriginalHandler.execute(editor, caret, dataContext)
    }
  }

  private fun handleBackspace(editor: Editor, caret: Caret): Boolean {
    if (handleBackspaceInInterpolatedString(editor, caret)) return true
    if (handleBackspaceInTripleQuotedString(editor, caret)) return true
    return doHandleBackspacePressed(
      editor, caret
    ) { tokenType ->
      tokenType == FSharpTokenType.STRING ||
        tokenType == FSharpTokenType.REGULAR_INTERPOLATED_STRING ||
        tokenType == FSharpTokenType.VERBATIM_STRING ||
        tokenType == FSharpTokenType.VERBATIM_INTERPOLATED_STRING
    }
  }

  fun handleBackspaceInInterpolatedString(editor: Editor, caret: Caret): Boolean {
    val caretOffset = caret.offset
    val iterator = editor.highlighter.createIterator(caretOffset)
    if (iterator.atEnd()) return false

    if (iterator.start != caretOffset) return false
    val tokenType = iterator.tokenType
    if (!FSharpTokenType.INTERPOLATED_STRING_MIDDLES.contains(tokenType) &&
      !FSharpTokenType.INTERPOLATED_STRING_ENDS.contains(tokenType)
    ) return false

    iterator.retreat()
    val prevTokenType = iterator.tokenType
    if (!FSharpTokenType.INTERPOLATED_STRING_STARTS.contains(prevTokenType) &&
      !FSharpTokenType.INTERPOLATED_STRING_MIDDLES.contains(prevTokenType)
    ) return false

    caret.moveToOffset(caretOffset - 1)
    editor.scrollingModel.scrollToCaret(ScrollType.MAKE_VISIBLE)
    editor.document.deleteString(caretOffset - 1, caretOffset + 1)
    return true
  }

  fun handleBackspaceInTripleQuotedString(editor: Editor, caret: Caret): Boolean {
    val offset = caret.offset

    val iterator = editor.highlighter.createIterator(offset)
    if (iterator.atEnd()) return false

    val tokenType = iterator.tokenType

    if (tokenType != FSharpTokenType.TRIPLE_QUOTED_STRING &&
      tokenType != FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING
      || iterator.start == offset
    ) return false

    val strStart = when (tokenType) {
      FSharpTokenType.TRIPLE_QUOTED_STRING -> "\"\"\""
      FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING -> "$\"\"\""
      else -> throw IllegalArgumentException("Unexpected token type: $tokenType")
    }

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
  ): Boolean {

    val caretOffset = caret.offset

    val iterator = editor.highlighter.createIterator(caretOffset)
    if (iterator.atEnd()) return false

    if (isStringLiteralToken(iterator.tokenType))
      return handleBackspaceInString(editor, caret, iterator)

    if (iterator.start != caretOffset) return false

    val rightBracketPos = iterator.start
    val bracketMatcher = FSharpBracketMatcher()

    iterator.retreat()
    val prevTokenType = iterator.tokenType
    if (bracketMatcher.findMatchingBracket(iterator.asTokenIterator()) == null || iterator.start != rightBracketPos) {
      return false
    }

    // Find the leftmost unclosed parenthesis of the current type
    // (including those before the cursor) such that there are no open parentheses of another type.
    var leftBracketPos = iterator.start
    do {
      if (iterator.tokenType == prevTokenType && bracketMatcher.isStackEmpty()) {
        leftBracketPos = iterator.start
      } else if (!bracketMatcher.proceedStack(iterator.tokenType)) {
        break
      }
      iterator.retreat()
    } while (!iterator.atEnd())

    val iterator2 = editor.highlighter.createIterator(leftBracketPos).asTokenIterator()

    // Attempt to find a matching closing bracket
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
