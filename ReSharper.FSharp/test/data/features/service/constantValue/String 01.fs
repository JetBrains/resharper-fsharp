module Module

let regular = "abc"
let escapes = "a\tb\n\r\b\a\f\v\"\'\\"
let codes = "\065\x41\u0041\U00000041"
let notEscapes = "\0\12\q"
let continuation = "a\
    b"
let continuationBlankLine = "a\

    b"
let verbatim = @"a""b\n"
let triple = """a"b\n"""
let paren = ("abc")
let interpolated = $"abc"
let byteArray = "abc"B
let number = 42
let character = 'a'
let empty = ""
let escapeAtEnd = "a\n"
let backslashAtEnd = "a\\"
let verbatimEmpty = @""
let verbatimQuoteAtEnd = @"a"""
let tripleEmpty = """"""
let trigraphAbove255 = "\999"
let trigraphNonAsciiDigits = "\١٢٣"

[<Literal>]
let Literal = "literal"
let literalReference = Literal
let valueReference = regular
let concatenated = "a" + "b"
let concatenatedChain = "a" + ("b" + "c") + "d"
let concatenatedLiteral = Literal + "b"
let concatenatedValue = regular + "b"
let subtracted = "a" - "b"
