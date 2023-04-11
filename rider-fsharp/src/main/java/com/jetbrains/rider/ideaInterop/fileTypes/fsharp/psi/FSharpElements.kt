package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi

import com.intellij.psi.PsiElement
import com.intellij.psi.PsiFile

interface FSharpElement : PsiElement

interface FSharpFile : FSharpElement, PsiFile

interface FSharpExpression : FSharpElement

interface FSharpStringLiteralExpression : FSharpElement

interface FSharpInterpolatedStringLiteralExpressionPart : FSharpElement

interface FSharpInterpolatedStringLiteralExpression : FSharpStringLiteralExpression
