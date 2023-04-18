package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi

enum class FSharpStringLiteralType {
  /** "{string representation}" */
  RegularString,

  /** @"{string representation}" */
  VerbatimString,

  /** """{string representation}""" */
  TripleQuoteString,

  /** $"{string representation}" */
  RegularInterpolatedString,

  /** $@"{string representation}" */
  VerbatimInterpolatedString,

  /** $"""{string representation}""" */
  TripleQuoteInterpolatedString,

  /** "{string representation}"B */
  ByteArray
}
