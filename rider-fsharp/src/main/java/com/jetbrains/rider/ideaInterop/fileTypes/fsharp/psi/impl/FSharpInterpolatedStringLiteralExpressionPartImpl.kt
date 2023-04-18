package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl

import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpElementType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpInterpolatedStringLiteralExpressionPart

class FSharpInterpolatedStringLiteralExpressionPartImpl(type: FSharpElementType) : FSharpPsiElementBase(type),
    FSharpInterpolatedStringLiteralExpressionPart
