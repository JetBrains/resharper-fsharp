namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open System
open System.Globalization
open JetBrains.Diagnostics
open JetBrains.ReSharper.Daemon.StringAnalysis
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Text
open JetBrains.Util

[<ElementProblemAnalyzer(typeof<FSharpString>)>]
type FSharpStringProblemAnalyzer() =
    inherit StringProblemAnalyzerBase<FSharpString>()

    static let stringLexerKey = Key<CachedPsiValue<IStringLexer>>("CachedFSharpLiteralWrapper")

    let createLexer (literalToken: FSharpString): IStringLexer  =
        let literalType = literalToken.GetTokenType().GetLiteralType()
        let buffer = StringBuffer(literalToken.GetText())

        match literalType with
        | FSharpLiteralType.Character
        | FSharpLiteralType.RegularString -> RegularStringLexer(buffer) :> _
        | FSharpLiteralType.VerbatimString -> VerbatimStringLexer(buffer) :> _
        | FSharpLiteralType.TripleQuoteString -> TripleQuoteStringLexer(buffer) :> _
        | FSharpLiteralType.ByteArray -> ByteArrayStringLexer(buffer) :> _

    let getCachedLexer (literalToken: FSharpString) =
        let isValid = literalToken.IsValid()
        if not isValid then createLexer literalToken else

        match literalToken.UserData.GetData(stringLexerKey) with
        | null ->
            let cachedValue = CachedPsiValue()
            let lexer = createLexer literalToken
            cachedValue.SetValue(literalToken, lexer)
            literalToken.UserData.PutData(stringLexerKey, cachedValue)
            lexer

        | cachedValue ->
            match cachedValue.GetValue(literalToken) with
            | null ->
                let lexer = createLexer literalToken
                cachedValue.SetValue(literalToken, lexer)
                lexer

            | lexer -> lexer

    override x.ExtractElements(literalToken: FSharpString, consumer: IHighlightingConsumer) =
        let lexer = getCachedLexer literalToken
        [| Pair(literalToken :> ITokenNode, lexer) |] :> _




type RegularStringLexer(buffer) =
    inherit StringLexerBase(buffer)

    static let maxUnicodeCodePoint = uint32 0x10FFFF

    static let mutable isHexDigit = Func<_, _>(CharEx.IsHexDigitFast)
    static let mutable isDigit = Func<_, _>(Char.IsDigit)

    override x.StartOffset = 1
    override x.EndOffset = 1

    override x.AdvanceInternal() =
        match x.Buffer.[x.Position] with
        | '\\' ->
            x.Position <- x.Position + 1
            if x.CanAdvance then x.ProcessEscapeSequence()
            else StringTokenTypes.CHARACTER
        | _ -> StringTokenTypes.CHARACTER

    abstract ProcessEscapeSequence: unit -> TokenNodeType
    default x.ProcessEscapeSequence() =
        match x.Buffer.[x.Position] with
        | 'u' -> x.ProcessHexEscapeSequence(4)
        | 'U' -> x.ProcessLongHexEscapeSequence()
        | 'x' -> x.ProcessHexEscapeSequence(2)
        | c when Char.IsDigit(c) -> x.ProcessNumericCharSequence() 
        | '"' | '\'' | '\\' | 'b' | 'n' | 'r' | 't' | 'a' | 'f' | 'v' -> StringTokenTypes.ESCAPE_CHARACTER
        | _ -> StringTokenTypes.CHARACTER

    member x.ProcessEscapeSequence(length, shift, matcher) =
        let str = x.ProcessEscapeSequence(length, length, shift, matcher)
        if str.Length = length then StringTokenTypes.ESCAPE_CHARACTER else StringTokenTypes.CHARACTER

    member x.ProcessHexEscapeSequence(length) =
        x.ProcessEscapeSequence(length, 1, isHexDigit)

    member x.ProcessNumericCharSequence() =
        x.ProcessEscapeSequence(3, 0, isDigit)

    member x.ProcessLongHexEscapeSequence() =
        let hex = x.ProcessEscapeSequence(8, max = 8, shift = 1, matcher = (fun c -> c.IsHexDigitFast()))
        if hex.Length <> 8 then StringTokenTypes.CHARACTER else

        let mutable codePoint = Unchecked.defaultof<uint32>
        match UInt32.TryParse(hex, NumberStyles.HexNumber, null, &codePoint) with
        | true when codePoint <= maxUnicodeCodePoint -> StringTokenTypes.ESCAPE_CHARACTER
        | _ -> StringTokenTypes.CHARACTER

    override x.ParseEscapeCharacter _ = raise (NotImplementedException())


type VerbatimStringLexer(buffer) =
    inherit StringLexerBase(buffer)

        override x.StartOffset = 2
        override x.EndOffset = 1

        override x.AdvanceInternal() =
            if x.Buffer.[x.Position] = '\"' then
                x.Position <- x.Position + 1

                if x.CanAdvance && x.Buffer.[x.Position] = '\"' then StringTokenTypes.ESCAPE_CHARACTER else
                StringTokenTypes.CHARACTER

            else StringTokenTypes.CHARACTER

        override x.ParseEscapeCharacter _ = raise (NotImplementedException())


type TripleQuoteStringLexer(buffer) =
        inherit VerbatimStringLexer(buffer)

        override x.StartOffset = 4
        override x.EndOffset = 4

        override x.AdvanceInternal() = StringTokenTypes.CHARACTER


type ByteArrayStringLexer(buffer) =
    inherit RegularStringLexer(buffer)

    override x.EndOffset = 2

    override x.ProcessEscapeSequence() =
        match x.Buffer.[x.Position] with
        | '\\' -> StringTokenTypes.ESCAPE_CHARACTER
        | _ -> StringTokenTypes.CHARACTER

    override x.ParseEscapeCharacter _ = raise (NotImplementedException())
