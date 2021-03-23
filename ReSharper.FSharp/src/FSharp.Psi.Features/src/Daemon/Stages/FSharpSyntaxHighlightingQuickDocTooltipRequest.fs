namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open JetBrains.ProjectModel
open JetBrains.ReSharper.Host.Features.Daemon.Tooltips.Request
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.TextControl.DocumentMarkup

[<SolutionComponent>]
type FSharpSyntaxHighlightingQuickDocTooltipRequest() =
    interface IRiderQuickDocTooltipRequest with
        member x.ShouldShowToolTipAsQuickDoc(highlighter: IHighlighter, _) =
            if highlighter.UserData :? FSharpIdentifierHighlighting then
                RiderTooltipAction.SHOW_AS_QUICK_DOC
            else
                RiderTooltipAction.UNSURE

        member x.Priority = 2000
