namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings.CommonErrors
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type RemoveNeverMatchingRuleFix(warning: RuleNeverMatchedWarning) =
    inherit FSharpScopedQuickFixBase()

    let matchClause = warning.MatchClause

    override x.Text = "Remove never matching rule"
    override x.IsAvailable _ = isValid matchClause

    override x.TryGetContextTreeNode() = matchClause :> _

    override x.ExecutePsiTransaction _ =
        use writeLock = WriteLockCookie.Create(matchClause.IsPhysical())
        use enableFormatter = FSharpRegistryUtil.AllowFormatterCookie.Create()

        if isLastChild matchClause then
            let matchClauseOwner = MatchClauseListOwnerNavigator.GetByClause(matchClause)
            if isNotNull matchClauseOwner && isInlineSpaceOrComment matchClauseOwner.NextSibling then
                let first = matchClauseOwner.NextSibling
                let last = getLastMatchingNodeAfter isInlineSpaceOrComment first
                deleteChildRange first last

        let last = getLastMatchingNodeAfter isInlineSpaceOrComment matchClause
        deleteChildRange matchClause last
