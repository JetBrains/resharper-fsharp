namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell

type RemoveSubsequentFix(expr: IFSharpExpression) =
    inherit FSharpQuickFixBase()

    new (warning: UnitTypeExpectedWarning) =
        RemoveSubsequentFix(warning.Expr)

    new (warning: FunctionValueUnexpectedWarning) =
        RemoveSubsequentFix(warning.Expr)

    override x.Text = "Remove subsequent expressions"

    override x.IsAvailable _ =
        isValid expr &&

        let seqExpr = SequentialExprNavigator.GetByExpression(expr)
        isNotNull seqExpr && not (SequentialExprUtil.isLastExprInSeqExpr seqExpr expr)

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())

        let seqExpr = SequentialExprNavigator.GetByExpression(expr)

        let exprs = seqExpr.Expressions
        let exprIndex = Seq.findIndex ((==) expr) exprs

        let firstExprToRemove =
            Seq.skip (exprIndex + 1) exprs |> Seq.head

        if exprIndex = 0 then
            // replace the whole sequential expression with the expression to keep
            let last =
                skipMatchingNodesBefore isInlineSpace firstExprToRemove
                |> skipNewLineBefore

            ModificationUtil.ReplaceChildRange(TreeRange(seqExpr), TreeRange(seqExpr.FirstChild, last)) |> ignore
        else
            // remove the subsequent expressions range
            let first =
                firstExprToRemove
                |> skipMatchingNodesBefore isInlineSpace
                |> getThisOrPrevNewLine

            ModificationUtil.DeleteChildRange(first, seqExpr.LastChild)
