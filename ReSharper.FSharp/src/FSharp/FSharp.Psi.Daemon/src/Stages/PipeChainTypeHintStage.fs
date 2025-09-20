namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Stages

open System
open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.Application.Parts
open JetBrains.Application.Settings
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Utils.VisibleRangeContainer
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Util.FSharpPredefinedType
open JetBrains.ReSharper.Plugins.FSharp.Util.FSharpSymbolUtil
open JetBrains.ReSharper.Psi.Tree

[<RequireQualifiedAccess>]
[<Struct>]
type SameLinePipeHints =
    | Show
    | Hide

type PipeOperatorVisitor(sameLinePipeHints: SameLinePipeHints) =
    inherit TreeNodeVisitor<VisibilityConsumer<IReferenceExpr * ITreeNode * bool>>()

    let showSameLineHints =
        match sameLinePipeHints with
        | SameLinePipeHints.Show -> true
        | SameLinePipeHints.Hide -> false

    let isApplicable binaryAppExpr =
        isPredefinedInfixOpApp "|>" binaryAppExpr

    let visitBinaryAppExpr binaryAppExpr (context: VisibilityConsumer<IReferenceExpr * ITreeNode * bool>) =
        if not (isApplicable binaryAppExpr) then () else

        let opExpr = binaryAppExpr.Operator

        let leftArgument = binaryAppExpr.LeftArgument
        let rightArgument = binaryAppExpr.RightArgument

        if isNull leftArgument then () else

        if showSameLineHints || leftArgument.EndLine <> opExpr.StartLine then
            context.Add(opExpr, leftArgument, true)

        if isNull rightArgument || rightArgument :? IFromErrorExpr then () else

        let isTopBinary =
            let outermostParent = binaryAppExpr.GetOutermostParentExpressionFromItsReturn(true)

            let binaryAppExpr =
                outermostParent
                |> BinaryAppExprNavigator.GetByArgument

            if isNotNull binaryAppExpr then not (isApplicable binaryAppExpr) else

            let binaryAppExpr =
                PrefixAppExprNavigator.GetByArgumentExpression(outermostParent)
                |> BinaryAppExprNavigator.GetByArgument

            isNull binaryAppExpr || not (isApplicable binaryAppExpr)

        if not isTopBinary then () else

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

type PipeChainHighlightingProcess(fsFile, settings: IContextBoundSettingsStore, daemonProcess: IDaemonProcess) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    let isRightPipeArgApplicable (expr: ITreeNode) (pipeResultType: FSharpType) =
        let rec skipFunArgs (t: FSharpType) =
            if t.IsFunctionType then skipFunArgs t.GenericArguments[1] else t

        let expr = expr.As<IFSharpExpression>().IgnoreInnerParens()
        let exprType = expr.TryGetFcsType()
        if isNull exprType then false else

        match expr with
        | :? IDotLambdaExpr -> skipFunArgs exprType = pipeResultType
        | _ ->
        if not exprType.IsFunctionType || exprType.GenericArguments[1] <> pipeResultType then false else
        if not (isUnit pipeResultType) then true else

        let invokedRefExpr =
            match expr with
            | :? IReferenceExpr as refExpr ->
                refExpr
            | :? IPrefixAppExpr as expr ->
                expr.InvokedReferenceExpression
            | _ -> null

        if isNull invokedRefExpr then true else
        let returnType = invokedRefExpr.Reference.GetFcsSymbol() |> getReturnType
        match returnType with
        | Some t ->
            // We know that the result of the pipe is unit.
            // We want to get the very last returned value and make sure that it is not generic, but strictly unit.
            skipFunArgs t
            |> isUnit
            |> not
        | None -> false

    let adornExprs (exprs : (IReferenceExpr * ITreeNode * bool) ICollection) =
        let highlightingConsumer = FilteringHighlightingConsumer(daemonProcess.SourceFile, fsFile, settings)

        for refExpr, exprToAdorn, isLeft in exprs do
            if daemonProcess.InterruptFlag then raise <| OperationCanceledException()

            let symbolUse = refExpr.Reference.GetSymbolUse()
            if isNull symbolUse then () else

            let _, fcsType = symbolUse.GenericArguments[if isLeft then 0 else 1]
            let isApplicable = isLeft || isRightPipeArgApplicable exprToAdorn fcsType

            if not isApplicable then () else
            let range = exprToAdorn.GetNavigationRange().EndOffsetRange()
            highlightingConsumer.AddHighlighting(TypeHintHighlighting(fcsType.Format(), range))

        highlightingConsumer.CollectHighlightings()

    override x.Execute(committer) =
        let sameLinePipeHints =
            if settings.GetValue(fun (key: FSharpTypeHintOptions) -> key.HideSameLine) then
                SameLinePipeHints.Hide
            else
                SameLinePipeHints.Show

        // Visible range may be larger than document range by 1 char
        // Intersect them to ensure commit doesn't throw
        let documentRange = daemonProcess.Document.GetDocumentRange()
        let visibleRange = daemonProcess.VisibleRange.Intersect(&documentRange)
        let consumer =
            VisibilityConsumer(visibleRange, fun (_, exprToAdorn: ITreeNode, _) -> exprToAdorn.GetNavigationRange())

        fsFile.Accept(PipeOperatorVisitor(sameLinePipeHints), consumer)

        let remainingHighlightings =
            if consumer.HasVisibleItems then
                // Partition the expressions to adorn by whether they're visible in the viewport or not
                // Adorn visible expressions first
                let visibleHighlightings = adornExprs consumer.VisibleItems
                committer.Invoke(DaemonStageResult(visibleHighlightings, visibleRange))

            // Finally adorn expressions that aren't visible in the viewport
            adornExprs consumer.NonVisibleItems

        committer.Invoke(DaemonStageResult remainingHighlightings)

[<DaemonStage(Instantiation.DemandAnyThreadSafe, StagesBefore = [| typeof<GlobalFileStructureCollectorStage> |])>]
type PipeChainTypeHintStage() =
    inherit FSharpDaemonStageBase(true, false)

    override x.CreateStageProcess(fsFile, settings, daemonProcess, _) =
        if not (settings.GetValue(fun (key: FSharpTypeHintOptions) -> key.ShowPipeReturnTypes)) then null else
        PipeChainHighlightingProcess(fsFile, settings, daemonProcess) :> _
