package com.jetbrains.rider.plugins.fsharp.editorActions

import com.intellij.openapi.editor.highlighter.HighlighterIterator
import com.intellij.psi.tree.IElementType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType
import java.util.*
import kotlin.math.abs

val HighlighterIterator.tokenText: String
    get() = this.document.charsSequence.substring(this.start, this.end)

val HighlighterIterator.tokenTypeSafe: IElementType?
  get() = if (this.atEnd()) null else this.tokenTypeSafe

// TODO: Move to the platform
abstract class BracketMatcher(
  private val myBrackets: Array<Pair<IElementType, IElementType>>,
) {
  private val myDirection: MutableMap<IElementType, Int> = mutableMapOf()
  private val myStack: Stack<IElementType> = Stack()

  init {
    for ((first, second) in myBrackets) {
      myDirection[first] = +1
      myDirection[second] = -1
    }
  }

  fun getDirection(type: IElementType?): Int {
    return myDirection[type] ?: 0
  }

  /**
   * Try to find the corresponding matching bracket for the given bracket.
   *
   * @param iterator Caching lexer positioned at the source bracket.
   * @return The position of the matching bracket or null if not found.
   */
  fun findMatchingBracket(iterator: HighlighterIterator): Int? {
    myStack.clear()
    var tokenType = iterator.tokenTypeSafe

    val delta = getDirection(tokenType)
    if (delta == 0) {
      return null
    }

    while (true) {
      if (!proceedStack(tokenType))
        return null

      if (isStackEmpty())
        return iterator.start

      repeat(abs(delta)) { if (delta > 0) iterator.advance() else iterator.retreat() }

      tokenType = iterator.tokenTypeSafe ?: return null
    }
  }

  fun isStackEmpty(): Boolean {
    return myStack.isEmpty()
  }

  fun proceedStack(tokenType: IElementType?, failIfRightOnEmpty: Boolean = false): Boolean {
    // Check that token is a bracket
    val direction = getDirection(tokenType)
    if (direction == 0) return true

    if (isStackEmpty()) {
      myStack.push(tokenType)
      return !failIfRightOnEmpty || direction != -1
    }

    // If the bracket direction matches the last bracket direction
    val prevToken = myStack.peek()
    if (getDirection(prevToken) == direction) {
      myStack.push(tokenType)
      return true
    }

    // Try to collapse brackets
    for ((first, second) in myBrackets) {
      if (tokenType == first) return tryCollapse(second, prevToken)
      if (tokenType == second) return tryCollapse(first, prevToken)
    }

    error("Should never get here!")
  }

  private fun tryCollapse(tokenType: IElementType, peekedTokenType: IElementType): Boolean {
    return if (tokenType == peekedTokenType) {
      myStack.pop()
      true
    } else {
      false
    }
  }
}

class FSharpBracketMatcher : BracketMatcher(
  arrayOf(
    Pair(FSharpTokenType.LPAREN, FSharpTokenType.RPAREN),
    Pair(FSharpTokenType.LBRACK, FSharpTokenType.RBRACK),
    Pair(FSharpTokenType.LBRACE, FSharpTokenType.RBRACE),
    Pair(FSharpTokenType.LBRACK_BAR, FSharpTokenType.BAR_RBRACK),
    Pair(FSharpTokenType.LBRACE_BAR, FSharpTokenType.BAR_RBRACE),
    Pair(FSharpTokenType.LBRACK_LESS, FSharpTokenType.GREATER_RBRACK),
    Pair(FSharpTokenType.LQUOTE_TYPED, FSharpTokenType.RQUOTE_TYPED),
    Pair(FSharpTokenType.LQUOTE_UNTYPED, FSharpTokenType.RQUOTE_UNTYPED)
  )
)
