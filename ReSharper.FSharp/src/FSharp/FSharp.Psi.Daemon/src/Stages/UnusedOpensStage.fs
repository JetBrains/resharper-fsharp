namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Stages

open JetBrains.Application
open JetBrains.Application.Parts
open JetBrains.ReSharper.Daemon.UsageChecking
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

[<DaemonStage(Instantiation.DemandAnyThreadSafe, StagesBefore = [| typeof<HighlightIdentifiersStage> |], StagesAfter = [| typeof<CollectUsagesStage> |])>]
type UnusedOpensStage() =
    inherit FSharpDaemonStageBase()

    override x.CreateStageProcess(fsFile: IFSharpFile, _, daemonProcess: IDaemonProcess, _) =
        UnusedOpensStageProcess(fsFile, daemonProcess) :> _


and UnusedOpensStageProcess(fsFile: IFSharpFile, daemonProcess: IDaemonProcess) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    override x.Execute(committer) =
        let unusedOpens = UnusedOpensUtil.getUnusedOpens fsFile

        unusedOpens
        |> Array.map (fun openDirective ->
            Interruption.Current.CheckAndThrow()
            let range = openDirective.GetHighlightingRange()
            HighlightingInfo(range, UnusedOpenWarning(openDirective)))
        |> DaemonStageResult
        |> committer.Invoke
