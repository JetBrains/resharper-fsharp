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

type TypeHighlightingVisitor(fsFile: IFSharpFile, checkResults: FSharpCheckFileResults) =
    inherit TreeNodeVisitor<IHighlightingConsumer>()

    override x.VisitNode(node, context) =
        for child in node.Children() do
            match child with
            | :? IFSharpTreeNode as treeNode -> treeNode.Accept(x, context)
            | _ -> ()

    override x.VisitBinaryAppExpr(binaryAppExpr, consumer) =
        if not (FSharpExpressionUtil.isPredefinedInfixOpApp "|>" binaryAppExpr) then () else

        let opExpr = binaryAppExpr.Operator
        match opExpr.Identifier.As<FSharpIdentifierToken>() with
        | null -> ()
        | token ->
            let (FSharpToolTipText layouts) = FSharpIdentifierTooltipProvider.GetFSharpToolTipText(checkResults, token)

            // The |> operator should have one overload and two type parameters
            match layouts with
            | [ FSharpStructuredToolTipElement.Group [ { TypeMapping = [ _; returnTypeParam ] } ] ] ->
                // TODO: do something way less hacky here
                // Trim off the: "'U is " prefix
                let text = ": " + (showL returnTypeParam).Substring(6)

                // Use EndOffsetRange to ensure the adornment appears at the end of multi-line expressions
                let range = binaryAppExpr.RightArgument.GetNavigationRange().EndOffsetRange()

                TypeHintHighlighting(RichText text, range)
                |> consumer.AddHighlighting
            | _ -> ()

        x.VisitNode(binaryAppExpr, consumer)

type TypeHintHighlightingProcess(fsFile, settings: IContextBoundSettingsStore, daemonProcess) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    let [<Literal>] opName = "TypeHintHighlightingProcess"

    override x.Execute(committer) =
        if not (settings.GetValue(fun (key: FSharpTypeHintOptions) -> key.ShowPipeReturnTypes)) then () else

        match fsFile.GetParseAndCheckResults(true, opName) with
        | None -> ()
        | Some results ->

        let consumer = FilteringHighlightingConsumer(daemonProcess.SourceFile, fsFile, settings)
        fsFile.Accept(TypeHighlightingVisitor(fsFile, results.CheckResults), consumer)
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
