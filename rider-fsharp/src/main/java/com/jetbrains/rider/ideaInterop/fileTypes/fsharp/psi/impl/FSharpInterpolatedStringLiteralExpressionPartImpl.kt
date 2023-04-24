package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl

import com.intellij.psi.util.elementType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpElementType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpInterpolatedStringLiteralExpressionPart

class FSharpInterpolatedStringLiteralExpressionPartImpl(type: FSharpElementType) : FSharpPsiElementBase(type),
  FSharpInterpolatedStringLiteralExpressionPart {
  override val tokenType: FSharpTokenType
    get() = firstChild?.elementType as FSharpTokenType
}
