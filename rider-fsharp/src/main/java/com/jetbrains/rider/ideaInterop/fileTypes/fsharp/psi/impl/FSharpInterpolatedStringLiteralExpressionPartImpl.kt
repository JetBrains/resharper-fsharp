package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl

import com.intellij.psi.util.elementType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenNodeType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpElementType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpInterpolatedStringLiteralExpressionPart

class FSharpInterpolatedStringLiteralExpressionPartImpl(type: FSharpElementType) : FSharpPsiElementBase(type),
  FSharpInterpolatedStringLiteralExpressionPart {
  override val tokenType: FSharpTokenNodeType
    get() = firstChild?.elementType as FSharpTokenNodeType
}
