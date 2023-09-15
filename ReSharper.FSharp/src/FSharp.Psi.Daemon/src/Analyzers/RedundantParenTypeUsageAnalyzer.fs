namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.RedundantParenTypeUsageAnalyzer
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

[<ElementProblemAnalyzer(typeof<IParenTypeUsage>, HighlightingTypes = [| typeof<RedundantParenTypeUsageWarning> |])>]
type RedundantParenTypeUsageAnalyzer() =
    inherit ElementProblemAnalyzer<IParenTypeUsage>()

    override this.Run(parenTypeUsage, _, consumer) =
        if isNull parenTypeUsage.LeftParen || isNull parenTypeUsage.RightParen then () else

        let typeUsage = parenTypeUsage.InnerTypeUsage
        let context = typeUsage.IgnoreParentParens()

        if typeUsage :? IParenTypeUsage || applicable typeUsage && not (needsParens context typeUsage) then
            consumer.AddHighlighting(RedundantParenTypeUsageWarning(parenTypeUsage))

    interface IFSharpRedundantParenAnalyzer
