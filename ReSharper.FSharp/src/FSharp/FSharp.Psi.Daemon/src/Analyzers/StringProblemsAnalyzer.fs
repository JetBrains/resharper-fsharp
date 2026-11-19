namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Analyzers

open JetBrains.ReSharper.Daemon.StringAnalysis
open JetBrains.ReSharper.Daemon.SyntaxHighlighting
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

[<ElementProblemAnalyzer(typeof<FSharpString>)>]
type FSharpStringProblemAnalyzer() =
    inherit StringProblemAnalyzerBase<FSharpString>()

    static let stringLexerKey = Key<CachedPsiValue<IStringLexer>>("CachedFSharpLiteralWrapper")

    let createLexer (literalToken: FSharpString): IStringLexer =
        literalToken.CreateLexer()

    let getCachedLexer (literalToken: FSharpString) =
        if not (literalToken.IsValid()) then createLexer literalToken else

        let cachedValue = literalToken.UserData.GetOrCreateDataUnderLock(stringLexerKey, fun _ -> CachedPsiValue())
        cachedValue.GetValue(literalToken, createLexer)

    override x.ExtractElements(literalToken: FSharpString, _ ,_) =
        [| Pair(literalToken :> ITokenNode, getCachedLexer literalToken) |] :> _


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
