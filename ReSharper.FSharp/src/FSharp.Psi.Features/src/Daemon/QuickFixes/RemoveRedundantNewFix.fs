namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

type RemoveRedundantNewFix(warning: RedundantNewWarning) =
    inherit QuickFixBase()

    let newExpr = warning.NewExpr

    override x.Text = "Remove 'new'"
    override x.IsAvailable _ = isValid newExpr

    override x.ExecutePsiTransaction(_, _) =
        use writeCookie = WriteLockCookie.Create(newExpr.IsPhysical())

        let factory = newExpr.CreateElementFactory()
        let refExpr = factory.AsReferenceExpr(newExpr.TypeName)

        let appExpr = factory.CreateAppExpr(refExpr, newExpr.ArgExpression, false)
        ModificationUtil.ReplaceChild(newExpr, appExpr) |> ignore
        
        null