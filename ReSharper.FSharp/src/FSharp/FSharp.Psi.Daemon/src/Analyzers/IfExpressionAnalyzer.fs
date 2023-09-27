namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.Errors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.PsiUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util

[<ElementProblemAnalyzer([| typeof<IIfThenElseExpr> |],
                         HighlightingTypes = [| typeof<ExpressionCanBeReplacedWithConditionWarning> |])>]
type IfExpressionAnalyzer() =
    inherit ElementProblemAnalyzer<IIfThenElseExpr>()

    override this.Run(expr, _, consumer) =
        let thenExpr = expr.ThenExpr
        let elseExpr = expr.ElseExpr

        match thenExpr.IgnoreInnerParens(), elseExpr.IgnoreInnerParens() with
        | :? ILiteralExpr as thenLiteral, (:? ILiteralExpr as elseLiteral) ->
            let thenTokenType = getTokenType thenLiteral.Literal
            let elseTokenType = getTokenType elseLiteral.Literal

            let createHighlighting =
                thenTokenType == FSharpTokenType.TRUE && elseTokenType == FSharpTokenType.FALSE ||
                thenTokenType == FSharpTokenType.FALSE && elseTokenType == FSharpTokenType.TRUE

            if not createHighlighting then () else

            let thenKeyword = expr.ThenKeyword
            let elseKeyword = expr.ElseKeyword
            if isNotNull thenKeyword && isNotNull elseKeyword &&
               skipMatchingNodesAfter isWhitespace thenKeyword == thenExpr &&
               skipMatchingNodesAfter isWhitespace thenExpr == elseKeyword &&
               skipMatchingNodesAfter isWhitespace elseKeyword == elseExpr
            then
                let needsNegation = thenTokenType == FSharpTokenType.FALSE
                consumer.AddHighlighting(ExpressionCanBeReplacedWithConditionWarning(expr, needsNegation))

        | _ -> ()
