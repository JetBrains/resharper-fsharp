namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open FSharp.Compiler.Tokenization
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util

[<ElementProblemAnalyzer(typeof<FSharpIdentifierToken>,
                         HighlightingTypes = [| typeof<RedundantBackticksWarning> |])>]
type RedundantBackticksAnalyzer() =
    inherit ElementProblemAnalyzer<FSharpIdentifierToken>()

    override x.Run(identifier, _, consumer) =
        let text = identifier.GetText()
        if text.Length <= 4 then () else

        let withoutBackticks = text.RemoveBackticks()
        if text.Length = withoutBackticks.Length then () else

        let escaped = FSharpKeywords.QuoteIdentifierIfNeeded withoutBackticks
        if escaped.Length = withoutBackticks.Length then
            consumer.AddHighlighting(RedundantBackticksWarning(identifier))
