namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Util

type RemoveRedundantParenExprFix(warning: RedundantParenExprWarning) =
    inherit ReplaceWithInnerTreeNodeFixBase(warning.ParenExpr, warning.ParenExpr.InnerExpression)

    override x.Text = "Remove redundant parens"

type RemoveRedundantParenTypeUsageFix(warning: RedundantParenTypeUsageWarning) =
    inherit ReplaceWithInnerTreeNodeFixBase(warning.ParenTypeUsage, warning.ParenTypeUsage.InnerTypeUsage)

    override x.Text = "Remove redundant parens"

    override this.AddSpaceAfter(prevToken) =
        getTokenType prevToken != FSharpTokenType.LESS && base.AddSpaceAfter(prevToken)

    override this.AddSpaceBefore(nextToken) =
        getTokenType nextToken != FSharpTokenType.GREATER && base.AddSpaceAfter(nextToken)

type RemoveRedundantParenPatFix(warning: RedundantParenPatWarning) =
    inherit ReplaceWithInnerTreeNodeFixBase(warning.ParenPat, warning.ParenPat.Pattern)

    override x.Text = "Remove redundant parens"
