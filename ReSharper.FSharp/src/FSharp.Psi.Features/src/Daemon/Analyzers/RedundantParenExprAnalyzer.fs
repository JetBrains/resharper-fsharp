namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.Util

type IFSharpRedundantParenAnalyzer =
    inherit IElementProblemAnalyzer

[<ElementProblemAnalyzer(typeof<IParenExpr>, HighlightingTypes = [| typeof<RedundantParenExprWarning> |])>]
type RedundantParenExprAnalyzer() =
    inherit ElementProblemAnalyzer<IParenExpr>()

    override x.Run(parenExpr, data, consumer) =
        if isNull parenExpr.LeftParen || isNull parenExpr.RightParen then () else
        let innerExpr = parenExpr.InnerExpression

        if isNull innerExpr then () else
        if precedence innerExpr < 13 && data.GetData(redundantParensEnabledKey) != BooleanBoxes.True then () else

        let context = parenExpr.IgnoreParentParens()
        if escapesTupleAppArg context innerExpr then () else

        if innerExpr :? IParenExpr || not (needsParens context innerExpr) then
            consumer.AddHighlighting(RedundantParenExprWarning(parenExpr))

    interface IFSharpRedundantParenAnalyzer
