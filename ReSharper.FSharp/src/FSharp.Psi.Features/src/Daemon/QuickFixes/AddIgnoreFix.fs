namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Resources.Shell

type AddIgnoreFix(warning: UnitTypeExpectedWarning) =
    inherit QuickFixBase()

    let expr = warning.Expr

    let addNewLine (expr: ISynExpr) =
        if expr.IsSingleLine then false else

        match expr with
        | :? IMatchExpr
        | :? IMatchLambdaExpr
        | :? IIfThenElseExpr
        | :? ILambdaExpr
        | :? IDoExpr
        | :? IAssertExpr
        | :? ITryWithExpr
        | :? ITryFinallyExpr
        | :? ILazyExpr -> true
        | _ -> false

    override x.Text = "Ignore value"
    override x.IsAvailable _ = isValid expr

    override x.ExecutePsiTransaction(_, _) =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        let elementFactory = expr.CreateElementFactory()
        replace expr (elementFactory.CreateIgnoreApp(expr, addNewLine expr)) 
        null
