namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.QuickFixes.Scoped
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

type RemoveNeverMatchingRuleFix(warning: RuleNeverMatchedWarning) =
    inherit FSharpScopedQuickFixBase(warning.MatchClause)

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
    override x.ScopedText = "Remove never matching rules"
    override x.IsAvailable _ = isValid warning.MatchClause

    override x.ExecutePsiTransaction _ =
        let clause = warning.MatchClause
        use enableFormatter = FSharpExperimentalFeatureCookie.Create(ExperimentalFeature.Formatter)
        use writeLock = WriteLockCookie.Create(clause.IsPhysical())
        removeMatchClause clause

    override x.GetScopedQuickFixExecutor(solution, fixingStrategy, highlighting, languageType) =
        ScopedNonIncrementalQuickFixExecutor(solution, fixingStrategy, highlighting.GetType(), languageType)