namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
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

        let expr = yieldExpr.Expression
        let shift = expr.Indent - yieldExpr.Indent
        if shift > 0 then
            // Parsing `return` currently doesn't support deindenting,
            // but we do a defensive indent diff check in case it's supported in future.
            shiftExpr -shift yieldExpr

        replaceWithCopy yieldExpr expr
