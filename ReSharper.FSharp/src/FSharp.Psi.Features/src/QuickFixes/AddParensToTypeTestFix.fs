namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type AddParensToTypeTestFix(error: RuntimeCoercionSourceSealedError) =
    inherit FSharpQuickFixBase()

    let typeTestExpr = error.Expr.As<ITypeTestExpr>()
    let prefixApp = if isNotNull typeTestExpr then typeTestExpr.Expression.As<IPrefixAppExpr>() else null
    let testedExpr = if isNotNull prefixApp then prefixApp.ArgumentExpression else null

    override x.Text = "Add parens to type test"
    override x.IsAvailable _ = isValid testedExpr

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(testedExpr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let testedExprCopy = testedExpr.Copy()
        let typeTestExprCopy = typeTestExpr.Copy()
        let prefixApp = ModificationUtil.ReplaceChild(typeTestExpr, prefixApp.Copy())

        typeTestExprCopy.SetExpression(testedExprCopy) |> ignore
        let parenExpr = ParenExprNavigator.GetByInnerExpression(addParens typeTestExprCopy)
        prefixApp.SetArgumentExpression(parenExpr) |> ignore
