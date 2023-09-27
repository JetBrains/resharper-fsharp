namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.ParenPatUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

[<ElementProblemAnalyzer(typeof<IParenPat>, HighlightingTypes = [| typeof<RedundantParenPatWarning> |])>]
type RedundantParenPatAnalyzer() =
    inherit ElementProblemAnalyzer<IParenPat>()

    override x.Run(parenPat, _, consumer) =
        let innerPattern = parenPat.Pattern
        if isNull innerPattern then () else

        let context = parenPat.IgnoreParentParens()
        if escapesTuplePatParamDecl context innerPattern then () else

        if innerPattern :? IParenPat || not (needsParens context innerPattern) && innerPattern.IsSingleLine then
            consumer.AddHighlighting(RedundantParenPatWarning(parenPat))

    interface IFSharpRedundantParenAnalyzer
