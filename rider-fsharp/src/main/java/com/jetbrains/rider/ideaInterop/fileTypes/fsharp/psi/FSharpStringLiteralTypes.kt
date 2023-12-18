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

  /** $$""" {string} {{interpolation hole}} """*/
  RawInterpolatedString,

  /** "{string representation}"B */
  ByteArray
}

val FSharpStringLiteralType.isRegular: Boolean
  get() =
    this == FSharpStringLiteralType.RegularString ||
      this == FSharpStringLiteralType.RegularInterpolatedString

val FSharpStringLiteralType.isInterpolated: Boolean
  get() = when (this) {
    FSharpStringLiteralType.RegularInterpolatedString,
    FSharpStringLiteralType.VerbatimInterpolatedString,
    FSharpStringLiteralType.TripleQuoteInterpolatedString,
    FSharpStringLiteralType.RawInterpolatedString -> true

    else -> false
  }

val FSharpStringLiteralType.isVerbatim: Boolean
  get() =
    this == FSharpStringLiteralType.VerbatimString ||
      this == FSharpStringLiteralType.VerbatimInterpolatedString
