namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

[<ElementProblemAnalyzer(typeof<IParenPat>,
                         HighlightingTypes = [| typeof<RedundantParenPatWarning> |])>]
type RedundantParenPatAnalyzer() =
    inherit ElementProblemAnalyzer<IParenPat>()

    override x.Run(parenPat, _, consumer) =
        if not (parenPat.Pattern.IgnoreInnerParens() :? IWildPat) then () else

        let leftParen = parenPat.LeftParen
        let rightParen = parenPat.RightParen

        if isNull leftParen || isNull rightParen then () else

        let highlighting = RedundantParenPatWarning(parenPat)
        consumer.AddHighlighting(highlighting, leftParen.GetHighlightingRange())
        consumer.AddHighlighting(highlighting, rightParen.GetHighlightingRange(), isSecondaryHighlighting = true)
