package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi

import com.jetbrains.rider.ideaInterop.fileTypes.common.psi.ClrLanguageWebReferenceContributorBase

class FSharpWebReferenceContributor : ClrLanguageWebReferenceContributorBase<FSharpStringLiteralExpression>() {
  override fun getAcceptableStringExpressionType() = FSharpStringLiteralExpression::class.java
}
