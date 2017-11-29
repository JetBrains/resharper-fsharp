namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages

open System.Linq
open JetBrains.ReSharper.Daemon.Stages
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree

type DeadCodeHighlightStageProcess(fsFile: IFSharpFile, daemonProcess) =
    inherit FSharpDaemonStageProcessBase(daemonProcess)

    override x.Execute(committer) =
        let highlightings = ResizeArray<HighlightingInfo>()
        for token in fsFile.Tokens().OfType<FSharpDeadCodeToken>() do
            let range = token.GetNavigationRange()
            highlightings.Add(HighlightingInfo(range, DeadCodeHighlighting(range)))
        committer.Invoke(DaemonStageResult(highlightings))

[<DaemonStage(StagesBefore = [| typeof<GlobalFileStructureCollectorStage> |])>]
type DeadCodeHighlightStage(daemonProcess) =
    inherit FSharpDaemonStageBase()

    override x.CreateProcess(fsFile, daemonProcess) =
        DeadCodeHighlightStageProcess(fsFile, daemonProcess) :> _
