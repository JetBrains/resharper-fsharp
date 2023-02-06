namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open System.Collections.Generic
open FSharp.Compiler.Syntax
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util

[<ElementProblemAnalyzer(typeof<FSharpIdentifierToken>,
                         HighlightingTypes = [| typeof<RedundantBackticksWarning> |])>]
type RedundantBackticksAnalyzer() =
    inherit ElementProblemAnalyzer<FSharpIdentifierToken>()

    let reservedKeywords =
        [ "break"
          "checked"
          "component"
          "constraint"
          "continue"
          "fori"
          "include"
          "mixin"
          "parallel"
          "params"
          "process"
          "protected"
          "pure"
          "sealed"
          "trait"
          "tailcall"
          "virtual" ]
        |> HashSet

    override x.Run(identifier, _, consumer) =
        let text = identifier.GetText()
        if text.Length <= 4 then () else

        let withoutBackticks = text.RemoveBackticks()
        if text.Length = withoutBackticks.Length || withoutBackticks = "_" ||
                reservedKeywords.Contains(withoutBackticks) then () else

        let escaped = PrettyNaming.AddBackticksToIdentifierIfNeeded withoutBackticks
        if escaped.Length = withoutBackticks.Length then
            consumer.AddHighlighting(RedundantBackticksWarning(identifier))
