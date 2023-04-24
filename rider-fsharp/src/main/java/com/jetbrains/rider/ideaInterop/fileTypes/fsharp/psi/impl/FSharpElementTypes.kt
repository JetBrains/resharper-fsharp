package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.impl

import com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi.*

object FSharpElementTypes {
  val FILE = FSharpFileElementType()

  val DUMMY_EXPRESSION = createCompositeElementType(
    "DUMMY_EXPRESSION", ::FSharpExpressionImpl
  )

  val STRING_LITERAL_EXPRESSION = createCompositeElementType(
    "STRING_LITERAL_EXPRESSION", ::FSharpFSharpStringLiteralExpressionImpl
  )

  val INTERPOLATED_STRING_LITERAL_EXPRESSION_PART = createCompositeElementType(
    "INTERPOLATED_STRING_LITERAL_EXPRESSION_PART", ::FSharpInterpolatedStringLiteralExpressionPartImpl
  )

  val INTERPOLATED_STRING_LITERAL_EXPRESSION = createCompositeElementType(
    "INTERPOLATED_STRING_LITERAL_EXPRESSION", ::FSharpInterpolatedStringLiteralExpressionImpl
  )
}
