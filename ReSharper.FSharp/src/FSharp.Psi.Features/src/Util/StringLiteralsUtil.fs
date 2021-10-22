[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.StringLiteralsUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Psi.Parsing

[<Extension>]
type FSharpLiteralType =
    /// '{char}'
    | Character
    /// "{string}"
    | RegularString
    /// @"{string}"
    | VerbatimString
    /// @"{string}"B
    | VerbatimByteArray
    /// """{string}"""
    | TripleQuoteString
    /// "{string}"B
    | ByteArray
    /// $"{string}"
    | InterpolatedString
    /// $"{string}{
    | InterpolatedStringStart
    /// }{string}{
    | InterpolatedStringMiddle
    /// }{string}"
    | InterpolatedStringEnd
    /// $@"{string}" @$"{string}"
    | VerbatimInterpolatedString
    /// $@"{string}{ @$"{string}{
    | VerbatimInterpolatedStringStart
    /// }{string}{
    | VerbatimInterpolatedStringMiddle
    /// }{string}"
    | VerbatimInterpolatedStringEnd
    /// $"""{string}"""
    | TripleQuoteInterpolatedString
    /// $"""{string}{
    | TripleQuoteInterpolatedStringStart
    /// }{string}{
    | TripleQuoteInterpolatedStringMiddle
    /// }{string}"""
    | TripleQuoteInterpolatedStringEnd

    [<Extension>]
    static member GetLiteralType(tokenType: TokenNodeType) =
        if tokenType == FSharpTokenType.CHARACTER_LITERAL then FSharpLiteralType.Character else
        if tokenType == FSharpTokenType.STRING then FSharpLiteralType.RegularString else
        if tokenType == FSharpTokenType.VERBATIM_STRING then FSharpLiteralType.VerbatimString else
        if tokenType == FSharpTokenType.VERBATIM_BYTEARRAY then FSharpLiteralType.VerbatimByteArray else
        if tokenType == FSharpTokenType.TRIPLE_QUOTED_STRING then FSharpLiteralType.TripleQuoteString else
        if tokenType == FSharpTokenType.BYTEARRAY then FSharpLiteralType.ByteArray else

        if tokenType == FSharpTokenType.REGULAR_INTERPOLATED_STRING then FSharpLiteralType.InterpolatedString else
        if tokenType == FSharpTokenType.REGULAR_INTERPOLATED_STRING_START then FSharpLiteralType.InterpolatedStringStart else
        if tokenType == FSharpTokenType.REGULAR_INTERPOLATED_STRING_MIDDLE then FSharpLiteralType.InterpolatedStringMiddle else
        if tokenType == FSharpTokenType.REGULAR_INTERPOLATED_STRING_END then FSharpLiteralType.InterpolatedStringEnd else

        if tokenType == FSharpTokenType.VERBATIM_INTERPOLATED_STRING then FSharpLiteralType.VerbatimInterpolatedString else
        if tokenType == FSharpTokenType.VERBATIM_INTERPOLATED_STRING_START then FSharpLiteralType.VerbatimInterpolatedStringStart else
        if tokenType == FSharpTokenType.VERBATIM_INTERPOLATED_STRING_MIDDLE then FSharpLiteralType.VerbatimInterpolatedStringMiddle else
        if tokenType == FSharpTokenType.VERBATIM_INTERPOLATED_STRING_END then FSharpLiteralType.VerbatimInterpolatedStringEnd else

        if tokenType == FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING then FSharpLiteralType.TripleQuoteInterpolatedString else
        if tokenType == FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_START then FSharpLiteralType.TripleQuoteInterpolatedStringStart else
        if tokenType == FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_MIDDLE then FSharpLiteralType.TripleQuoteInterpolatedStringMiddle else
        if tokenType == FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_END then FSharpLiteralType.TripleQuoteInterpolatedStringEnd else

        if tokenType == FSharpTokenType.UNFINISHED_STRING then FSharpLiteralType.RegularString else
        if tokenType == FSharpTokenType.UNFINISHED_VERBATIM_STRING then FSharpLiteralType.VerbatimString else
        if tokenType == FSharpTokenType.UNFINISHED_TRIPLE_QUOTED_STRING then FSharpLiteralType.TripleQuoteString else
        if tokenType == FSharpTokenType.UNFINISHED_REGULAR_INTERPOLATED_STRING then FSharpLiteralType.InterpolatedString else
        if tokenType == FSharpTokenType.UNFINISHED_VERBATIM_INTERPOLATED_STRING then FSharpLiteralType.VerbatimInterpolatedString else
        if tokenType == FSharpTokenType.UNFINISHED_TRIPLE_QUOTE_INTERPOLATED_STRING then FSharpLiteralType.TripleQuoteInterpolatedString else

        failwithf $"Token {tokenType} is not a string literal"


let private assertStringTokenType (tokenType: TokenNodeType) =
    if not FSharpTokenType.Strings.[tokenType] then
        failwithf $"Got token type: {tokenType}"

let getStringStartingQuotesLength (tokenType: TokenNodeType) =
    assertStringTokenType tokenType

    match tokenType.GetLiteralType() with
    | Character
    | InterpolatedStringMiddle
    | InterpolatedStringEnd
    | VerbatimInterpolatedStringMiddle
    | VerbatimInterpolatedStringEnd
    | TripleQuoteInterpolatedStringMiddle
    | TripleQuoteInterpolatedStringEnd
    | RegularString -> 1
    | VerbatimString
    | InterpolatedString
    | InterpolatedStringStart
    | ByteArray
    | VerbatimByteArray -> 2
    | TripleQuoteString
    | VerbatimInterpolatedString
    | VerbatimInterpolatedStringStart -> 3
    | TripleQuoteInterpolatedString
    | TripleQuoteInterpolatedStringStart -> 4

let getStringEndingQuote tokenType =
    assertStringTokenType tokenType
    if tokenType == FSharpTokenType.CHARACTER_LITERAL then '\'' else '\"'

let getStringEndingQuotesLength (tokenType: TokenNodeType) =
    assertStringTokenType tokenType

    match tokenType.GetLiteralType() with
    | Character
    | RegularString
    | InterpolatedString
    | InterpolatedStringStart
    | InterpolatedStringMiddle
    | InterpolatedStringEnd
    | VerbatimString
    | VerbatimInterpolatedString
    | VerbatimInterpolatedStringStart
    | VerbatimInterpolatedStringMiddle
    | VerbatimInterpolatedStringEnd
    | TripleQuoteInterpolatedStringStart
    | TripleQuoteInterpolatedStringMiddle -> 1
    | TripleQuoteString
    | TripleQuoteInterpolatedString
    | TripleQuoteInterpolatedStringEnd -> 3
    | ByteArray
    | VerbatimByteArray -> 2

let emptyString = "\"\""
let emptyChar = "''"

let getCorrespondingQuotesPair char =
    match char with
    | '"' -> emptyString
    | '\'' -> emptyChar
    | _ -> failwithf "Got char: %O" char

let isInterpolatedStringStartToken (tokenType: TokenNodeType) =
    tokenType == FSharpTokenType.REGULAR_INTERPOLATED_STRING_START ||
    tokenType == FSharpTokenType.VERBATIM_INTERPOLATED_STRING_START ||
    tokenType == FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_START

let isInterpolatedStringMiddleToken (tokenType: TokenNodeType) =
    tokenType == FSharpTokenType.REGULAR_INTERPOLATED_STRING_MIDDLE ||
    tokenType == FSharpTokenType.VERBATIM_INTERPOLATED_STRING_MIDDLE ||
    tokenType == FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_MIDDLE

let isInterpolatedStringEndToken (tokenType: TokenNodeType) =
    tokenType == FSharpTokenType.REGULAR_INTERPOLATED_STRING_END ||
    tokenType == FSharpTokenType.VERBATIM_INTERPOLATED_STRING_END ||
    tokenType == FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING_END

let isInterpolatedStringPartToken (tokenType: TokenNodeType) =
    isInterpolatedStringStartToken tokenType ||
    isInterpolatedStringMiddleToken tokenType ||
    isInterpolatedStringEndToken tokenType