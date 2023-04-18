package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl

import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpElementType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpInterpolatedStringLiteralExpression

class FSharpInterpolatedStringLiteralExpressionImpl(type: FSharpElementType) : FSharpPsiElementBase(type),
    FSharpInterpolatedStringLiteralExpression
