namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

[<ElementProblemAnalyzer(typeof<IParenExpr>, HighlightingTypes = [| typeof<RedundantParenExpressionWarning> |])>]
type RedundantParenExprAnalyzer() =
    inherit ElementProblemAnalyzer<IParenExpr>()

    override x.Run(parenExpr, _, consumer) =
        let highlight () =
            let leftParen = parenExpr.LeftParen
            let rightParen = parenExpr.RightParen
            if isNull leftParen || isNull rightParen then () else

            let highlighting = RedundantParenExpressionWarning(parenExpr)

            consumer.AddHighlighting(highlighting, leftParen.GetHighlightingRange())
            consumer.AddHighlighting(highlighting, rightParen.GetHighlightingRange(), isSecondaryHighlighting = true)

        let parent = parenExpr.Parent
        if (parent :? IParenExpr || parent :? IChameleonExpression) && isNotNull parenExpr.InnerExpression then
            highlight () else

        let letOrUseExpr = LocalBindingNavigator.GetByExpression(parenExpr)
        if isNotNull letOrUseExpr then highlight ()
