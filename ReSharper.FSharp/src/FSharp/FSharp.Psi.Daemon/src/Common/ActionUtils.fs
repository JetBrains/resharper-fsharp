module JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Common.ActionUtils

open JetBrains.Application.UI.Components
open JetBrains.Application.UI.PopupLayout
open JetBrains.Application.UI.Tooltips
open JetBrains.RdBackend.Common.Features.Services
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Resources
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl.DocumentMarkup

let copyToClipboard text (highlighter: IHighlighter) =
    let shell = Shell.Instance
    shell.GetComponent<Clipboard>().SetText(text)
    shell.GetComponent<ITooltipManager>().Show(Strings.InferredTypeCodeVisionProvider_TypeCopied_TooltipText, PopupWindowContextSource(fun _ ->
        if highlighter.IsValid then RiderEditorOffsetPopupWindowContext highlighter
        else null
    ))
