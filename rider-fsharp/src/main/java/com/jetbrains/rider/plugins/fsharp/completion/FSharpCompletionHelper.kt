package com.jetbrains.rider.plugins.fsharp.completion

import com.intellij.openapi.editor.Document
import com.intellij.patterns.ElementPattern
import com.intellij.patterns.PatternCondition
import com.intellij.psi.PsiElement
import com.intellij.util.ProcessingContext
import com.jetbrains.rider.completion.CustomCharPattern
import com.jetbrains.rider.completion.ICompletionHelper
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpFile

class FSharpCompletionHelper : ICompletionHelper {
  private fun completionInsideReferenceDirective(psiElement: PsiElement, offset: Int): Boolean =
    psiElement is FSharpFile && insideReferenceDirective(psiElement, offset)

  private fun isValid(character: Char) =
    Character.isJavaIdentifierStart(character) || character == '.' || Character.isDigit(character)

  override fun getIdentifierPart(documentOffset: Int,
                                 document: Document,
                                 element: PsiElement,
                                 completionOffset: Int): ElementPattern<Char>? {
    return if (completionInsideReferenceDirective(element, documentOffset))
      CustomCharPattern.customCharacter().with(object : PatternCondition<Char>("fsharpIdentifierPart") {
        override fun accepts(character: Char, context: ProcessingContext) = isValid(character)
      })
    else null
  }

  override fun getIdentifierStart(documentOffset: Int,
                                  document: Document,
                                  element: PsiElement,
                                  completionOffset: Int): ElementPattern<Char>? {
    return if (completionInsideReferenceDirective(element, documentOffset))
      CustomCharPattern.customCharacter().with(object : PatternCondition<Char>("fsharpIdentifierStart") {
        override fun accepts(character: Char, context: ProcessingContext) = isValid(character)
      })
    else null
  }
}
