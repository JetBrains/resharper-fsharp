package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl

import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpElementType
import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.FSharpExpression

open class FSharpExpressionImpl(type: FSharpElementType) : FSharpPsiElementBase(type), FSharpExpression
