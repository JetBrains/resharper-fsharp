namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

[<ElementProblemAnalyzer([| typeof<IItemIndexerExpr> |], HighlightingTypes = [| typeof<RedundantIndexerDotWarning> |])>]
type IndexerExprAnalyzer() =
    inherit ElementProblemAnalyzer<IItemIndexerExpr>()

    let isApplicable (indexerExpr: IItemIndexerExpr) : bool =
        match indexerExpr.Qualifier with
        | :? IPrefixAppExpr as prefixAppExpr -> not prefixAppExpr.IsHighPrecedence
        | _ -> true

    override this.Run(indexerExpr, data, consumer) =
        if data.IsFSharp60Supported && isApplicable indexerExpr then
            consumer.AddHighlighting(RedundantIndexerDotWarning(indexerExpr))
