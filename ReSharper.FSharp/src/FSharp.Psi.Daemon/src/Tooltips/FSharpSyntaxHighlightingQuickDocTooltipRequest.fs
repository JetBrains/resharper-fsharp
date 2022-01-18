namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Tooltips

open JetBrains.ProjectModel
open JetBrains.RdBackend.Common.Features.Daemon.Tooltips.Request
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.TextControl.DocumentMarkup

[<SolutionComponent>]
type FSharpSyntaxHighlightingQuickDocTooltipRequest() =
    member x.ShouldShowToolTipAsQuickDoc(highlighter: IHighlighter, _) =
        if highlighter.UserData :? FSharpIdentifierHighlighting then
            RiderTooltipAction.SHOW_AS_QUICK_DOC
        else
            RiderTooltipAction.UNSURE

    interface IRiderQuickDocTooltipRequest with
        member x.ShouldShowToolTipAsQuickDoc(highlighter: IHighlighter, dataContext) =
            x.ShouldShowToolTipAsQuickDoc(highlighter, dataContext)

        member x.Priority = 2000
