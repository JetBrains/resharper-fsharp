namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages

open JetBrains.ReSharper.Daemon.UsageChecking
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.Util
open Microsoft.FSharp.Compiler.SourceCodeServices

[<AllowNullLiteral>]
type TypeCheckErrorsStageProcess(fsFile: IFSharpFile, daemonProcess, logger: ILogger) =
    inherit ErrorsStageProcessBase(daemonProcess)

    override x.Execute(committer) =
        fsFile.GetParseAndCheckResults(false)
        |> Option.iter (fun results ->
            daemonProcess.CustomData.PutData(FSharpDaemonStageBase.TypeCheckResults, Some results.CheckResults)
            let projectWarnings, fileErrors  =
                results.CheckResults.Errors
                |> Array.partition (fun e ->
                    e.StartLineAlternate = 0 && e.EndLineAlternate = 0 && e.Severity = FSharpErrorSeverity.Warning)

            if not (Array.isEmpty projectWarnings) then
                // https://github.com/Microsoft/visualfsharp/issues/4030
                let errors = Array.fold (fun a e -> a + "\n" + e.ToString()) "" projectWarnings
                logger.LogMessage(LoggingLevel.WARN, "Project warnings during file typecheck:" + errors)
            x.Execute(fileErrors, committer))


[<DaemonStage(StagesBefore = [| typeof<SyntaxErrorsStage> |], StagesAfter = [| typeof<HighlightIdentifiersStage> |])>]
type TypeCheckErrorsStage(daemonProcess, logger: ILogger) =
    inherit FSharpDaemonStageBase()

    override x.CreateProcess(fsFile, daemonProcess) =
        TypeCheckErrorsStageProcess(fsFile, daemonProcess, logger) :> _
