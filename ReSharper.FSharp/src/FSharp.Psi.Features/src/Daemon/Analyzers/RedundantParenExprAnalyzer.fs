namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.Util

[<ElementProblemAnalyzer(typeof<IParenExpr>, HighlightingTypes = [| typeof<RedundantParenExprWarning> |])>]
type RedundantParenExprAnalyzer() =
    inherit ElementProblemAnalyzer<IParenExpr>()

    override x.Run(parenExpr, data, consumer) =
        if data.GetData(redundantParenAnalysisEnabledKey) != BooleanBoxes.True then () else
        if isNull parenExpr.LeftParen || isNull parenExpr.RightParen then () else

        let innerExpression = parenExpr.InnerExpression

        if innerExpression :? IParenExpr || not (needsParens null innerExpression) then
            consumer.AddHighlighting(RedundantParenExprWarning(parenExpr))
