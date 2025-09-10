namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.FcsErrors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util

type AddMissingSeqFix(expr: IComputationExpr) =
    inherit FSharpQuickFixBase()
    
    new(error: ConstructDeprecatedSequenceExpressionsInvalidFormError) =
        AddMissingSeqFix(error.ComputationExpr)
        
    new(error: InvalidRecordSequenceOrComputationExpressionError) =
        AddMissingSeqFix(error.ComputationExpr)

    override x.Text = "Add missing 'seq'"

    override x.IsAvailable _ =
        isValid expr

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        
        let factory = expr.Expression.CreateElementFactory()
        let seqRefExpr = factory.CreateReferenceExpr("seq") :> IFSharpExpression
        let newAppExpr = factory.CreateAppExpr(seqRefExpr, expr, true)
        
        let inserted = ModificationUtil.ReplaceChild(expr, newAppExpr)
        
        // Add parentheses only when required by the surrounding context (e.g., application or member access)
        addParensIfNeeded inserted |> ignore  
        
