namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Analyzers

open JetBrains.ReSharper.Daemon.StringAnalysis
open JetBrains.ReSharper.Daemon.SyntaxHighlighting
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
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
        | FSharpLiteralType.VerbatimByteArray -> VerbatimByteArrayLexer(buffer) :> _
        | FSharpLiteralType.TripleQuoteString -> TripleQuoteStringLexer(buffer) :> _
        | FSharpLiteralType.ByteArray -> ByteArrayLexer(buffer) :> _

        | FSharpLiteralType.InterpolatedString
        | FSharpLiteralType.InterpolatedStringStart -> RegularInterpolatedStringLexer(buffer) :> _
        | FSharpLiteralType.InterpolatedStringMiddle
        | FSharpLiteralType.InterpolatedStringEnd -> RegularInterpolatedStringMiddleEndLexer(buffer) :> _

        | FSharpLiteralType.VerbatimInterpolatedString
        | FSharpLiteralType.VerbatimInterpolatedStringStart -> VerbatimInterpolatedStringLexer(buffer) :> _
        | FSharpLiteralType.VerbatimInterpolatedStringMiddle
        | FSharpLiteralType.VerbatimInterpolatedStringEnd -> VerbatimInterpolatedStringMiddleEndLexer(buffer) :> _

        | FSharpLiteralType.TripleQuoteInterpolatedString -> TripleQuoteInterpolatedStringLexer(buffer) :> _
        | FSharpLiteralType.TripleQuoteInterpolatedStringStart -> TripleQuoteInterpolatedStringStartLexer(buffer) :> _
        | FSharpLiteralType.TripleQuoteInterpolatedStringMiddle -> TripleQuoteInterpolatedStringMiddleLexer(buffer) :> _
        | FSharpLiteralType.TripleQuoteInterpolatedStringEnd -> TripleQuoteInterpolatedStringEndLexer(buffer) :> _

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

    override x.ExtractElements(literalToken: FSharpString, _ ,_) =
        [| Pair(literalToken :> ITokenNode, getCachedLexer literalToken) |] :> _


[<AbstractClass>]
type FSharpStringLexerBase(buffer) =
    inherit StringLexerBase(buffer)

    member x.Position with get () = base.Position and
                           set value = base.Position <- value


[<ElementProblemAnalyzer(typeof<IInterpolatedStringExpr>)>]
type InterpolatedStringExprAnalyzer() =
    inherit ElementProblemAnalyzer<IInterpolatedStringExpr>()

    override this.Run(expr, data, consumer) =
        if not data.IsFSharp50Supported then () else

        if expr.IsTrivial() then
            consumer.AddHighlighting(RedundantStringInterpolationWarning(expr))
        else
            let range = expr.GetDollarSignRange()
            consumer.AddHighlighting(ReSharperSyntaxHighlighting(FSharpHighlightingAttributeIds.Method, null, range))
