namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.CommonErrors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Resources.Shell

type RemoveNeverMatchingRuleFix(warning: RuleNeverMatchedWarning) =
    inherit FSharpQuickFixBase()

    let matchClause = warning.MatchClause

    override x.Text = "Remove never matching rule"
    override x.IsAvailable _ = isValid matchClause

    override x.ExecutePsiTransaction _ =
        use writeLock = WriteLockCookie.Create(matchClause.IsPhysical())

        if isLastChild matchClause then
            let matchExpr = MatchExprNavigator.GetByClause(matchClause)
            if isInlineSpaceOrComment matchExpr.NextSibling then
                let first = matchExpr.NextSibling
                let last = getLastMatchingNodeAfter isInlineSpaceOrComment first
                deleteChildRange first last

        let first = getFirstMatchingNodeBefore isWhitespace matchClause
        let last = getLastMatchingNodeAfter isInlineSpaceOrComment matchClause
        deleteChildRange first last
