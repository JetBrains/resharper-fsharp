[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.StringLiteralsUtil

open System.Runtime.CompilerServices
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
