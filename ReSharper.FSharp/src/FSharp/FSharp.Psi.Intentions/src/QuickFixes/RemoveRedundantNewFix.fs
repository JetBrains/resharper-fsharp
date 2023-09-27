namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

type RemoveRedundantNewFix(warning: RedundantNewWarning) =
    inherit FSharpScopedQuickFixBase(warning.NewExpr)

    let newExpr = warning.NewExpr

    override x.Text = "Remove redundant 'new'"
    override x.IsAvailable _ = isValid newExpr

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(newExpr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let factory = newExpr.CreateElementFactory()
        let refExpr = factory.AsReferenceExpr(newExpr.TypeName)

        let argExpr = newExpr.ArgumentExpression
        let addSpace = not (argExpr :? IParenExpr || argExpr :? IUnitExpr)

        // todo: convert referenceName, copy other children from original node
        let appExpr = factory.CreateAppExpr(refExpr, argExpr, addSpace)
        ModificationUtil.ReplaceChild(newExpr, appExpr) |> ignore
