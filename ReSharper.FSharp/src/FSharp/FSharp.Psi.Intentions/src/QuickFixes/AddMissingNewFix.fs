namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Resources.Shell

type AddMissingNewFix(warning: IDisposableTypeShouldUseNewWarning) =
    inherit FSharpQuickFixBase()

    let appExpr = warning.Expr.As<IPrefixAppExpr>()

    override x.Text = "Add missing 'new'"

    override x.IsAvailable _ = isValid appExpr

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(appExpr.IsPhysical())

        let factory = appExpr.CreateElementFactory()
        let typeName = appExpr.FunctionExpression.GetText()

        let newExpr = factory.CreateExpr($"new {typeName}()") :?> INewExpr
        let argExpr = ModificationUtil.ReplaceChild(newExpr.ArgumentExpression, appExpr.ArgumentExpression)
        if not (isParenOrUnitExpr argExpr) then
            addParens argExpr |> ignore

        let newExpr = ModificationUtil.ReplaceChild(appExpr, newExpr)
        addParensIfNeeded newExpr |> ignore
