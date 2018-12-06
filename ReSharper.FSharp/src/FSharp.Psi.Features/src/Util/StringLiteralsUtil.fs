[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.StringLiteralsUtil

open JetBrains.ReSharper.Plugins.FSharp.Common.Util
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

    /// """{string}"""
    | TripleQuoteString

    /// "{string}"B
    | ByteArray

    [<Extension>]
    static member GetLiteralType(literalTokenType: TokenNodeType) =
        if literalTokenType == FSharpTokenType.CHARACTER_LITERAL then FSharpLiteralType.Character else
        if literalTokenType == FSharpTokenType.STRING then FSharpLiteralType.RegularString else
        if literalTokenType == FSharpTokenType.VERBATIM_STRING then FSharpLiteralType.VerbatimString else
        if literalTokenType == FSharpTokenType.TRIPLE_QUOTED_STRING then FSharpLiteralType.TripleQuoteString else
        if literalTokenType == FSharpTokenType.BYTEARRAY then FSharpLiteralType.ByteArray else

        failwithf "Token %O is not a string literal" literalTokenType


let private assertStringTokenType (tokenType: TokenNodeType) =
    if isNull tokenType || not tokenType.IsStringLiteral then failwithf "Got token type: %O" tokenType


let getStringEndingQuote tokenType =
    assertStringTokenType tokenType
    if tokenType == FSharpTokenType.CHARACTER_LITERAL then '\'' else '\"'

let getStringEndingQuotesOffset (tokenType: TokenNodeType) =
    assertStringTokenType tokenType
    match tokenType.GetLiteralType() with
    | Character
    | RegularString
    | VerbatimString -> 1
    | TripleQuoteString -> 3
    | ByteArray -> 2


let emptyString = "\"\""
let emptyChar = "''"

let getCorresponingQuotesPair char =
    match char with
    | '"' -> emptyString
    | '\'' -> emptyChar
    | _ -> failwithf "Got char: %O" char
