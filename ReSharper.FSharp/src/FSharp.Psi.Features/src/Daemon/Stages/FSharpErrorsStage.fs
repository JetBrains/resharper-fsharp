namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open System
open System.Collections.Generic
open JetBrains.ReSharper.Daemon.Stages.Dispatcher
open JetBrains.ReSharper.Daemon.VisualElements
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

module FSharpErrorsStage =
    let visualElementFactoryKey = Key<VisualElementHighlighter>("ColorUsageHighlightingEnabled")


[<DaemonStage(StagesBefore = [| typeof<HighlightIdentifiersStage> |])>]
type FSharpErrorsStage(elementProblemAnalyzerRegistrar) =
    inherit FSharpDaemonStageBase()

    override x.CreateStageProcess(fsFile, settings, daemonProcess) =
        FSharpErrorStageProcess(fsFile, daemonProcess, settings, elementProblemAnalyzerRegistrar) :> _


and FSharpErrorStageProcess(fsFile, daemonProcess, settings, analyzerRegistrar: ElementProblemAnalyzerRegistrar) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    static let analyzerRunKind = ElementProblemAnalyzerRunKind.FullDaemon

    let interruptCheck = daemonProcess.GetCheckForInterrupt()
    let elementProblemAnalyzerData = ElementProblemAnalyzerData(fsFile, settings, analyzerRunKind, interruptCheck)
    let analyzerDispatcher = analyzerRegistrar.CreateDispatcher(elementProblemAnalyzerData)

    do
        let visualElementHighlighter = VisualElementHighlighter(fsFile.Language, settings)
        elementProblemAnalyzerData.PutData(FSharpErrorsStage.visualElementFactoryKey, visualElementHighlighter)

    override x.VisitNode(element: ITreeNode, consumer: IHighlightingConsumer) =
        analyzerDispatcher.Run(element, consumer)

    override x.Execute(committer) =
        let consumer = FilteringHighlightingConsumer(daemonProcess.SourceFile, fsFile, settings)
        let globalProcessor = GlobalProcessor(x, consumer)
        fsFile.ProcessThisAndDescendants(globalProcessor)

        let fibers = daemonProcess.CreateFibers()
        for node in globalProcessor.MemberDeclarations do
            fibers.EnqueueJob((fun _ -> node.ProcessThisAndDescendants(Processor(x, consumer))), x.ResolveContext)
        fibers.Dispose()

        committer.Invoke(DaemonStageResult(consumer.Highlightings))


and private Processor(daemonProcess: FSharpDaemonStageProcessBase, consumer: IHighlightingConsumer) =
    abstract InteriorShouldBeProcessed: ITreeNode -> bool
    default x.InteriorShouldBeProcessed _ = true

    abstract ProcessAfterInterior: ITreeNode -> unit
    default x.ProcessAfterInterior(element) =
        match element with
        | :? FSharpToken as token ->
            if not (token.GetTokenType().IsWhitespace) then
                token.Accept(daemonProcess, consumer)

        | :? IFSharpTreeNode as fsTreeNode ->
            fsTreeNode.Accept(daemonProcess, consumer)

        | _ -> failwithf "Unreachable: %O" element

    interface IRecursiveElementProcessor with
        member x.InteriorShouldBeProcessed(element) =
            x.InteriorShouldBeProcessed(element)

        member x.ProcessAfterInterior(element) = x.ProcessAfterInterior(element)
        member x.ProcessBeforeInterior _ = ()

        member x.ProcessingIsFinished =
            match daemonProcess.DaemonProcess.InterruptFlag with
            | true -> OperationCanceledException() |> raise
            | _ -> false

and private GlobalProcessor(daemonProcessor, consumer) =
    inherit Processor(daemonProcessor, consumer)

    let shouldProcess (node: ITreeNode) =
        not (node :? IBinding || node :? IMemberDeclaration || node :? IDo)

    member val MemberDeclarations: JetHashSet<ITreeNode> = JetHashSet()

    override x.InteriorShouldBeProcessed(node) = shouldProcess node

    override x.ProcessAfterInterior(node) =
        if shouldProcess node then
            base.ProcessAfterInterior(node)
        else
            x.MemberDeclarations.Add(node) |> ignore
