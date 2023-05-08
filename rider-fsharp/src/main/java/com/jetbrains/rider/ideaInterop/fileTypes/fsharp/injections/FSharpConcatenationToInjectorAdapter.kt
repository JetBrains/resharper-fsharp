package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.injections

import com.intellij.openapi.project.Project
import com.intellij.psi.PsiElement
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpExpression
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpStringLiteralExpression
import com.jetbrains.rider.plugins.appender.database.common.ClrLanguageConcatenationToInjectorAdapter

class FSharpConcatenationToInjectorAdapter(project: Project) :
  ClrLanguageConcatenationToInjectorAdapter(project, FSharpTokenType.PLUS) {
  override fun elementsToInjectIn() = arrayListOf(FSharpStringLiteralExpression::class.java)
  override fun isConcatenationExpression(element: PsiElement) = element is FSharpExpression
}
