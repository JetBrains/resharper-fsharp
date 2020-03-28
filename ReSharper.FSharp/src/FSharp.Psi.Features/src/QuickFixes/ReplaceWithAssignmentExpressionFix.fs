namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.CommonErrors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util

type ReplaceWithAssignmentExpressionFix(warning: UnitTypeExpectedWarning) =
    inherit FSharpQuickFixBase()
   
    let expr = warning.Expr.As<IBinaryAppExpr>()
    override x.IsAvailable _ =        
        if not (isValid expr) || expr = null then false else
        match expr.LeftArgument with
        | :? IReferenceExpr
        | :? IIndexerExpr -> true
        | _ -> false
        //isPredefinedFunctionRef "=" (info.Result.DeclaredElement :?> ISynExpr)
        
    override x.Text = "Replace with assignment expression"
    
    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        let factory = warning.Expr.CreateElementFactory()
        let setExpr = factory.CreateSetExpr(expr.LeftArgument, expr.RightArgument)
        replace expr setExpr
