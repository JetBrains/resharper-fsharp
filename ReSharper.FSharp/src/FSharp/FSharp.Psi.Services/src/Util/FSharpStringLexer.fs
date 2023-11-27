namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util

open System
open System.Globalization
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.Services
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

[<AbstractClass>]
type FSharpStringLexerBase(buffer) =
    inherit StringLexerBase(buffer)

    member x.Position with get () = base.Position and
                           set value = base.Position <- value

type RegularStringLexer(buffer) =
    inherit FSharpStringLexerBase(buffer)

    static let maxUnicodeCodePoint = uint32 0x10FFFF

    static let mutable isHexDigit = Func<_, _>(CharEx.IsHexDigitFast)
    static let mutable isDigit = Func<_, _>(Char.IsDigit)

    override x.StartOffset = 1
    override x.EndOffset = 1

    override x.AdvanceInternal() =
        match x.Buffer[x.Position] with
        | '\\' ->
            x.Position <- x.Position + 1
            if x.CanAdvance then x.ProcessEscapeSequence()
            else StringTokenTypes.CHARACTER
        | _ -> StringTokenTypes.CHARACTER

    abstract ProcessEscapeSequence: unit -> TokenNodeType
    default x.ProcessEscapeSequence() =
        match x.Buffer[x.Position] with
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

type RegularInterpolatedStringLexer(buffer) =
    inherit RegularStringLexer(buffer)

    override x.StartOffset = 2

    override x.AdvanceInternal() =
        match InterpolatedStringLexer.advance x with
        | null -> base.AdvanceInternal()
        | nodeType -> nodeType

type RegularInterpolatedStringMiddleEndLexer(buffer) =
    inherit RegularInterpolatedStringLexer(buffer)

    override x.StartOffset = 1

type VerbatimStringLexer(buffer) =
    inherit FSharpStringLexerBase(buffer)

        override x.StartOffset = 2
        override x.EndOffset = 1

        override x.AdvanceInternal() =
            if x.Buffer[x.Position] = '\"' then
                x.Position <- x.Position + 1

                if x.CanAdvance && x.Buffer[x.Position] = '\"' then StringTokenTypes.ESCAPE_CHARACTER else
                StringTokenTypes.CHARACTER

            else StringTokenTypes.CHARACTER

        override x.ParseEscapeCharacter _ = raise (NotImplementedException())

type VerbatimByteArrayLexer(buffer) =
    inherit VerbatimStringLexer(buffer)

    override x.EndOffset = 2

type VerbatimInterpolatedStringLexer(buffer) =
    inherit VerbatimStringLexer(buffer)

    override x.StartOffset = 3

    override x.AdvanceInternal() =
        match InterpolatedStringLexer.advance x with
        | null -> base.AdvanceInternal()
        | nodeType -> nodeType

type VerbatimInterpolatedStringMiddleEndLexer(buffer) =
    inherit VerbatimInterpolatedStringLexer(buffer)

    override x.StartOffset = 1


type TripleQuoteStringLexer(buffer) =
    inherit VerbatimStringLexer(buffer)

    override x.StartOffset = 3
    override x.EndOffset = 3

    override x.AdvanceInternal() = StringTokenTypes.CHARACTER

type TripleQuoteInterpolatedStringLexer(buffer, dollarCount) =
    inherit TripleQuoteStringLexer(buffer)

    override x.StartOffset = dollarCount + 3

    override x.AdvanceInternal() =
        match InterpolatedStringLexer.advance x with
        | null -> base.AdvanceInternal()
        | nodeType -> nodeType

type TripleQuoteInterpolatedStringStartLexer(buffer, dollarCount) =
    inherit TripleQuoteInterpolatedStringLexer(buffer, dollarCount)

    override x.EndOffset = dollarCount

type TripleQuoteInterpolatedStringMiddleLexer(buffer, dollarCount) =
    inherit TripleQuoteInterpolatedStringLexer(buffer, dollarCount)

    override x.StartOffset = dollarCount
    override x.EndOffset = dollarCount

type TripleQuoteInterpolatedStringEndLexer(buffer, dollarCount) =
    inherit TripleQuoteInterpolatedStringLexer(buffer, dollarCount)

    override x.StartOffset = dollarCount
    override x.EndOffset = 3


type ByteArrayLexer(buffer) =
    inherit RegularStringLexer(buffer)

    override x.EndOffset = 2

    override x.ProcessEscapeSequence() =
        match x.Buffer[x.Position] with
        | '\\' -> StringTokenTypes.ESCAPE_CHARACTER
        | _ -> StringTokenTypes.CHARACTER

    override x.ParseEscapeCharacter _ = raise (NotImplementedException())


module InterpolatedStringLexer =
    let private checkChar (lexer: FSharpStringLexerBase) c =
        lexer.Position <- lexer.Position + 1
        if lexer.CanAdvance && lexer.Buffer[lexer.Position] = c then
            StringTokenTypes.ESCAPE_CHARACTER
        else
            lexer.Position <- lexer.Position - 1
            StringTokenTypes.INVALID_CHARACTER

    let advance (lexer: FSharpStringLexerBase) =
        match lexer.Buffer[lexer.Position] with
        | '{' -> checkChar lexer '{'
        | '}' -> checkChar lexer '}'
        | _ -> null


[<Language(typeof<FSharpLanguage>)>]
type FSharpLiteralService() =
    inherit LiteralService()

    override this.IsConstantLiteral _ = failwith "todo"

    override this.GetValueTextRange(element) = element.GetTreeTextRange()

    override this.ParseStringLiteral(text: string, translator: byref<RangeTranslator>): string =
        translator <- RangeTranslator()
        translator.StartMapping(text.Length)
        translator.EndMapping(text.Length)
        text
