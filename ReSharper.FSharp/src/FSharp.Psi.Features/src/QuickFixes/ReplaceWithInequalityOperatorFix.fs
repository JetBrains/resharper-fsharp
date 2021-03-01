namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Resources.Shell

type ReplaceWithInequalityOperatorFix(error: ErrorFromAddingConstraintError) =
    inherit FSharpQuickFixBase()

    let expr = error.Expr

    override x.IsAvailable _ =
        match expr with
        | :? IReferenceExpr as ref -> isValid ref && ref.ShortName = "!="
        | _ -> false

    override x.Text = "Replace with '<>'"

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()
        let factory = expr.CreateElementFactory()

        replace expr (factory.CreateReferenceExpr("op_Inequality").SetName("<>"))
