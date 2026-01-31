package com.jetbrains.rider.plugins.fsharp.editorActions

import com.intellij.application.options.CodeStyle.getIndentSize
import com.intellij.codeInsight.editorActions.enter.EnterHandlerDelegate.Result
import com.intellij.codeInsight.editorActions.enter.EnterHandlerDelegateAdapter
import com.intellij.openapi.actionSystem.DataContext
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.editor.Document
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.ScrollType
import com.intellij.openapi.editor.actionSystem.EditorActionHandler
import com.intellij.openapi.editor.highlighter.HighlighterIterator
import com.intellij.openapi.util.Ref
import com.intellij.psi.PsiDocumentManager
import com.intellij.psi.PsiFile
import com.intellij.psi.tree.IElementType
import com.intellij.psi.tree.TokenSet
import com.intellij.psi.util.parentOfType
import com.intellij.psi.util.startOffset
import com.intellij.util.text.CharArrayUtil
import com.jetbrains.rdclient.patches.isPatchEngineEnabled
import com.jetbrains.rider.editors.getPsiFile
import com.jetbrains.rider.editors.offsetToDocCoordinates
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpLanguage
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType.LINE_COMMENT
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpInterpolatedStringLiteralExpression
import com.jetbrains.rider.plugins.fsharp.editorActions.LineIndent.Type.Comments
import com.jetbrains.rider.plugins.fsharp.editorActions.LineIndent.Type.Source
import java.lang.Character.isWhitespace

private val logger = Logger.getInstance(FSharpEnterHandlerDelegate::class.java)

class FSharpEnterHandlerDelegate : EnterHandlerDelegateAdapter() {
  private val indentFromToken: TokenSet = TokenSet.create(
    FSharpTokenType.LBRACK_LESS,
    FSharpTokenType.LQUOTE_TYPED,
    FSharpTokenType.LQUOTE_UNTYPED,
    FSharpTokenType.STRUCT,
    FSharpTokenType.CLASS,
    FSharpTokenType.INTERFACE,
    FSharpTokenType.TRY,
    FSharpTokenType.NEW,
    FSharpTokenType.LAZY
  )

  private val allowKeepIndent: TokenSet = TokenSet.create(
    FSharpTokenType.LPAREN,
    FSharpTokenType.LBRACK,
    FSharpTokenType.LBRACE,
    FSharpTokenType.LBRACK_BAR,
    FSharpTokenType.EQUALS,
    FSharpTokenType.LARROW,
    FSharpTokenType.RARROW,
    FSharpTokenType.IF,
    FSharpTokenType.THEN,
    FSharpTokenType.ELIF,
    FSharpTokenType.ELSE,
    FSharpTokenType.MATCH,
    FSharpTokenType.WHILE,
    FSharpTokenType.WHEN,
    FSharpTokenType.DO,
    FSharpTokenType.DO_BANG,
    FSharpTokenType.YIELD,
    FSharpTokenType.YIELD_BANG,
    FSharpTokenType.BEGIN
  )

  private val indentFromPrevLine: TokenSet = TokenSet.create(
    FSharpTokenType.FUNCTION,
    FSharpTokenType.EQUALS,
    FSharpTokenType.LARROW,
    FSharpTokenType.MATCH,
    FSharpTokenType.WHILE,
    FSharpTokenType.WHEN,
    FSharpTokenType.DO,
    FSharpTokenType.DO_BANG,
    FSharpTokenType.YIELD,
    FSharpTokenType.YIELD_BANG,
    FSharpTokenType.BEGIN
  )

  private val indentTokens: TokenSet = TokenSet.orSet(indentFromToken, indentFromPrevLine)

  private val deindentingTokens: TokenSet = TokenSet.create(
    FSharpTokenType.RPAREN,
    FSharpTokenType.RBRACK,
    FSharpTokenType.BAR_RBRACK,
    FSharpTokenType.GREATER_RBRACK,
    FSharpTokenType.RQUOTE_TYPED,
    FSharpTokenType.RQUOTE_UNTYPED,
    FSharpTokenType.RBRACE,
    FSharpTokenType.END
  )

