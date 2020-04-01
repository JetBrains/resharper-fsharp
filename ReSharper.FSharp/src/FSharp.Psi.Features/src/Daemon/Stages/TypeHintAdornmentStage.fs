namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open System
open JetBrains.Application.Settings
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Daemon.Stages
open JetBrains.ReSharper.Feature.Services.Daemon.Attributes
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages.Tooltips
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.TextControl.DocumentMarkup
open JetBrains.UI.RichText
open FSharp.Compiler.SourceCodeServices
open FSharp.Compiler.Layout

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
    inherit TreeNodeVisitor<IHighlightingConsumer>()

    let document = fsFile.GetSourceFile().Document

    let showSameLineHints =
        match sameLinePipeHints with
        | SameLinePipeHints.Show -> true
        | SameLinePipeHints.Hide -> false

    /// Formats a type parameter layout.
    /// Removes the "'T1 is " prefix from the layout string.
    let formatTypeParamLayout (layout : Layout) =
        let typeParamStr = showL layout
        let prefixToRemove = "'T1 is "
        if typeParamStr.StartsWith prefixToRemove then
            typeParamStr.Substring prefixToRemove.Length
        else
            null

    let visitBinaryAppExpr binaryAppExpr (consumer: IHighlightingConsumer) =
        if not (FSharpExpressionUtil.isPredefinedInfixOpApp "|>" binaryAppExpr) then () else

        let opExpr = binaryAppExpr.Operator
        let exprToAdorn = binaryAppExpr.LeftArgument

        let argCoords = document.GetCoordsByOffset(exprToAdorn.GetTreeEndOffset().Offset)
        let opCoords = document.GetCoordsByOffset(opExpr.GetTreeStartOffset().Offset)

        if not showSameLineHints && argCoords.Line = opCoords.Line then () else

        match opExpr.Identifier.As<FSharpIdentifierToken>() with
        | null -> ()
        | token ->

        let (FSharpToolTipText layouts) = FSharpIdentifierTooltipProvider.GetFSharpToolTipText(checkResults, token)

        // The |> operator should have one overload and two type parameters
        match layouts with
        | [ FSharpStructuredToolTipElement.Group [ { TypeMapping = [ argumentType; _ ] } ] ] ->
            let returnTypeStr = formatTypeParamLayout argumentType
            if returnTypeStr = null then () else

            // Use EndOffsetRange to ensure the adornment appears at the end of multi-line expressions
            let range = exprToAdorn.GetNavigationRange().EndOffsetRange()

            TypeHintHighlighting(RichText(": " + returnTypeStr), range)
            |> consumer.AddHighlighting
        | _ -> ()

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

    override x.Execute(committer) =
        let sameLinePipeHints =
            if settings.GetValue(fun (key: FSharpTypeHintOptions) -> key.HideSameLine) then
                SameLinePipeHints.Hide
            else
                SameLinePipeHints.Show

        if not (settings.GetValue(fun (key: FSharpTypeHintOptions) -> key.ShowPipeReturnTypes)) then
            // Clear all highlightings from this stage
            committer.Invoke(DaemonStageResult [])
        else

        match fsFile.GetParseAndCheckResults(true, opName) with
        | None -> ()
        | Some results ->

        let consumer = FilteringHighlightingConsumer(daemonProcess.SourceFile, fsFile, settings)
        fsFile.Accept(TypeHighlightingVisitor(fsFile, results.CheckResults, sameLinePipeHints), consumer)
        committer.Invoke(DaemonStageResult(consumer.Highlightings))

[<DaemonStage(StagesBefore = [| typeof<GlobalFileStructureCollectorStage> |])>]
type TypeHintAdornmentStage() =
    inherit FSharpDaemonStageBase()

    override x.IsSupported(sourceFile, processKind) =
        processKind = DaemonProcessKind.VISIBLE_DOCUMENT
        && base.IsSupported(sourceFile, processKind)
        && not (sourceFile.LanguageType.Is<FSharpSignatureProjectFileType>())

    override x.CreateStageProcess(fsFile, settings, daemonProcess) =
        TypeHintHighlightingProcess(fsFile, settings, daemonProcess) :> _
