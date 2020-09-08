namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.Errors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

// todo: add for nested patterns like `1 :: 2 :: []`
// todo: add for expressions

[<ElementProblemAnalyzer([| typeof<IListConsPat> |],
                         HighlightingTypes = [| typeof<ConsWithEmptyListPatWarning> |])>]
type ListConsPatAnalyzer() =
    inherit ElementProblemAnalyzer<IListConsPat>()

    override x.Run(consPat, _, consumer) =
        let listPat = consPat.TailPattern.As<IListPat>()
        if isNotNull listPat && listPat.Patterns.IsEmpty then
            consumer.AddHighlighting(ConsWithEmptyListPatWarning(consPat))
