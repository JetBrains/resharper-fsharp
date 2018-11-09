package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl

import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.*

object FSharpElementTypes {
    val FILE = FSharpFileElementType()
    val LINE = FSharpLineType()
    val INDENTATION_BLOCK = FSharpIndentationBlockType()
    val STRING_LITERAL_EXPRESSION = createCompositeElementType("STRING_LITERAL_EXPRESSION", ::FSharpStringLiteralExpressionImpl)
}