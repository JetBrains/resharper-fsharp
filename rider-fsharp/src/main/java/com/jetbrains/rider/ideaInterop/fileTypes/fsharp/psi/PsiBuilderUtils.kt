package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi

import com.intellij.lang.PsiBuilder
import com.intellij.psi.tree.IElementType

/** Repeats given action until lexer advances and action returns true */
inline fun PsiBuilder.whileMakingProgress(action: PsiBuilder.() -> Boolean) {
  var position = currentOffset
  while (action() && position != currentOffset) {
    position = currentOffset
  }
}

/**
 * Parse node of given type if builder was advanced.
 * Returns true if node was parsed (and builder was advanced)
 */
inline fun PsiBuilder.parse(nodeType: IElementType, action: PsiBuilder.() -> Unit): Boolean {
  val position = rawTokenIndex()
  val mark = mark()
  action()
  return if (position == rawTokenIndex()) {
    mark.drop()
    false
  } else {
    mark.done(nodeType)
    true
  }
}

inline fun PsiBuilder.parseEvenEmpty(nodeType: IElementType, action: PsiBuilder.() -> Unit): Boolean {
  val mark = mark()
  action()
  mark.done(nodeType)
  return true
}

/**
 * Parse node of returned type, or just scan, if action returns null.
 * Returns true if builder was advanced
 * */
inline fun PsiBuilder.parse(action: () -> IElementType?): Boolean {
  val mark = mark()
  val positionBefore = rawTokenIndex()

  val elementType = action()
  if (elementType == null) {
    mark.drop()
    return positionBefore != rawTokenIndex()
  }
  return if (positionBefore == rawTokenIndex()) {
    mark.drop()
    false
  } else {
    mark.done(elementType)
    true
  }
}

/** Scans lexer. Allows to rollback, if action returns true. Returns true if lexer was advanced */
inline fun PsiBuilder.scanOrRollback(action: () -> Boolean): Boolean {
  val mark = mark()
  val positionBefore = rawTokenIndex()

  if (!action()) {
    mark.rollbackTo()
    return false
  }
  mark.drop()
  return positionBefore != rawTokenIndex()
}