  private val emptyBracketsToAddSpace = setOf(
    Pair(FSharpTokenType.LBRACE, FSharpTokenType.RBRACE),
    Pair(FSharpTokenType.LBRACK, FSharpTokenType.RBRACK),
    Pair(FSharpTokenType.LBRACK_BAR, FSharpTokenType.BAR_RBRACK),
    Pair(FSharpTokenType.LBRACE_BAR, FSharpTokenType.BAR_RBRACE),
    Pair(FSharpTokenType.LQUOTE_TYPED, FSharpTokenType.RQUOTE_TYPED),
    Pair(FSharpTokenType.LQUOTE_UNTYPED, FSharpTokenType.RQUOTE_UNTYPED)
  )

  private val leftBracketsToAddIndent = TokenSet.create(
    FSharpTokenType.LPAREN,
    FSharpTokenType.LBRACE,
    FSharpTokenType.LBRACK,
    FSharpTokenType.LBRACK_BAR,
    FSharpTokenType.LBRACK_LESS,
    FSharpTokenType.LQUOTE_TYPED,
    FSharpTokenType.LQUOTE_UNTYPED
  )

  private val rightBracketsToAddSpace = emptyBracketsToAddSpace.map { it.second }.toSet()

  private fun isIgnored(tokenType: IElementType?) =
    tokenType != null && (tokenType == FSharpTokenType.WHITESPACE || tokenType.isComment)

  private fun shouldTrimSpacesBeforeToken(tokenType: IElementType?) =
    !(tokenType == null ||
      FSharpTokenType.RIGHT_BRACES.contains(tokenType) ||
      tokenType.isComment)

  private fun findUnmatchedBracketToLeft(iterator: HighlighterIterator, offset: Int, minOffset: Int): Boolean {
    if (iterator.end > offset) iterator.retreat()

    val matcher = FSharpBracketMatcher()
    var foundToken = false

    val tokenIterator = iterator.asTokenIterator()

    while (!foundToken && !iterator.atEnd() && tokenIterator.tokenStart >= minOffset) {
      if (FSharpTokenType.RIGHT_BRACES.contains(tokenIterator.tokenType)) {
        if (matcher.findMatchingBracket(tokenIterator) != null) {
          tokenIterator.retreat()
        }
      }

      if (FSharpTokenType.LEFT_BRACES.contains(tokenIterator.tokenType)) {
        foundToken = true
      } else if (!FSharpTokenType.RIGHT_BRACES.contains(tokenIterator.tokenType)) {
        tokenIterator.retreat()
      }
    }

    return foundToken
  }

  fun getAdditionalSpacesBeforeToken(editor: Editor, offset: Int, lineStart: Int): Int {
    val iterator = editor.highlighter.createIterator(offset)
    if (iterator.atEnd()) return 0

    // Always add a single space before -> so completion works nicely
    if (iterator.tokenTypeSafe == FSharpTokenType.RARROW) return 1

    if (!rightBracketsToAddSpace.contains(iterator.tokenTypeSafe)) return 0

    val rightBracketOffset = iterator.start
    if (!findUnmatchedBracketToLeft(iterator, offset, lineStart)) return 0

    val leftBracketEndOffset = iterator.end

    iterator.advance()
    while (iterator.tokenTypeSafe == FSharpTokenType.WHITESPACE) iterator.advance()

    // Empty list with spaces, add the same space as before caret.
    return if (iterator.start >= offset) {
      offset - leftBracketEndOffset - 1
    } else if (iterator.start == rightBracketOffset) {
      // Empty list with no spaces, no additional spaces should be added.
      0
    } else {
      // Space before first list element.
      iterator.start - leftBracketEndOffset
    }
  }

  fun trimTrailingSpacesAtOffset(
    editor: Editor,
    caretOffset: Int,
    trimAfterCaret: Boolean
  ): Int {
    val document = editor.document
    val line = document.getLineNumber(caretOffset)
    val lineStartOffset = document.getLineStartOffset(line)
    val buffer = document.charsSequence

    if (buffer.subSequence(lineStartOffset, caretOffset).all(::isWhitespace)) return caretOffset
    val lineEndOffset = document.getLineEndOffset(line)

    // skip whitespace before
    val startOffset = CharArrayUtil.shiftBackward(buffer, -1, caretOffset - 1, " ") + 1

    val endOffset =
      if (trimAfterCaret) CharArrayUtil.shiftForward(buffer, caretOffset, lineEndOffset, " ")
      else caretOffset

    val additionalSpaces =
      if (endOffset >= lineEndOffset) 0
      else getAdditionalSpacesBeforeToken(editor, endOffset, lineStartOffset)

    if (additionalSpaces > 0) {
      document.replaceString(startOffset, endOffset, " ".repeat(additionalSpaces))
      return startOffset
    } else if (startOffset != endOffset) {
      document.deleteString(startOffset, endOffset)
      return startOffset
    }
    return caretOffset
  }

