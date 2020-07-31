namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.CommonErrors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.Util

[<ElementProblemAnalyzer(typeof<IParenExpr>,
                         HighlightingTypes = [| typeof<RedundantParenExprWarning> |])>]
type RedundantParenAnalyzer() =
    inherit ElementProblemAnalyzer<IParenExpr>()

    let addHighlighting (consumer: IHighlightingConsumer) (parenExpr: IParenExpr) =
        let leftParen = parenExpr.LeftParen
        let rightParen = parenExpr.RightParen
        if isNull leftParen || isNull rightParen then () else

        let highlighting = RedundantParenExprWarning(parenExpr)
        consumer.AddHighlighting(highlighting, leftParen.GetHighlightingRange())
        consumer.AddHighlighting(highlighting, rightParen.GetHighlightingRange(), isSecondaryHighlighting = true)

    override x.Run(parenExpr, data, consumer) =
        if data.GetData(experimentalFeaturesEnabledKey) != BooleanBoxes.True then () else

        let innerExpression = parenExpr.InnerExpression

        if innerExpression :? IParenExpr || not (needsParens null innerExpression) then
            addHighlighting consumer parenExpr
