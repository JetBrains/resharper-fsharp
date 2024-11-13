module JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Common.ActionUtils

open JetBrains.Application.UI.Components
open JetBrains.Application.UI.PopupLayout
open JetBrains.Application.UI.Tooltips
open JetBrains.RdBackend.Common.Features.Services
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Resources
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl.DocumentMarkup

let copyToClipboard text (highlighting: IHighlighting) highlighterKey =
    let shell = Shell.Instance
    shell.GetComponent<Clipboard>().SetText(text)
    let documentMarkupManager = shell.GetComponent<IDocumentMarkupManager>();
    shell.GetComponent<ITooltipManager>().Show(Strings.InferredTypeCodeVisionProvider_TypeCopied_TooltipText, PopupWindowContextSource(fun _ ->
        let documentMarkup = documentMarkupManager.TryGetMarkupModel(highlighting.CalculateRange().Document)
        if isNull documentMarkup then null else

        documentMarkup.GetFilteredHighlighters(highlighterKey, fun h -> highlighting.Equals(h.GetHighlighting()))
        |> Seq.tryHead
        |> Option.map RiderEditorOffsetPopupWindowContext
        |> Option.defaultValue null :> _
    ))
