namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open System
open System.Collections.Generic
open FSharp.Compiler.Layout
open FSharp.Compiler.SourceCodeServices
open JetBrains.Application.Settings
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Daemon.Stages
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages.Tooltips
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util.Logging

[<RequireQualifiedAccess>]
[<Struct>]
type SameLinePipeHints =
    | Show
    | Hide

type PipeOperatorVisitor(sameLinePipeHints: SameLinePipeHints) =
    inherit TreeNodeVisitor<List<FSharpIdentifierToken * ITreeNode>>()

    let showSameLineHints =
        match sameLinePipeHints with
        | SameLinePipeHints.Show -> true
        | SameLinePipeHints.Hide -> false

    let visitBinaryAppExpr binaryAppExpr (context: List<FSharpIdentifierToken * ITreeNode>) =
        if not (isPredefinedInfixOpApp "|>" binaryAppExpr) then () else

        match binaryAppExpr.LeftArgument with
        | null -> ()
        | exprToAdorn ->

        let opExpr = binaryAppExpr.Operator
        if not showSameLineHints && exprToAdorn.EndLine = opExpr.StartLine then () else

        match opExpr.Identifier.As<FSharpIdentifierToken>() with
        | null -> ()
        | token -> context.Add(token, exprToAdorn :> _)

    override x.VisitNode(node, context) =
        for child in node.Children() do
            match child with
            | :? IFSharpTreeNode as treeNode -> treeNode.Accept(x, context)
            | _ -> ()

    override x.VisitBinaryAppExpr(binaryAppExpr, context) =
        visitBinaryAppExpr binaryAppExpr context
        x.VisitNode(binaryAppExpr, context)

type PipeChainHighlightingProcess(logger: ILogger, fsFile, settings: IContextBoundSettingsStore, daemonProcess: IDaemonProcess) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    let [<Literal>] opName = "PipeChainHighlightingProcess"

    /// Formats a type parameter layout.
    /// Removes the "'T1 is " prefix from the layout string.
    let formatTypeParamLayout (layout : Layout) =
        let typeParamStr = showL layout
        let prefixToRemove = "'T1 is "
        if typeParamStr.StartsWith(prefixToRemove) then
            typeParamStr.Substring(prefixToRemove.Length)
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

                highlightingConsumer.AddHighlighting(TypeHintHighlighting(returnTypeStr, range))
            | _ -> ()

        highlightingConsumer.Highlightings

    override x.Execute(committer) =
        let sameLinePipeHints =
            if settings.GetValue(fun (key: FSharpTypeHintOptions) -> key.HideSameLine) then
                SameLinePipeHints.Hide
            else
                SameLinePipeHints.Show

        match fsFile.GetParseAndCheckResults(true, opName) with
        | None -> ()
        | Some results ->

        let consumer = List()
        fsFile.Accept(PipeOperatorVisitor(sameLinePipeHints), consumer)
        let allHighlightings = Array.ofSeq consumer

        // Visible range may be larger than document range by 1 char
        // Intersect them to ensure commit doesn't throw
        let documentRange = daemonProcess.Document.GetDocumentRange()
        let visibleRange = daemonProcess.VisibleRange.Intersect(&documentRange)

        let remainingHighlightings =
            if visibleRange.IsValid() then
                // Partition the expressions to adorn by whether they're visible in the viewport or not
                let visible, notVisible =
                    allHighlightings
                    |> Array.partition (fun (_, exprToAdorn) ->
                        exprToAdorn.GetNavigationRange().IntersectsOrContacts(&visibleRange)
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
type PipeChainTypeHintStage(logger: ILogger) =
    inherit FSharpDaemonStageBase()

    override x.IsSupported(sourceFile, processKind) =
        processKind = DaemonProcessKind.VISIBLE_DOCUMENT &&
        base.IsSupported(sourceFile, processKind) &&
        not (sourceFile.LanguageType.Is<FSharpSignatureProjectFileType>())

    override x.CreateStageProcess(fsFile, settings, daemonProcess) =
        if not (settings.GetValue(fun (key: FSharpTypeHintOptions) -> key.ShowPipeReturnTypes)) then null else
        PipeChainHighlightingProcess(logger, fsFile, settings, daemonProcess) :> _
