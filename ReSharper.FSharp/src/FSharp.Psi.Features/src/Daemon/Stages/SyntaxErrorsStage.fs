namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

[<AllowNullLiteral>]
type SyntaxErrorsStageProcess(fsFile: IFSharpFile, daemonProcess) =
    inherit ErrorsStageProcessBase(daemonProcess)

    override x.Execute(committer) =
        fsFile.CheckerService.ParseFile(daemonProcess.SourceFile)
        |> Option.iter (fun parseResults -> x.Execute(parseResults.Errors, committer))

[<DaemonStage(StagesBefore = [| typeof<DeadCodeHighlightStage> |], StagesAfter = [| typeof<HighlightIdentifiersStage> |])>]
type SyntaxErrorsStage(daemonProcess, errors) =
    inherit FSharpDaemonStageBase()

    override x.CreateProcess(fsFile, daemonProcess) =
        SyntaxErrorsStageProcess(fsFile, daemonProcess) :> _
