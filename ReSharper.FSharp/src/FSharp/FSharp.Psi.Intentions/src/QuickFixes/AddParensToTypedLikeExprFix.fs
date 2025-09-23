namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
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
        let typedLikeExpr = TypedLikeExprNavigator.GetByExpression(error.Expr)
        AddParensToTypedLikeExprFix typedLikeExpr

    override x.Text =
        $"Add parens to {getExprPresentableName typedLikeExpr}"

    override x.IsAvailable _ = isValid expr

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(expr.IsPhysical())
        let factory = expr.CreateElementFactory()

        let exprCopy = expr.Copy()
        let typedLikeExprCopy = typedLikeExpr.Copy()

        let prefixApp = ModificationUtil.ReplaceChild(typedLikeExpr, prefixApp.Copy())
        let parenExpr = prefixApp.SetArgumentExpression(factory.CreateParenExpr()) :?> IParenExpr
        let typedLikeExprCopy = parenExpr.SetInnerExpression(typedLikeExprCopy) :?> ITypedLikeExpr
        typedLikeExprCopy.SetExpression(exprCopy) |> ignore
