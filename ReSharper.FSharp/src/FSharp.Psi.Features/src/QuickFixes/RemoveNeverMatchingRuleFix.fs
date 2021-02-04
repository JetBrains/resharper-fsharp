namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.Intentions.Scoped
open JetBrains.ReSharper.Feature.Services.Intentions.Scoped.Actions
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type RemoveNeverMatchingRuleFix(warning: RuleNeverMatchedWarning) =
    inherit FSharpQuickFixBase()

    let removeMatchClause (clause : IMatchClause) =
        if isLastChild clause then
            let matchClauseOwner = MatchClauseListOwnerExprNavigator.GetByClause(clause)
            if isNotNull matchClauseOwner && isInlineSpaceOrComment matchClauseOwner.NextSibling then
                let first = matchClauseOwner.NextSibling
                let last = getLastMatchingNodeAfter isInlineSpaceOrComment first
                deleteChildRange first last

        let last = getLastMatchingNodeAfter isInlineSpaceOrComment clause
        deleteChildRange clause last

    override x.Text = "Remove never matching rule"
    override x.IsAvailable _ = isValid warning.MatchClause

    override x.ExecutePsiTransaction _ =
        let clause = warning.MatchClause
        use enableFormatter = FSharpExperimentalFeatures.EnableFormatterCookie.Create()
        use writeLock = WriteLockCookie.Create(clause.IsPhysical())
        removeMatchClause clause

    interface IHighlightingsSetScopedAction with
        member x.ScopedText = "Remove never matching rules"
        member x.FileCollectorInfo = FileCollectorInfo.Default

        member x.ExecuteAction(highlightingInfos, _, _) =
            use enableFormatter = FSharpExperimentalFeatures.EnableFormatterCookie.Create()

            for highlightingInfo in highlightingInfos do
                match highlightingInfo.Highlighting.As<RuleNeverMatchedWarning>() with
                | null -> ()
                | warning ->
                    let clause = warning.MatchClause
                    use writeLock = WriteLockCookie.Create(clause.IsPhysical())
                    removeMatchClause clause
            null
