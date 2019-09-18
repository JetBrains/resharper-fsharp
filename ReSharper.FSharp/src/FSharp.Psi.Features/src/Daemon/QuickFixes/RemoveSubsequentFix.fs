namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type RemoveSubsequentFix(warning: UnitTypeExpectedWarning) =
    inherit QuickFixBase()

    let expr = warning.Expr

    override x.Text = "Remove subsequent expressions"

    override x.IsAvailable _ =
        isValid expr &&

        let seqExpr = SequentialExprNavigator.GetByExpression1(expr)
        isValid seqExpr

    override x.ExecutePsiTransaction(_, _) =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        let seqExpr = SequentialExprNavigator.GetByExpression1(expr)
        ModificationUtil.ReplaceChild(seqExpr, expr.Copy()) |> ignore
        
        null
