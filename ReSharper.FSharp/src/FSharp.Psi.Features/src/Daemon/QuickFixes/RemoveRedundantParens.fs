namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Highlightings.CommonErrors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type RemoveRedundantParens(warning: RedundantParenExpressionWarning) =
    inherit QuickFixBase()

    let [<Literal>] actionText = "Remove redundant parenthesis"

    override x.Text = actionText
    override x.IsAvailable _ = warning.ParenExpr.IsValid()

    override x.ExecutePsiTransaction(_, _) =
        let parenExpr = warning.ParenExpr
        match parenExpr.Parent.As<IParenExpr>() with
        | null -> null
        | parent ->

        use writeLock = WriteLockCookie.Create(true)        
        parent.SetInnerExpression(parenExpr.InnerExpression) |> ignore

        null
