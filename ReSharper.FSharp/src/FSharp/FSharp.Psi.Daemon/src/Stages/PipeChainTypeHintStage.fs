namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Stages

open System
open System.Collections.Generic
open JetBrains.Application.Settings
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util.Logging

[<RequireQualifiedAccess>]
[<Struct>]
type SameLinePipeHints =
    | Show
    | Hide

type PipeOperatorVisitor(sameLinePipeHints: SameLinePipeHints) =
    inherit TreeNodeVisitor<List<IReferenceExpr * ITreeNode * bool>>()

    let showSameLineHints =
        match sameLinePipeHints with
        | SameLinePipeHints.Show -> true
        | SameLinePipeHints.Hide -> false

    let visitBinaryAppExpr binaryAppExpr (context: List<IReferenceExpr * ITreeNode * bool>) =
        if not (isPredefinedInfixOpApp "|>" binaryAppExpr) then () else

        let opExpr = binaryAppExpr.Operator

        let leftArgument = binaryAppExpr.LeftArgument
        let rightArgument = binaryAppExpr.RightArgument

        if isNull leftArgument then () else

        if showSameLineHints || leftArgument.EndLine <> opExpr.StartLine then
            context.Add(opExpr, leftArgument, true)

        if isNull rightArgument || rightArgument :? IFromErrorExpr then () else

        let isTopPipe =
            let binaryAppExpr = BinaryAppExprNavigator.GetByLeftArgument(binaryAppExpr.IgnoreParentParens())
            isNull binaryAppExpr || not (isPredefinedInfixOpApp "|>" binaryAppExpr)

        if not isTopPipe then () else

        if showSameLineHints || leftArgument.EndLine <> rightArgument.StartLine then
            context.Add(opExpr, rightArgument, false)

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

    let adornExprs logKey (exprs : (IReferenceExpr * ITreeNode * bool)[]) =
        use _swc = logger.StopwatchCookie(sprintf "Adorning %s expressions" logKey, sprintf "exprCount=%d sourceFile=%s" exprs.Length daemonProcess.SourceFile.Name)
        let highlightingConsumer = FilteringHighlightingConsumer(daemonProcess.SourceFile, fsFile, settings)

        for refExpr, exprToAdorn, isLeft in exprs do
            if daemonProcess.InterruptFlag then raise <| OperationCanceledException()

            let symbolUse = refExpr.Reference.GetSymbolUse()
            if isNull symbolUse then () else

            let _, fcsType = symbolUse.GenericArguments[if isLeft then 0 else 1]
            let range = exprToAdorn.GetNavigationRange().EndOffsetRange()
            highlightingConsumer.AddHighlighting(TypeHintHighlighting(fcsType.Format(symbolUse.DisplayContext), range))

        highlightingConsumer.CollectHighlightings()

    override x.Execute(committer) =
        let sameLinePipeHints =
            if settings.GetValue(fun (key: FSharpTypeHintOptions) -> key.HideSameLine) then
                SameLinePipeHints.Hide
            else
                SameLinePipeHints.Show

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
                    |> Array.partition (fun (_, exprToAdorn, _) ->
                        exprToAdorn.GetNavigationRange().IntersectsOrContacts(&visibleRange)
                    )

                // Adorn visible expressions first
                let visibleHighlightings = adornExprs "visible" visible
                committer.Invoke(DaemonStageResult(visibleHighlightings, visibleRange))

                // Finally adorn expressions that aren't visible in the viewport
                adornExprs "not visible" notVisible
            else
                adornExprs "all" allHighlightings

        committer.Invoke(DaemonStageResult remainingHighlightings)

[<DaemonStage(StagesBefore = [| typeof<GlobalFileStructureCollectorStage> |])>]
type PipeChainTypeHintStage(logger: ILogger) =
    inherit FSharpDaemonStageBase()

    override x.IsSupported(sourceFile, processKind) =
        processKind = DaemonProcessKind.VISIBLE_DOCUMENT &&
        base.IsSupported(sourceFile, processKind) &&
        not (sourceFile.LanguageType.Is<FSharpSignatureProjectFileType>())

    override x.CreateStageProcess(fsFile, settings, daemonProcess, _) =
        if not (settings.GetValue(fun (key: FSharpTypeHintOptions) -> key.ShowPipeReturnTypes)) then null else
        PipeChainHighlightingProcess(logger, fsFile, settings, daemonProcess) :> _
