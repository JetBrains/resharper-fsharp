namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages

open System.Linq
open JetBrains.ReSharper.Daemon.Stages
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

type DeadCodeHighlightStageProcess(fsFile: IFSharpFile, daemonProcess) =
    inherit FSharpDaemonStageProcessBase(daemonProcess)

    override x.Execute(committer) =
        let highlightings = LocalList<HighlightingInfo>()
        for token in fsFile.Tokens() do
            match token with
            | :? FSharpDeadCodeToken ->
                let range = token.GetNavigationRange()
                highlightings.Add(HighlightingInfo(range, DeadCodeHighlighting(range)))
            | _ -> ()
            x.SeldomInterruptChecker.CheckForInterrupt()
        committer.Invoke(DaemonStageResult(highlightings.ReadOnlyList()))

[<DaemonStage(StagesBefore = [| typeof<GlobalFileStructureCollectorStage> |])>]
type DeadCodeHighlightStage(daemonProcess) =
    inherit FSharpDaemonStageBase()

    override x.CreateProcess(fsFile, daemonProcess) =
        DeadCodeHighlightStageProcess(fsFile, daemonProcess) :> _
