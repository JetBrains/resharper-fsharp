package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi

import com.intellij.openapi.util.TextRange
import com.intellij.psi.ElementManipulator

class FSharpStringElementManipulator : ElementManipulator<FSharpStringLiteralExpression> {
  override fun handleContentChange(
    element: FSharpStringLiteralExpression,
    newContent: String
  ): FSharpStringLiteralExpression = handleContentChange(element, getRangeInElement(element), newContent)

  override fun handleContentChange(
    element: FSharpStringLiteralExpression,
    range: TextRange,
    newContent: String
  ): FSharpStringLiteralExpression = element

  override fun getRangeInElement(element: FSharpStringLiteralExpression) = element.getRelativeRangeTrimQuotes()
}