  fun getContinuedIndentLine(
    editor: Editor,
    caretOffset: Int,
    continueByLeadingLParen: Boolean
  ): Int {
    val document = editor.document
    val line = document.getLineNumber(caretOffset)

    if (caretOffset == document.getLineStartOffset(line)) return line

    val iterator = editor.highlighter.createIterator(caretOffset - 1)
    if (iterator.atEnd()) return line

    tailrec fun tryFindContinuedLine(
      line: Int,
      lineStartOffset: Int,
      hasLeadingLeftBracket: Boolean
    ): Int {
      if (iterator.atEnd()) return line
      if (iterator.end <= lineStartOffset && !hasLeadingLeftBracket) return line

      val interpolatedStringExpr =
        if (!FSharpTokenType.INTERPOLATED_STRING_ENDINGS.contains(iterator.tokenTypeSafe)) null
        else {
          val manager = PsiDocumentManager.getInstance(editor.project!!)
          manager.commitDocument(document)
          val psiFileAfterCommit = manager.getPsiFile(editor.document)
          if (psiFileAfterCommit == null) null
          else {
            val elementAtCaret = psiFileAfterCommit.findElementAt(iterator.end - 1)
            val interpolatedString =
              elementAtCaret?.parentOfType<FSharpInterpolatedStringLiteralExpression>(true)
            interpolatedString
          }
        }

      interpolatedStringExpr?.let {
        val interpolatedStringExprStartOffset = it.startOffset
        while (iterator.start > interpolatedStringExprStartOffset) {
          iterator.retreat()
        }
      }

      val continuedLine = if (deindentingTokens.contains(iterator.tokenTypeSafe)) {
        if (FSharpBracketMatcher().findMatchingBracket(iterator.asTokenIterator()) == null) {
          line
        } else {
          document.getLineNumber(iterator.start)
        }
      } else {
        if (iterator.start >= lineStartOffset) line else {
          document.getLineNumber(iterator.start)
        }
      }

      val newLineStartOffset =
        if (line == continuedLine) lineStartOffset
        else document.getLineStartOffset(continuedLine)

      val hasLeadingLeftParen = continueByLeadingLParen &&
        (iterator.start > newLineStartOffset && iterator.tokenTypeSafe == FSharpTokenType.LPAREN) ||
        (hasLeadingLeftBracket && isIgnored(iterator.tokenTypeSafe))

      iterator.retreat()
      return tryFindContinuedLine(continuedLine, newLineStartOffset, hasLeadingLeftParen)
    }

    val lineStartOffset = document.getLineStartOffset(line)
    return tryFindContinuedLine(line, lineStartOffset, iterator.tokenTypeSafe == FSharpTokenType.LPAREN)
  }

  fun getLineWhitespaceIndent(editor: Editor, line: Int): Int {
    val document = editor.document
    val buffer = document.charsSequence
    val startOffset = document.getLineStartOffset(line)
    val endOffset = document.getLineEndOffset(line)

    val pos = buffer
      .substring(startOffset, endOffset)
      .takeWhile { it.isWhitespace() }.length + startOffset

    return pos - startOffset
  }

  fun insertText(
    editor: Editor,
    insertOffset: Int,
    text: String
  ) {
    editor.document.insertString(insertOffset, text)
    val newCaretPos = insertOffset + text.length
    editor.caretModel.moveToOffset(newCaretPos)
    editor.scrollingModel.scrollToCaret(ScrollType.MAKE_VISIBLE)
  }

  fun trimTrailingSpaces(editor: Editor, caretOffset: Int, trimAfterCaret: Boolean): Int {
    val caretOffset = trimTrailingSpacesAtOffset(editor, caretOffset, trimAfterCaret)
    return caretOffset
  }

