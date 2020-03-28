namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.CommonErrors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI

type ReplaceWithAssignmentExpressionFix(warning: UnitTypeExpectedWarning) =
    inherit FSharpQuickFixBase()
   
    let expr = warning.Expr.As<IBinaryAppExpr>()
    override x.IsAvailable _ =        
        if not (isValid expr) || expr = null then false else
        match expr.LeftArgument with
        | :? IReferenceExpr
        | :? IIndexerExpr -> isEquals expr.Operator.FirstChild
        | _ -> false
        
    override x.Text = "Replace with assignment expression"
    
    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        let factory = expr.CreateElementFactory()
        let setExpr = factory.CreateSetExpr(expr.LeftArgument, expr.RightArgument)
        replace expr setExpr
