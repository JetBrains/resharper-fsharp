namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type RemoveYieldFix(yieldExpr: IYieldOrReturnExpr) =
    inherit FSharpQuickFixBase()

    let yieldKeyword = yieldExpr.YieldKeyword

    new (error: ReturnRequiresComputationExpressionError) =
        RemoveYieldFix(error.YieldExpr)

    new (error: YieldRequiresSeqExpressionError) =
        RemoveYieldFix(error.YieldExpr)

    override x.Text = "Remove " + yieldKeyword.GetTokenType().TokenRepresentation

    override x.IsAvailable _ =
        isValid yieldExpr && isValid yieldKeyword

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(yieldExpr.IsPhysical())
        replaceWithCopy yieldExpr yieldExpr.Expression
