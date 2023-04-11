package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl

import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpElementType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpStringLiteralExpression

class FSharpStringLiteralExpressionImpl(type: FSharpElementType) : FSharpPsiElementBase(type),
  FSharpStringLiteralExpression
