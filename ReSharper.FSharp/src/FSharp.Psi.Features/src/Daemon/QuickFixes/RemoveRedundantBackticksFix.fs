namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.CodeCleanup.HighlightingModule
open JetBrains.ReSharper.Feature.Services.Intentions.Scoped.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Resources.Shell

type RemoveRedundantBackticksFix(warning: RedundantBackticksWarning) =
    inherit ScopedQuickFixBase()

    let identifier = warning.Identifier

    override x.Text = "Remove redundant quotation"
    override x.IsAvailable _ = isValid identifier

    member x.ExecutePsiTransaction() =
        use writeCookie = WriteLockCookie.Create(identifier.IsPhysical())
        let name = identifier.GetText().RemoveBackticks()
        let newId = FSharpIdentifierToken(name)
        replace identifier newId
        null

    override x.TryGetContextTreeNode() = identifier :> _
    override x.ExecutePsiTransaction(_, _) = x.ExecutePsiTransaction()

    interface IHighlightingsCleanupItem with
        member x.IsAvailable _ = true
        member x.IsReanalysisRequired = false
        member x.IsValid() = isValid identifier
        member x.Execute() = x.ExecutePsiTransaction() |> ignore