  fun insertNewLineAt(editor: Editor, indent: Int, caretOffset: Int, trimAfterCaret: Boolean) {
    val insertPos = trimTrailingSpaces(editor, caretOffset, trimAfterCaret)
    val text = "\n" + " ".repeat(indent)
    insertText(editor, insertPos, text)
  }

  fun insertIndentFromLine(editor: Editor, line: Int, caretOffset: Int, trimSpacesAfterCaret: Boolean) {
    val indentSize = getLineWhitespaceIndent(editor, line)
    insertNewLineAt(editor, indentSize, caretOffset, trimSpacesAfterCaret)
  }

  fun isInsideString(editor: Editor, caretOffset: Int): Boolean {
    val iterator = editor.highlighter.createIterator(caretOffset - 1)
    return !iterator.atEnd() &&
      FSharpTokenType.ALL_STRINGS.contains(iterator.tokenType) &&
      caretOffset > iterator.start && caretOffset < iterator.end
  }

  private fun doDumpIndent(editor: Editor, caretOffset: Int, trimSpacesAfterCaret: Boolean) {
    if (isInsideString(editor, caretOffset)) {
      insertNewLineAt(editor, 0, caretOffset, trimSpacesAfterCaret)
      return
    }

    val document = editor.document
    val buffer = document.charsSequence
    val caretLine = document.getLineNumber(caretOffset)
    val line = getContinuedIndentLine(editor, caretOffset, false)

    if (line != caretLine) {
      insertIndentFromLine(editor, line, caretOffset, trimSpacesAfterCaret)
    } else {
      val startOffset = document.getLineStartOffset(line)

      val pos = CharArrayUtil.shiftForward(buffer, startOffset, caretOffset, " ")

      val indent = pos - startOffset
      insertNewLineAt(editor, indent, caretOffset, trimSpacesAfterCaret)
    }
  }

  fun handleEnterInTripleQuotedString(editor: Editor, caretOffset: Int): Boolean {
    val iterator = editor.highlighter.createIterator(caretOffset)
    if (iterator.atEnd()) return false

    // """{caret} foo"""
    if (iterator.tokenTypeSafe != FSharpTokenType.TRIPLE_QUOTED_STRING) return false
    if (caretOffset < iterator.start + 3 || caretOffset > iterator.end - 3) return false

    val document = editor.document
    val strStartLine = document.getLineNumber(iterator.start)
    val strEndLine = document.getLineNumber(iterator.end)
    if (strStartLine != strEndLine) return false

    document.insertString(iterator.end - 3, "\n")
    insertText(editor, caretOffset, "\n")
    return true
  }

  private fun getIndentBeforeToken(editor: Editor, offset: Int): String {
    val line = editor.document.getLineNumber(offset)
    val lineStartOffset = editor.document.getLineStartOffset(line)

    val indent = editor.document.charsSequence.substring(lineStartOffset, offset)
    return if (indent.isBlank()) {
      indent
    } else {
      " ".repeat(indent.length)
    }
  }

  private fun trimWhitespaceInLineCommentOnEnter(
    document: Document,
    iterator: HighlighterIterator,
    caretPosition: Int
  ): Int {
    var newCaretPosition = caretPosition
    val caretOffsetInComment = newCaretPosition - iterator.start
    val currTokenText = iterator.tokenText
    val firstCommentPartText = currTokenText.substring(0, caretOffsetInComment)

    // Trim comment text on the right unless it's a single whitespace
    for (i in firstCommentPartText.length - 1 downTo 0) {
      if (firstCommentPartText[i].isWhitespace()) continue

      val trimLength = firstCommentPartText.length - i - 1
      require(trimLength >= 0)

      val startOffset = newCaretPosition - trimLength
      val endOffset = newCaretPosition

      newCaretPosition -= trimLength
      require(newCaretPosition > 0) { "caretPos > 0" }

      document.deleteString(startOffset, endOffset)
      break
    }

    // Trim comment text on the left
    val secondCommentPartText = currTokenText.substring(caretOffsetInComment)
    for (i in secondCommentPartText.indices) {
      if (secondCommentPartText[i].isWhitespace() && i != secondCommentPartText.length - 1) continue

      document.deleteString(newCaretPosition, newCaretPosition + i)
      break
    }

    return newCaretPosition
  }

