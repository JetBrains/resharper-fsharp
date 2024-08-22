namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.Intentions.Scoped
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util

type RemoveUnusedOpensFix(warning: UnusedOpenWarning) =
    inherit FSharpScopedNonIncrementalQuickFixBase(warning.OpenStatement)

    let [<Literal>] actionText = "Remove unused opens"

    override x.Text = actionText

    override x.FileCollectorInfo = FileCollectorInfo.WithoutCaretFix

    override this.IsReanalysisRequired = false
    override this.ReanalysisDependencyRoot = null

    override x.IsAvailable _ = warning.OpenStatement.IsValid()
    override x.ExecutePsiTransaction(_, _) =
        removeOpen warning.OpenStatement
        null
