namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open System
open System.Diagnostics
open JetBrains.Application.Settings
open JetBrains.Diagnostics
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Daemon.Stages
open JetBrains.ReSharper.Feature.Services.Daemon.Attributes
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages.Tooltips
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.TextControl.DocumentMarkup
open JetBrains.UI.RichText
open FSharp.Compiler.SourceCodeServices
open FSharp.Compiler.Layout
open JetBrains.Util.Logging

[<DaemonIntraTextAdornmentProvider(typeof<TypeHintAdornmentProvider>)>]
[<StaticSeverityHighlighting(Severity.INFO,
     HighlightingGroupIds.IntraTextAdornmentsGroup,
     AttributeId = AnalysisHighlightingAttributeIds.PARAMETER_NAME_HINT,
     OverlapResolve = OverlapResolveKind.NONE,
     ShowToolTipInStatusBar = false)>]
type TypeHintHighlighting(text: RichText, range: DocumentRange) =
    interface IHighlighting with
        member x.ToolTip = null
        member x.ErrorStripeToolTip = null
        member x.IsValid() = not text.IsEmpty && not range.IsEmpty
        member x.CalculateRange() = range

    interface IHighlightingWithTestOutput with
        member x.TestOutput = text.Text

    member x.Text = text

and [<SolutionComponent>] TypeHintAdornmentProvider() =
    interface IHighlighterIntraTextAdornmentProvider with
        member x.CreateDataModel(highlighter) =
            match highlighter.UserData with
            | :? TypeHintHighlighting as thh ->
                { new IIntraTextAdornmentDataModel with
                    override x.Text = thh.Text
                    override x.HasContextMenu = false
                    override x.ContextMenuTitle = null
                    override x.ContextMenuItems = null
                    override x.IsNavigable = false
                    override x.ExecuteNavigation _ = ()
                    override x.SelectionRange = Nullable<_>()
                    override x.IconId = null
                    override x.IsPreceding = false
                }
            | _ -> null

[<RequireQualifiedAccess>]
[<Struct>]
type SameLinePipeHints =
    | Show
    | Hide

type TypeHighlightingVisitor(fsFile: IFSharpFile, checkResults: FSharpCheckFileResults, sameLinePipeHints: SameLinePipeHints) =
    inherit TreeNodeVisitor<ResizeArray<FSharpIdentifierToken * ITreeNode>>()

    let document = fsFile.GetSourceFile().Document

    let showSameLineHints =
        match sameLinePipeHints with
        | SameLinePipeHints.Show -> true
        | SameLinePipeHints.Hide -> false

    let visitBinaryAppExpr binaryAppExpr (consumer: ResizeArray<FSharpIdentifierToken * ITreeNode>) =
        if not (FSharpExpressionUtil.isPredefinedInfixOpApp "|>" binaryAppExpr) then () else

        let opExpr = binaryAppExpr.Operator
        let exprToAdorn = binaryAppExpr.LeftArgument

        let argCoords = document.GetCoordsByOffset(exprToAdorn.GetTreeEndOffset().Offset)
        let opCoords = document.GetCoordsByOffset(opExpr.GetTreeStartOffset().Offset)

        if not showSameLineHints && argCoords.Line = opCoords.Line then () else

        match opExpr.Identifier.As<FSharpIdentifierToken>() with
        | null -> ()
        | token -> consumer.Add (token, exprToAdorn :> _)

    override x.VisitNode(node, context) =
        for child in node.Children() do
            match child with
            | :? IFSharpTreeNode as treeNode -> treeNode.Accept(x, context)
            | _ -> ()

    override x.VisitBinaryAppExpr(binaryAppExpr, consumer) =
        visitBinaryAppExpr binaryAppExpr consumer
        x.VisitNode(binaryAppExpr, consumer)

