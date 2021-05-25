namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open JetBrains.DocumentModel.DataContext
open JetBrains.ProjectModel
open JetBrains.RdBackend.Common.Features.Daemon.Tooltips
open JetBrains.RdBackend.Common.Features.Daemon.Tooltips.Request
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.Rider.Backend.Features.QuickDoc.Impl
open JetBrains.TextControl
open JetBrains.TextControl.DataContext

[<SolutionComponent>]
type FSharpQuickDocSuppressor
    (
        request: FSharpSyntaxHighlightingQuickDocTooltipRequest,
        quickDocHighlighterManager: RiderQuickDocHighlighterManager
    ) =
    inherit QuickDocSuppressorBase()
    with
        override x.ShouldSuppressDocumentation context =
            let primaryPsiLanguage = context.GetData(PsiDataConstants.SOURCE_FILE).PrimaryPsiLanguage
            if not (FSharpLanguage.Instance.Equals(primaryPsiLanguage)) then false else
            let editorContext = context.GetData(DocumentModelDataConstants.EDITOR_CONTEXT)
            let textControl = context.GetData(TextControlDataConstants.TEXT_CONTROL)
            if textControl == null then false else
            let offset = if editorContext == null then textControl.Caret.Offset() else editorContext.CaretOffset.Offset
            let document = textControl.Document
            let highlighters = quickDocHighlighterManager.TryFindHighlightersForQuickDoc(context, document, offset)
            let requestInterface = request :> IRiderQuickDocTooltipRequest
            highlighters
                .Select(fun highlighter -> properlyTypedRequest.ShouldShowToolTipAsQuickDoc(highlighter, context))
                .Any(fun action -> action == RiderTooltipAction.SHOW_AS_QUICK_DOC)
