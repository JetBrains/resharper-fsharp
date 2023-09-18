package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi

import com.jetbrains.rider.languages.fileTypes.clr.psi.ClrLanguageWebReferenceContributorBase


class FSharpWebReferenceContributor : ClrLanguageWebReferenceContributorBase<FSharpStringLiteralExpression>() {
  override fun getAcceptableStringExpressionType() = FSharpStringLiteralExpression::class.java
}
