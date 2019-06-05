namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open System
open JetBrains.Application.Settings
open JetBrains.ReSharper.Daemon.Stages.Dispatcher
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree

[<DaemonStage(StagesBefore = [| typeof<HighlightIdentifiersStage> |])>]
type FSharpErrorsStage(elementProblemAnalyzerRegistrar) =
    inherit FSharpDaemonStageBase()

    override x.CreateStageProcess(fsFile, settings, daemonProcess) =
        FSharpErrorStageProcess(fsFile, daemonProcess, settings, elementProblemAnalyzerRegistrar) :> IDaemonStageProcess


and FSharpErrorStageProcess
        (fsFile: IFSharpFile, daemonProcess: IDaemonProcess, settings: IContextBoundSettingsStore,
         analyzerRegistrar: ElementProblemAnalyzerRegistrar) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    static let analyzerRunKind = ElementProblemAnalyzerRunKind.FullDaemon

    let interruptCheck = daemonProcess.GetCheckForInterrupt()
    let elementProblemAnalyzerData = ElementProblemAnalyzerData(fsFile, settings, analyzerRunKind, interruptCheck)
    let analyzerDispatcher = analyzerRegistrar.CreateDispatcher(elementProblemAnalyzerData)

    override x.VisitNode(element: ITreeNode, consumer: IHighlightingConsumer) =
        analyzerDispatcher.Run(element, consumer)

    override x.Execute(committer) =
        // todo: schedule separate processors for different top level nodes
        let consumer = FilteringHighlightingConsumer(daemonProcess.SourceFile, fsFile, settings)
        fsFile.ProcessThisAndDescendants(Processor(x, consumer))
        committer.Invoke(DaemonStageResult(consumer.Highlightings))


and Processor(daemonProcess: FSharpDaemonStageProcessBase, consumer: IHighlightingConsumer) =
    abstract InteriorShouldBeProcessed: ITreeNode -> bool
    default x.InteriorShouldBeProcessed _ = true

    interface IRecursiveElementProcessor with
        member x.InteriorShouldBeProcessed(element) =
            x.InteriorShouldBeProcessed(element)

        member x.ProcessingIsFinished =
            match daemonProcess.DaemonProcess.InterruptFlag with
            | true -> OperationCanceledException() |> raise
            | _ -> false

        member x.ProcessBeforeInterior _ = ()

        member x.ProcessAfterInterior(element) =
            match element with
            | :? FSharpToken as token ->
                if not (token.GetTokenType().IsWhitespace) then
                    token.Accept(daemonProcess, consumer)

            | :? IFSharpTreeNode as fsTreeNode ->
                fsTreeNode.Accept(daemonProcess, consumer)

            | _ -> daemonProcess.VisitNode(element, consumer)
