namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Stages

open JetBrains.Application.Parts
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

[<DaemonStage(Instantiation.DemandAnyThreadSafe,
              StagesBefore = [| typeof<GlobalFileStructureCollectorStage> |],
              StagesAfter = [| typeof<HighlightIdentifiersStage> |])>]
type SyntaxErrorsStage() =
    inherit FSharpDaemonStageBase()

    override x.CreateStageProcess(fsFile, _, daemonProcess, _) =
        SyntaxErrorsStageProcess(fsFile, daemonProcess) :> _


and SyntaxErrorsStageProcess(fsFile: IFSharpFile, daemonProcess) =
    inherit FcsErrorsStageProcessBase(fsFile, daemonProcess)

    override x.Execute(committer) =
        match fsFile.CheckerService.ParseFile(daemonProcess.SourceFile) with
        | Some parseResults -> x.Execute(parseResults.Diagnostics, committer)
        | _ -> ()
