namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open JetBrains.ReSharper.Feature.Services.Intentions.Scoped
open JetBrains.ReSharper.Feature.Services.Intentions.Scoped.Actions
open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Resources.Shell

type RemoveUnusedOpensFix(warning: UnusedOpenWarning) =
    inherit QuickFixBase()

    let [<Literal>] actionText = "Remove unused opens"

    override x.Text = actionText
    override x.IsAvailable _ = warning.OpenStatement.IsValid()
    override x.ExecutePsiTransaction(_, _) = null

    interface IHighlightingsSetScopedAction with
        member x.ScopedText = actionText
        member x.FileCollectorInfo = FileCollectorInfo.WithoutCaretFix

        member x.ExecuteAction(highlightingInfos, _, _) =
            use writeLock = WriteLockCookie.Create(true)
            for highlightingInfo in highlightingInfos do
                match highlightingInfo.Highlighting.As<UnusedOpenWarning>() with
                | null -> ()
                | warning ->

                let openStatement = warning.OpenStatement

                let first = skipTokensOfTypeBefore FSharpTokenType.WHITESPACE openStatement
                let last =
                    openStatement
                    |> skipSemicolonsAndWhiteSpacesAfter
                    |> skipOneTokenOfTypeAfter FSharpTokenType.NEW_LINE
                    
                deleteChildRange first last

            null
