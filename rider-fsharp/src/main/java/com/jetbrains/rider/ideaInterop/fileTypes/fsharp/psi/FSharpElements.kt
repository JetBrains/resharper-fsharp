package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi

import com.intellij.psi.PsiElement
import com.intellij.psi.PsiFile
import com.intellij.psi.PsiLanguageInjectionHost

interface FSharpElement : PsiElement

interface FSharpFile : FSharpElement, PsiFile
interface FSharpScript : FSharpElement, PsiFile

interface FSharpReparseableElement : FSharpElement

interface FSharpIndentationBlock : FSharpReparseableElement

interface FSharpLine : FSharpReparseableElement

interface FSharpStringLiteralExpression : FSharpElement, PsiLanguageInjectionHost {
    val literalType: FSharpStringLiteralType
}