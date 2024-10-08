namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Analyzers

open FSharp.Compiler.Syntax
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
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
        if text.Length = withoutBackticks.Length || withoutBackticks = "_" ||
                FSharpNamingService.reservedKeywords.Contains(withoutBackticks) then () else

        let escaped = PrettyNaming.NormalizeIdentifierBackticks withoutBackticks
        if escaped.Length = withoutBackticks.Length then
            consumer.AddHighlighting(RedundantBackticksWarning(identifier))
