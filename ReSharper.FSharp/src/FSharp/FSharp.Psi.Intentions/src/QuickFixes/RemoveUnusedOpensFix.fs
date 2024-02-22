namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.Intentions.Scoped
open JetBrains.ReSharper.Feature.Services.QuickFixes.Scoped
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util

type RemoveUnusedOpensFix(warning: UnusedOpenWarning) =
    inherit FSharpScopedQuickFixBase(warning.OpenStatement)

    let [<Literal>] actionText = "Remove unused opens"

    override x.Text = actionText
    override x.FileCollectorInfo = FileCollectorInfo.WithoutCaretFix
    override x.IsAvailable _ = warning.OpenStatement.IsValid()
    override x.ExecutePsiTransaction(_, _) =
        removeOpen warning.OpenStatement
        null

    override x.GetScopedQuickFixExecutor(solution, fixingStrategy, highlighting, languageType) =
        ScopedNonIncrementalQuickFixExecutor(solution, fixingStrategy, highlighting.GetType(), languageType)