package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi

import com.intellij.psi.PsiElement
import com.intellij.psi.PsiFile
import com.jetbrains.rider.ideaInterop.fileTypes.common.psi.ClrLanguageStringLiteralExpression
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenNodeType

interface FSharpElement : PsiElement

interface FSharpFile : FSharpElement, PsiFile

interface FSharpExpression : FSharpElement

interface FSharpStringLiteralExpression : FSharpElement, ClrLanguageStringLiteralExpression {
  val literalType: FSharpStringLiteralType
}

interface FSharpInterpolatedStringLiteralExpressionPart : FSharpElement {
  val tokenType: FSharpTokenNodeType
}

interface FSharpInterpolatedStringLiteralExpression : FSharpStringLiteralExpression
