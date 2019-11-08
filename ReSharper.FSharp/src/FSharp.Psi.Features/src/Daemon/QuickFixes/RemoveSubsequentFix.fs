namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell

type RemoveSubsequentFix(warning: UnitTypeExpectedWarning) =
    inherit QuickFixBase()

    let expr = warning.Expr

    override x.Text = "Remove subsequent expressions"

    override x.IsAvailable _ =
        if not (isValid expr) then false else

        let seqExpr = SequentialExprNavigator.GetByExpression(expr)
        if not (isValid seqExpr) then false else

        let lastExpr = seqExpr.Expressions.LastOrDefault()
        isNotNull lastExpr && expr != lastExpr

    override x.ExecutePsiTransaction(_, _) =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        let seqExpr = SequentialExprNavigator.GetByExpression(expr)

        let exprs = seqExpr.Expressions
        let exprIndex = Seq.findIndex ((==) expr) exprs

        let firstExprToRemove: ITreeNode =
            Seq.skip (exprIndex + 1) exprs |> Seq.head :> _

        if exprIndex = 0 && exprs.Count = 2 then
            let rangeToReplace = getRangeWithNewLineAfter seqExpr
            let last = firstExprToRemove |> getPrevSibling |> skipThisWhitespaceBeforeNode
            ModificationUtil.ReplaceChildRange(rangeToReplace, TreeRange(seqExpr.FirstChild, last)) |> ignore
            null
        else
            let first = firstExprToRemove |> skipPreviousWhitespaceBeforeNode
            ModificationUtil.DeleteChildRange(first, seqExpr.LastChild)
            null
