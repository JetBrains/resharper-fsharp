namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.Errors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.PsiUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util

[<ElementProblemAnalyzer([| typeof<IIfThenElseExpr> |],
                         HighlightingTypes = [| typeof<IfCanBeReplacedByConditionOperandWarning> |])>]
type IfExpressionAnalyzer() =
    inherit ElementProblemAnalyzer<IIfThenElseExpr>()

    override this.Run(expr, _, consumer) =
        if not (skipMatchingNodesAfter isWhitespace expr.ThenKeyword == expr.ThenExpr) ||
           not (skipMatchingNodesAfter isWhitespace expr.ThenExpr == expr.ElseKeyword) ||
           not (skipMatchingNodesAfter isWhitespace expr.ElseKeyword == expr.ElseExpr) then () else

        let thenExpr = expr.ThenExpr.IgnoreInnerParens()
        let elseExpr = expr.ElseExpr.IgnoreInnerParens()

        match thenExpr, elseExpr with
        | :? ILiteralExpr as thenExpr, (:? ILiteralExpr as elseExpr) ->
            let thenToken = getTokenType thenExpr.Literal
            let elseToken = getTokenType elseExpr.Literal

            if thenToken = FSharpTokenType.TRUE &&
               elseToken = FSharpTokenType.FALSE then
                consumer.AddHighlighting(IfCanBeReplacedByConditionOperandWarning(expr, false))

            elif thenToken = FSharpTokenType.FALSE &&
                 elseToken = FSharpTokenType.TRUE then
                consumer.AddHighlighting(IfCanBeReplacedByConditionOperandWarning(expr, true))
        | _ -> ()
