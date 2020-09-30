namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

type RemoveRedundantParenExprFix(warning: RedundantParenExprWarning) =
    inherit ReplaceWithInnerExpressionFixBase(warning.ParenExpr, warning.ParenExpr.InnerExpression)

    override x.Text = "Remove redundant parens"
