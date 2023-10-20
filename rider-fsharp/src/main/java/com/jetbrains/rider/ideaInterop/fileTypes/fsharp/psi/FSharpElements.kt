package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi

import com.intellij.psi.PsiElement
import com.intellij.psi.PsiFile
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenNodeType
import com.jetbrains.rider.languages.fileTypes.clr.psi.ClrLanguageInterpolatedStringLiteralExpression
import com.jetbrains.rider.languages.fileTypes.clr.psi.ClrLanguageInterpolatedStringLiteralExpressionPart
import com.jetbrains.rider.languages.fileTypes.clr.psi.ClrLanguageStringLiteralExpression

interface FSharpElement : PsiElement

interface FSharpFile : FSharpElement, PsiFile

interface FSharpExpression : FSharpElement

interface FSharpStringLiteralExpression : FSharpElement, ClrLanguageStringLiteralExpression {
  val literalType: FSharpStringLiteralType
}

interface FSharpInterpolatedStringLiteralExpressionPart : FSharpElement,
  ClrLanguageInterpolatedStringLiteralExpressionPart {
  val tokenType: FSharpTokenNodeType
}

interface FSharpInterpolatedStringLiteralExpression : FSharpStringLiteralExpression,
  ClrLanguageInterpolatedStringLiteralExpression
