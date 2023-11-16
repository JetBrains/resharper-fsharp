namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.Util

type IFSharpRedundantParenAnalyzer =
    inherit IElementProblemAnalyzer

[<ElementProblemAnalyzer(typeof<IParenExpr>, HighlightingTypes = [| typeof<RedundantParenExprWarning> |])>]
type RedundantParenExprAnalyzer() =
    inherit ElementProblemAnalyzer<IParenExpr>()

    let isEnabled (expr: IFSharpExpression) (context: IFSharpExpression) (data: ElementProblemAnalyzerData) =
        if precedence expr >= 13 then true else

        if data.GetData(redundantParensEnabledKey) == BooleanBoxes.True then true else

        isNotNull (EnumCaseDeclarationNavigator.GetByExpression(context))

    override x.Run(parenExpr, data, consumer) =
        if isNull parenExpr.LeftParen || isNull parenExpr.RightParen then () else
        let innerExpr = parenExpr.InnerExpression
        if isNull innerExpr then () else

        let context = parenExpr.IgnoreParentParens(includingBeginEndExpr = false)
        if not (isEnabled innerExpr context data) then () else

        let context = parenExpr.IgnoreParentParens(includingBeginEndExpr = false)
        if escapesTupleAppArg context innerExpr || escapesAppAtNamedArgPosition parenExpr then () else
        if escapesRefExprAtNamedArgPosition context innerExpr then () else
        if escapesEnumFieldLiteral parenExpr then () else

        let allowHighPrecedenceAppParens () = data.AllowHighPrecedenceAppParens
        if innerExpr :? IParenExpr || not (needsParensImpl allowHighPrecedenceAppParens context innerExpr) then
            consumer.AddHighlighting(RedundantParenExprWarning(parenExpr))

    interface IFSharpRedundantParenAnalyzer
