namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

type RemoveRedundantParenExprFix(warning: RedundantParenExprWarning) =
    inherit ReplaceWithInnerTreeNodeFixBase(warning.ParenExpr, warning.ParenExpr.InnerExpression)

    override x.Text = "Remove redundant parens"

type RemoveRedundantParenTypeUsageFix(warning: RedundantParenTypeUsageWarning) =
    inherit ReplaceWithInnerTreeNodeFixBase(warning.ParenTypeUsage, warning.ParenTypeUsage.InnerTypeUsage)

    override x.Text = "Remove redundant parens"

type RemoveRedundantParenPatFix(warning: RedundantParenPatWarning) =
    inherit ReplaceWithInnerTreeNodeFixBase(warning.ParenPat, warning.ParenPat.Pattern)

    override x.Text = "Remove redundant parens"
