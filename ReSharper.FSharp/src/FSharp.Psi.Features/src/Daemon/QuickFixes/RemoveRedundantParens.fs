namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Highlightings.CommonErrors
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type RemoveRedundantParens(warning: RedundantParenExpressionWarning) =
    inherit QuickFixBase()

    let [<Literal>] actionText = "Remove redundant parenthesis"

    override x.Text = actionText
    override x.IsAvailable _ = warning.ParenExpr.IsValid()

    override x.ExecutePsiTransaction(_, _) =
        let parenExpr = warning.ParenExpr
        let innerExpr = parenExpr.InnerExpression
        if isNull innerExpr then null else

        use writeLock = WriteLockCookie.Create(true)
        let innerExpr = innerExpr.Copy() // "newChild" should not be child of "oldChild", create another node
        ModificationUtil.ReplaceChild(parenExpr, innerExpr.Copy()) |> ignore

        null