  private fun handleEnterInLineComment(
    editor: Editor,
    caretOffset: Int
  ): Boolean {
    val iterator = editor.highlighter.createIterator(caretOffset - 1)

    if (iterator.tokenTypeSafe != LINE_COMMENT) return false

    val lineCommentStart = "//"
    val docCommentStart = "///"

    if (caretOffset - iterator.start < lineCommentStart.length) return false

    val tokenText = iterator.tokenText
    if (!tokenText.startsWith(lineCommentStart)) return false

    if (!tokenText.startsWith(docCommentStart)) {
      val charCountAfterSelection = iterator.end - caretOffset
      val textAfterSelection = tokenText.substring(tokenText.length - charCountAfterSelection)
      if (textAfterSelection.isBlank()) return false
    }

    // Check that indentation is correct
    val indent = getIndentBeforeToken(editor, iterator.start)

    // Insert text and position cursor
    val currTokenText = iterator.tokenText
    val minimumCommentLength = minOf(lineCommentStart.length + 1, docCommentStart.length)
    require(currTokenText.length >= minimumCommentLength) {
      "Expected either a doc comment or non-empty line comment"
    }

    val commentStart = if (currTokenText.startsWith(docCommentStart)) {
      // Always add a leading space to doc comments
      currTokenText.substring(0, docCommentStart.length) + " "
    } else {
      currTokenText.substring(0, minimumCommentLength)
        .let { if (!it.endsWith(" ")) it.substring(0, lineCommentStart.length) else it }
    }

    val textToInsert = "\n" + indent + commentStart
    val caretOffset = trimWhitespaceInLineCommentOnEnter(editor.document, iterator, caretOffset)

    insertText(editor, caretOffset, textToInsert)
    return true
  }

  fun handleEnterFindLeftBracket(editor: Editor, caretOffset: Int): Boolean {
    val iterator = editor.highlighter.createIterator(caretOffset - 1)

    if (iterator.atEnd()) return false

    val document = editor.document
    val caretLine = document.getLineNumber(caretOffset)
    val lineStartOffset = document.getLineStartOffset(caretLine)

    if (!findUnmatchedBracketToLeft(iterator, caretOffset, lineStartOffset)) return false

    val leftBracketOffset = iterator.start
    val leftBracketType = iterator.tokenTypeSafe

    iterator.advance()
    while (iterator.tokenTypeSafe == FSharpTokenType.WHITESPACE) iterator.advance()

    val indent =
      // { new IInterface with {caret} }
      if (leftBracketType == FSharpTokenType.LBRACE && iterator.tokenTypeSafe == FSharpTokenType.NEW) {
        val braceOffset = leftBracketOffset - document.getLineStartOffset(caretLine)
        val defaultIndent = getIndentSize(editor.getPsiFile()!!)
        braceOffset + defaultIndent
      } else {
        iterator.start - lineStartOffset
      }
    insertNewLineAt(editor, indent, caretOffset, true)
    return true
  }

  private fun isFirstTokenOnLine(editor: Editor, offset: Int): Boolean {
    val iterator = editor.highlighter.createIterator(offset)
    iterator.retreat()
    while (!iterator.atEnd() && isIgnored(iterator.tokenType)) {
      iterator.retreat()
    }
    return iterator.atEnd() || iterator.tokenType == FSharpTokenType.NEW_LINE
  }

  private fun isLastTokenOnLine(editor: Editor, offset: Int): Boolean {
    val iterator = editor.highlighter.createIterator(offset)
    iterator.advance()
    while (!iterator.atEnd() && isIgnored(iterator.tokenType)) {
      iterator.advance()
    }
    return iterator.atEnd() || iterator.tokenType == FSharpTokenType.NEW_LINE
  }

