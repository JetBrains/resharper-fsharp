[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.StringLiteralsUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.Tree

let private assertStringTokenType (tokenType: TokenNodeType) =
    if not FSharpTokenType.Strings[tokenType] then
        failwithf $"Got token type: {tokenType}"

let private getBorderLengths (token: ITokenNode) =
    assertStringTokenType (token.GetTokenType())

    let stringToken = token :?> FSharpString
    stringToken.GetBorderLengths()

let getStringStartingQuotesLength (token: ITokenNode) =
    let struct (startLength, _) = getBorderLengths token
    startLength

let getStringEndingQuote tokenType =
    assertStringTokenType tokenType
    if tokenType == FSharpTokenType.CHARACTER_LITERAL then '\'' else '\"'

let getDefaultStringEndingQuotesLength (tokenType: TokenNodeType) =
    let struct (_, endLength) = tokenType.GetBorderLengths(FSharpLiteralTypeExtensions.DefaultRawStringDollarCount)
    endLength

let getStringEndingQuotesLength (token: ITokenNode) =
    let struct (_, endLength) = getBorderLengths token
    endLength


let getStringEndingQuotes (token: ITokenNode) =
    let tokenType = token.GetTokenType()
    assertStringTokenType tokenType
    let endingQuotesLength = getStringEndingQuotesLength token
    let text = token.GetText()
    text.Substring(text.Length - endingQuotesLength, endingQuotesLength)

let isUnfinishedString (token: ITokenNode) =
    let tokenType = token.GetTokenType()
    assertStringTokenType tokenType
    if FSharpTokenType.UnfinishedStrings[tokenType] then true else

    let startingQuotesLength = getStringStartingQuotesLength token
    let endingQuotesLength = getStringEndingQuotesLength token

    let tokenText = token.GetText()
    tokenText.Length < startingQuotesLength + endingQuotesLength ||
    getStringEndingQuotes token |> Seq.exists (fun x -> x <> '"')

let getStringContent (token: ITokenNode) =
    let tokenType = token.GetTokenType()
    assertStringTokenType tokenType

    let startBorderLength = getStringStartingQuotesLength token
    let endBorderLength = getStringEndingQuotesLength token

    let tokenText = token.GetText()
    tokenText.Substring(startBorderLength, tokenText.Length - endBorderLength - startBorderLength)

let emptyString = "\"\""
let emptyChar = "''"

let getCorrespondingQuotesPair char =
    match char with
    | '"' -> emptyString
    | '\'' -> emptyChar
    | _ -> failwithf "Got char: %O" char

let isRegularStringToken (tokenType: TokenNodeType) =
    tokenType == FSharpTokenType.STRING ||
    tokenType == FSharpTokenType.REGULAR_INTERPOLATED_STRING ||
    tokenType == FSharpTokenType.REGULAR_INTERPOLATED_STRING_START ||
    tokenType == FSharpTokenType.REGULAR_INTERPOLATED_STRING_MIDDLE ||
    tokenType == FSharpTokenType.REGULAR_INTERPOLATED_STRING_END

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

let isFullInterpolatedStringToken (tokenType: TokenNodeType) =
    tokenType == FSharpTokenType.REGULAR_INTERPOLATED_STRING ||
    tokenType == FSharpTokenType.VERBATIM_INTERPOLATED_STRING ||
    tokenType == FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING

let isInterpolatedStringPartToken (tokenType: TokenNodeType) =
    isInterpolatedStringStartToken tokenType ||
    isInterpolatedStringMiddleToken tokenType ||
    isInterpolatedStringEndToken tokenType
