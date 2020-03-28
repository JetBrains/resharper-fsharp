namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.CommonErrors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Plugins.FSharp.Util

type ReplaceWithAssignmentExpressionFix(warning: UnitTypeExpectedWarning) =
    inherit FSharpQuickFixBase()
   
    let expr = warning.Expr.As<IBinaryAppExpr>()
    override x.IsAvailable _ =        
        if not (isValid expr) then false else
            
        // Now we can’t find out if the operator is '=', so we define it bypassing the grammar rules
        // TODO: rewrite after https://youtrack.jetbrains.com/issue/RIDER-41848 fix
        match expr.LeftArgument with
        | :? IReferenceExpr
        | :? IIndexerExpr -> getTokenType expr.Operator.FirstChild == FSharpTokenType.EQUALS
        | _ -> false
        
    override x.Text = "Replace with assignment expression"
    
    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        let factory = expr.CreateElementFactory()
        let setExpr = factory.CreateSetExpr(expr.LeftArgument, expr.RightArgument)
        replace expr setExpr
