package com.jetbrains.rider.plugins.fsharp.services.completion

import com.intellij.codeInsight.completion.CompletionType
import com.intellij.openapi.editor.Editor
import com.intellij.psi.PsiFile
import com.jetbrains.rider.completion.CompletionSessionHeuristics
import com.jetbrains.rider.completion.CompletionSessionStrategy

class FSharpCompletionStrategy : CompletionSessionStrategy {
  override fun shouldForbidCompletion(editor: Editor, type: CompletionType) = editor.selectionModel.hasSelection()
  override fun shouldRescheduleCompletion(prefix: String, psiFile: PsiFile, char: Char?, offset: Int) =
    CompletionSessionHeuristics.getInstance(psiFile.project).shouldRescheduleDefaultStrategy(char)
}