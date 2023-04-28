package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl

import com.intellij.psi.PsiLanguageInjectionHost
import com.intellij.psi.PsiReference
import com.intellij.psi.impl.source.resolve.reference.ReferenceProvidersRegistry
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpElementType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpStringLiteralExpression

abstract class FSharpStringLiteralExpressionBase(type: FSharpElementType) : FSharpPsiElementBase(type),
  FSharpStringLiteralExpression,
  PsiLanguageInjectionHost {
  override fun getReferences(): Array<PsiReference> = ReferenceProvidersRegistry.getReferencesFromProviders(this)
}
