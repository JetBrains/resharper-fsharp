namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.Errors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

[<ElementProblemAnalyzer([| typeof<INamedSelfId> |], HighlightingTypes = [| typeof<UseWildSelfIdWarning> |])>]
type SelfIdAnalyzer() =
    inherit ElementProblemAnalyzer<INamedSelfId>()

    override this.Run(selfId, data, consumer) =
        if selfId.SourceName = "__" && data.IsFSharp47Supported then
            consumer.AddHighlighting(UseWildSelfIdWarning(selfId))
