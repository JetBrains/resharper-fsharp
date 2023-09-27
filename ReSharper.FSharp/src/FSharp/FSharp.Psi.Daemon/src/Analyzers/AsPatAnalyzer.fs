namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

[<ElementProblemAnalyzer([| typeof<IAsPat> |],
                         HighlightingTypes = [| typeof<RedundantAsPatternWarning> |])>]
type AsPatAnalyzer() =
    inherit ElementProblemAnalyzer<IAsPat>()

    override x.Run(asPat, _, consumer) =
        if asPat.LeftPattern.IgnoreInnerParens() :? IWildPat then
            consumer.AddHighlighting(RedundantAsPatternWarning(asPat))