type TypeHintHighlightingProcess(fsFile, settings: IContextBoundSettingsStore, daemonProcess) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    let [<Literal>] opName = "TypeHintHighlightingProcess"

    /// Formats a type parameter layout.
    /// Removes the "'T1 is " prefix from the layout string.
    let formatTypeParamLayout (layout : Layout) =
        let typeParamStr = showL layout
        let prefixToRemove = "'T1 is "
        if typeParamStr.StartsWith prefixToRemove then
            typeParamStr.Substring prefixToRemove.Length
        else
            null

    let adornExprs logKey checkResults (exprs : (FSharpIdentifierToken * ITreeNode)[]) =
        use _swc = logger.StopwatchCookie(sprintf "Adorning %s expressions" logKey, sprintf "exprCount=%d sourceFile=%s" exprs.Length daemonProcess.SourceFile.Name)
        let highlightingConsumer = FilteringHighlightingConsumer(daemonProcess.SourceFile, fsFile, settings)

        for token, exprToAdorn in exprs do
            if daemonProcess.InterruptFlag then raise <| OperationCanceledException()

            let (FSharpToolTipText layouts) = FSharpIdentifierTooltipProvider.GetFSharpToolTipText(checkResults, token)

            // The |> operator should have one overload and two type parameters
            match layouts with
            | [ FSharpStructuredToolTipElement.Group [ { TypeMapping = [ argumentType; _ ] } ] ] ->
                let returnTypeStr = formatTypeParamLayout argumentType
                if returnTypeStr = null then () else

                // Use EndOffsetRange to ensure the adornment appears at the end of multi-line expressions
                let range = exprToAdorn.GetNavigationRange().EndOffsetRange()

                TypeHintHighlighting(RichText(": " + returnTypeStr), range)
                |> highlightingConsumer.AddHighlighting
            | _ -> ()

        highlightingConsumer.Highlightings

    override x.ExecuteStage(committer) =
        let sameLinePipeHints =
            if settings.GetValue(fun (key: FSharpTypeHintOptions) -> key.HideSameLine) then
                SameLinePipeHints.Hide
            else
                SameLinePipeHints.Show

        match fsFile.GetParseAndCheckResults(true, opName) with
        | None -> ()
        | Some results ->

        let consumer = ResizeArray<_>()
        fsFile.Accept(TypeHighlightingVisitor(fsFile, results.CheckResults, sameLinePipeHints), consumer)
        let allHighlightings = Array.ofSeq consumer

        // Visible range may be larger than document range by 1 char
        // Intersect them to ensure commit doesn't throw
        let documentRange = daemonProcess.Document.GetDocumentRange()
        let visibleRange = daemonProcess.VisibleRange.Intersect &documentRange

        let remainingHighlightings =
            if visibleRange.IsValid() then
                // Partition the expressions to adorn by whether they're visible in the viewport or not
                let visible, notVisible =
                    allHighlightings
                    |> Array.partition (fun (token, exprToAdorn) ->
                        exprToAdorn.GetNavigationRange().IntersectsOrContacts &visibleRange
                    )

                // Adorn visible expressions first
                let visibleHighlightings = adornExprs "visible" results.CheckResults visible
                committer.Invoke(DaemonStageResult(visibleHighlightings, visibleRange))

                // Finally adorn expressions that aren't visible in the viewport
                adornExprs "not visible" results.CheckResults notVisible
            else
                adornExprs "all" results.CheckResults allHighlightings

        committer.Invoke(DaemonStageResult remainingHighlightings)

[<DaemonStage(StagesBefore = [| typeof<GlobalFileStructureCollectorStage> |])>]
type TypeHintAdornmentStage() =
    inherit FSharpDaemonStageBase()
type TypeHintAdornmentStage(logger: ILogger) =
    inherit FSharpDaemonStageBase(logger)

    override x.IsSupported(sourceFile, processKind) =
        processKind = DaemonProcessKind.VISIBLE_DOCUMENT
        && base.IsSupported(sourceFile, processKind)
        && not (sourceFile.LanguageType.Is<FSharpSignatureProjectFileType>())

    override x.CreateStageProcess(fsFile, settings, daemonProcess) =
        if not (settings.GetValue(fun (key: FSharpTypeHintOptions) -> key.ShowPipeReturnTypes)) then null else
        TypeHintHighlightingProcess(logger, fsFile, settings, daemonProcess) :> _
