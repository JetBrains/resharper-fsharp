namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

[<AbstractClass>]
type ReplaceWithInnerExpressionFixBase(parentExpr: IFSharpExpression, innerExpr: IFSharpExpression) =
    inherit FSharpQuickFixBase()

    override x.IsAvailable _ =
        isValid parentExpr && isValid innerExpr

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(parentExpr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let parenExprIndent = parentExpr.Indent
        let innerExprIndent = innerExpr.Indent
        let indentDiff = parenExprIndent - innerExprIndent

        let expr = ModificationUtil.ReplaceChild(parentExpr, innerExpr.Copy())
        shiftExpr indentDiff expr