  private fun getLineIndent(editor: Editor, line: Int): LineIndent? {
    val document = editor.document
    if (line >= document.lineCount) return null

    val startOffset = document.getLineStartOffset(line)
    val endOffset = document.getLineEndOffset(line)

    val iterator = editor.highlighter.createIterator(startOffset)
    if (iterator.atEnd()) return null

    var commentOffset: LineIndent? = null
    while (!iterator.atEnd() && iterator.start < endOffset && isIgnored(iterator.tokenType)) {
      if (commentOffset == null && iterator.tokenType.isComment) {
        commentOffset = LineIndent(Comments, iterator.start - startOffset)
      }
      iterator.advance()
    }

    val tokenType = iterator.tokenTypeSafe
    return if (tokenType == null || isIgnored(tokenType) || tokenType == FSharpTokenType.NEW_LINE) {
      commentOffset
    } else {
      LineIndent(Source, iterator.start - startOffset)
    }
  }

  private fun tryGetNestedIndentBelow(
    editor: Editor,
    line: Int,
    preferComment: Boolean,
    currentIndent: Int
  ): Pair<Int, LineIndent>? {

    tailrec fun tryFindIndent(
      editor: Editor,
      firstFoundCommentIndent: Pair<Int, LineIndent>?,
      line: Int
    ): Pair<Int, LineIndent>? {
      if (line >= editor.document.lineCount) return firstFoundCommentIndent

      val lineIndent = getLineIndent(editor, line) ?: return tryFindIndent(editor, firstFoundCommentIndent, line + 1)
      val indent = Pair(line, lineIndent)

      return when (lineIndent.type) {
        Source -> if (lineIndent.indent > currentIndent) indent else firstFoundCommentIndent
        Comments -> {
          if (preferComment) indent
          else if (firstFoundCommentIndent == null && lineIndent.indent > currentIndent)
            tryFindIndent(editor, indent, line + 1)
          else tryFindIndent(editor, firstFoundCommentIndent, line + 1)
        }
      }
    }

    return tryFindIndent(editor, null, line + 1)
  }

  private fun tryGetNestedIndentBelowLine(
    editor: Editor,
    line: Int
  ): Pair<Int, LineIndent>? {
    val lineIndent = getLineIndent(editor, line)
    return when (lineIndent?.type) {
      null,
      Comments -> null

      Source -> tryGetNestedIndentBelow(editor, line, false, lineIndent.indent)
    }
  }

  fun findRightBracket(iterator: HighlighterIterator): Boolean {
    return leftBracketsToAddIndent.contains(iterator.tokenType) &&
      FSharpBracketMatcher().findMatchingBracket(iterator.asTokenIterator()) != null
  }

  fun isSingleLineBrackets(editor: Editor, offset: Int): Boolean {
    val iterator = editor.highlighter.createIterator(offset)
    val document = editor.document
    val startLine = document.getLineNumber(iterator.start)
    return if (!findRightBracket(iterator)) {
      false
    } else {
      document.getLineNumber(iterator.start) == startLine
    }
  }

  fun getOffsetInLine(document: Document, line: Int, offset: Int) = offset - document.getLineStartOffset(line)

  fun handleEnterAddIndent(editor: Editor, caretOffset: Int): Boolean {
    var iterator = editor.highlighter.createIterator(caretOffset - 1)
    if (iterator.atEnd()) return false

    var encounteredNewLine = false
    while (!iterator.atEnd() && (isIgnored(iterator.tokenType) || iterator.tokenType == FSharpTokenType.NEW_LINE)) {
      if (iterator.tokenType == FSharpTokenType.NEW_LINE) encounteredNewLine = true
      iterator.retreat()
    }

    if (iterator.atEnd()) return false
    if (!indentTokens.contains(iterator.tokenType) && (encounteredNewLine || !allowKeepIndent.contains(iterator.tokenType)))
      return false

    val tokenStart = iterator.start
    val tokenType = iterator.tokenType
    val document = editor.document
    val line = document.getLineNumber(tokenStart)

    if (leftBracketsToAddIndent.contains(tokenType) &&
      !isSingleLineBrackets(editor, tokenStart) &&
      !isLastTokenOnLine(editor, tokenStart) &&
      isFirstTokenOnLine(editor, tokenStart)
    ) return false

    val caretLine = document.getLineNumber(caretOffset)

    val nestedIndent = tryGetNestedIndentBelowLine(editor, line)
    if (nestedIndent != null) {
      val (belowLine, lineIndent) = nestedIndent
      if (belowLine == caretLine) return false

      insertNewLineAt(editor, lineIndent.indent, caretOffset, true)
      return true
    }

    iterator = editor.highlighter.createIterator(caretOffset)

    if (!iterator.atEnd() &&
      isFirstTokenOnLine(editor, iterator.start) &&
      !isLastTokenOnLine(editor, iterator.start)
    ) return false

    val indentSize = run {
      val lineIndent = getLineIndent(editor, caretLine)
      if (lineIndent != null && lineIndent.type == Comments)
        return@run lineIndent.indent

      iterator = editor.highlighter.createIterator(caretOffset - 1)
      val indent = getIndentSettings(editor).indentSize

      if (tokenType == FSharpTokenType.EQUALS && !encounteredNewLine && isFirstTokenOnLine(editor, iterator.start)
      ) {
        getOffsetInLine(document, line, tokenStart)
      } else if (indentFromToken.contains(tokenType)) {
        indent + getOffsetInLine(document, line, tokenStart)
      } else {
        val prevIndentSize = run {
          val continuedLine = getContinuedIndentLine(editor, tokenStart, true)
          getLineWhitespaceIndent(editor, continuedLine)
        }
        prevIndentSize + indent
      }
    }

    insertNewLineAt(editor, indentSize, caretOffset, true)
    return true
  }

