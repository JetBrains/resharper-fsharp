namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type AddParensToTypedLikeExprFix(typedLikeExpr: ITypedLikeExpr) =
    inherit FSharpQuickFixBase()

    let prefixApp = if isNotNull typedLikeExpr then typedLikeExpr.Expression.As<IPrefixAppExpr>() else null
    let expr = if isNotNull prefixApp then prefixApp.ArgumentExpression else null

    new (error: RuntimeCoercionSourceSealedError) =
        AddParensToTypedLikeExprFix(error.Expr)

    new (error: TypeConstraintMismatchError) =
        AddParensToTypedLikeExprFix(error.Expr.As<ITypedLikeExpr>())

    override x.Text =
        match typedLikeExpr with
        | :? ICastExpr -> "Add parens to type cast"
        | :? ITypeTestExpr -> "Add parens to type test"
        | _ -> ""

    override x.IsAvailable _ = isValid expr

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let exprCopy = expr.Copy()
        let typedLikeExprCopy = typedLikeExpr.Copy()
        let prefixApp = ModificationUtil.ReplaceChild(typedLikeExpr, prefixApp.Copy())

        typedLikeExprCopy.SetExpression(exprCopy) |> ignore
        let parenExpr = ParenExprNavigator.GetByInnerExpression(addParens typedLikeExprCopy)
        prefixApp.SetArgumentExpression(parenExpr) |> ignore
