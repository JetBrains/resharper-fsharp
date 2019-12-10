namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.CommonErrors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type RemoveNeverMatchingRuleFix(warning: RuleNeverMatchedWarning) =
    inherit FSharpQuickFixBase()

    let matchClause = warning.MatchClause

    let isNotMatchClause (node: ITreeNode) =
        not (node :? IMatchClause)

    override x.Text = "Remove never matching rule"
    override x.IsAvailable _ = isValid matchClause

    override x.ExecutePsiTransaction _ =
        use writeLock = WriteLockCookie.Create(matchClause.IsPhysical())

        if isLastChild matchClause then
            let matchClauseOwner = MatchClauseListOwnerNavigator.GetByClause(matchClause)
            if isNotNull matchClauseOwner && isInlineSpaceOrComment matchClauseOwner.NextSibling then
                let first = matchClauseOwner.NextSibling
                let last = getLastMatchingNodeAfter isInlineSpaceOrComment first
                deleteChildRange first last

            let first = getFirstMatchingNodeBefore isWhitespace matchClause
            let last = getLastMatchingNodeAfter isInlineSpaceOrComment matchClause
            deleteChildRange first last

        else
            let first = matchClause
            let last = getLastMatchingNodeAfter isNotMatchClause matchClause
            deleteChildRange first last