  fun handleEnterAddBiggerIndentFromBelow(editor: Editor, caretOffset: Int): Boolean {
    val document = editor.document
    val caretCoords = document.offsetToDocCoordinates(caretOffset)
    val caretLine = caretCoords.line

    if (caretLine + 1 >= document.lineCount) return false

    val lineStartOffset = document.getLineStartOffset(caretLine)
    val iterator = editor.highlighter.createIterator(lineStartOffset)
    var seenComment = false

    if (iterator.atEnd()) return false

    while (!iterator.atEnd() && (
        iterator.tokenType == FSharpTokenType.WHITESPACE ||
          (iterator.tokenType != null && iterator.tokenType.isComment && iterator.start < caretOffset)
        )
    ) {
      seenComment = seenComment || iterator.tokenType.isComment
      iterator.advance()
    }

    if (iterator.tokenTypeSafe != FSharpTokenType.NEW_LINE) return false

    val currentIndent = if (seenComment) 0 else caretCoords.column

    val nestedIndentBelow = tryGetNestedIndentBelow(editor, caretLine, seenComment, currentIndent)
    if (nestedIndentBelow == null) return false

    val (_, lineIndent) = nestedIndentBelow
    insertNewLineAt(editor, lineIndent.indent, caretOffset, false)
    return true
  }

  private fun handleEnter(
    editor: Editor,
    caretOffset: Int,
  ) {
    if (handleEnterInTripleQuotedString(editor, caretOffset)) return
    if (handleEnterInLineComment(editor, caretOffset)) return
    if (handleEnterAddIndent(editor, caretOffset)) return
    if (handleEnterFindLeftBracket(editor, caretOffset)) return
    if (handleEnterAddBiggerIndentFromBelow(editor, caretOffset)) return

    val iterator = editor.highlighter.createIterator(caretOffset)

    val trimSpacesAfterCaret =
      if (iterator.atEnd()) false
      else {
        while (iterator.tokenTypeSafe == FSharpTokenType.WHITESPACE) iterator.advance()
        shouldTrimSpacesBeforeToken(iterator.tokenTypeSafe)
      }

    doDumpIndent(editor, caretOffset, trimSpacesAfterCaret)
  }

  override fun preprocessEnter(
    file: PsiFile,
    editor: Editor,
    caretOffset: Ref<Int>,
    caretAdvance: Ref<Int>,
    dataContext: DataContext,
    originalHandler: EditorActionHandler?
  ): Result {
    if (file.language != FSharpLanguage) return Result.Continue
    if (!isPatchEngineEnabled) return Result.Stop
    runCatching {
      handleEnter(editor, caretOffset.get())
      return Result.Stop
    }.getOrElse { exception ->
      val trace = exception.stackTrace.joinToString(System.lineSeparator()) { it.toString() }
      logger.error("Couldn't execute enter handler for F#: ${exception.message}${System.lineSeparator()}$trace")
      return Result.Continue
    }
  }
}


internal data class LineIndent(val type: Type, val indent: Int) {
  enum class Type {
    Source, Comments
  }
}
