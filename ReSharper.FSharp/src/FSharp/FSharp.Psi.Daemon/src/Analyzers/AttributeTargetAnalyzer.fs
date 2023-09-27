namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Analyzers

open System.Collections.Generic
open JetBrains.ReSharper.Daemon.SyntaxHighlighting
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree

[<ElementProblemAnalyzer(typeof<IAttributeTarget>, HighlightingTypes = [| typeof<ReSharperSyntaxHighlighting> |])>]
type AttributeTargetAnalyzer() =
    inherit ElementProblemAnalyzer<IAttributeTarget>()

    let allowedTargets =
        [| "assembly"; "return"; "field"; "property"; "method"; "param"; "type"; "constructor"; "event" |]
        |> HashSet

    override x.Run(attributeTarget, _, consumer) =
        let target = attributeTarget.Identifier
        if isNotNull target && allowedTargets.Contains(target.GetText()) then
            let range = target.GetDocumentRange()
            consumer.AddHighlighting(ReSharperSyntaxHighlighting(FSharpHighlightingAttributeIds.Keyword, null, range))
