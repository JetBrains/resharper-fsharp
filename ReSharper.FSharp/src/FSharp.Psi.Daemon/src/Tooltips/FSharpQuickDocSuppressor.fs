namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Tooltips

open JetBrains.DocumentModel.DataContext
open JetBrains.RdBackend.Common.Features.Daemon.Tooltips
open JetBrains.RdBackend.Common.Features.Daemon.Tooltips.Request
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.Rider.Backend.Features.QuickDoc.Impl
open JetBrains.TextControl
open JetBrains.TextControl.DataContext

//[<SolutionComponent>]
type FSharpQuickDocSuppressor(request: FSharpSyntaxHighlightingQuickDocTooltipRequest,
        quickDocManager: RiderQuickDocHighlighterManager) =
    inherit QuickDocSuppressorBase()

    override x.ShouldSuppressDocumentation context =
        let sourceFile = context.GetData(PsiDataConstants.SOURCE_FILE)
        if not (sourceFile.PrimaryPsiLanguage.Is<FSharpLanguage>()) then false else

        let textControl = context.GetData(TextControlDataConstants.TEXT_CONTROL)
        if isNull textControl then false else

        let editorContext = context.GetData(DocumentModelDataConstants.EDITOR_CONTEXT)
        let offset = if isNull editorContext then textControl.Caret.Offset() else editorContext.CaretOffset.Offset

        let highlighters = quickDocManager.TryFindHighlightersForQuickDoc(context, textControl.Document, offset)
        highlighters
        |> Seq.map (fun highlighter -> request.ShouldShowToolTipAsQuickDoc(highlighter, context))
        |> Seq.exists ((=) RiderTooltipAction.SHOW_AS_QUICK_DOC)
