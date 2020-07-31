namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type RemoveRedundantParenExprFix(warning: RedundantParenExprWarning) =
    inherit FSharpQuickFixBase()

    let parenExpr = warning.ParenExpr
    let innerExpr = parenExpr.InnerExpression

    override x.Text = "Remove redundant parens"

    override x.IsAvailable _ =
        isValid parenExpr && isValid innerExpr

    override x.ExecutePsiTransaction _ =
        use writeCookie = WriteLockCookie.Create(parenExpr.IsPhysical())
        use disableFormatter = new DisableCodeFormatter()

        let parenExprIndent = parenExpr.Indent
        let innerExprIndent = innerExpr.Indent
        let indentDiff = parenExprIndent - innerExprIndent

        let expr = ModificationUtil.ReplaceChild(parenExpr, innerExpr.Copy())
        shiftExpr indentDiff expr
