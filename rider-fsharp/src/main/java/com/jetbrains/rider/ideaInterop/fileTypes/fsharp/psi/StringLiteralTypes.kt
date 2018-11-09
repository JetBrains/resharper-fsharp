package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.psi

enum class FSharpStringLiteralType {
    /** '{character representation}' */
    Character,
    /** "{string representation}" */
    RegularString,
    /** @"{string representation}" */
    VerbatimString,
    /** """{string representation}""" */
    TripleQuotedString,
    /** "{string representation}"B */
    ByteArray
}