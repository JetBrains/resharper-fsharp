namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.Errors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.PsiUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util

[<ElementProblemAnalyzer([| typeof<IIfThenElseExpr> |],
                         HighlightingTypes = [| typeof<IfCanBeReplacedWithConditionOperandWarning> |])>]
type IfExpressionAnalyzer() =
    inherit ElementProblemAnalyzer<IIfThenElseExpr>()

    override this.Run(expr, _, consumer) =
        let thenExpr = expr.ThenExpr
        let elseExpr = expr.ElseExpr

        match thenExpr.IgnoreInnerParens(), elseExpr.IgnoreInnerParens() with
        | :? ILiteralExpr as thenLiteral, (:? ILiteralExpr as elseLiteral) ->
            let thenToken = getTokenType thenLiteral.Literal
            let elseToken = getTokenType elseLiteral.Literal

            let createHighlighting, needNegation =
                if thenToken == FSharpTokenType.TRUE && elseToken == FSharpTokenType.FALSE then true, false
                elif thenToken == FSharpTokenType.FALSE && elseToken == FSharpTokenType.TRUE then true, true
                else false, false

            if (createHighlighting &&
                let thenKeyword = expr.ThenKeyword
                let elseKeyword = expr.ElseKeyword
                isNotNull thenKeyword && isNotNull elseKeyword &&
                (skipMatchingNodesAfter isWhitespace thenKeyword == thenExpr) &&
                (skipMatchingNodesAfter isWhitespace thenExpr == elseKeyword) &&
                (skipMatchingNodesAfter isWhitespace elseKeyword == elseExpr))
            then consumer.AddHighlighting(IfCanBeReplacedWithConditionOperandWarning(expr, needNegation))

        | _ -> ()
