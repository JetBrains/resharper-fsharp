namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages

open FSharp.Compiler.ErrorLogger
open FSharp.Compiler.SourceCodeServices
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.Util

[<DaemonStage(StagesBefore = [| typeof<SyntaxErrorsStage> |], StagesAfter = [| typeof<HighlightIdentifiersStage> |])>]
type TypeCheckErrorsStage(logger: ILogger) =
    inherit FSharpDaemonStageBase()

    override x.CreateStageProcess(fsFile, _, daemonProcess) =
        TypeCheckErrorsStageProcess(fsFile, daemonProcess, logger) :> _


and TypeCheckErrorsStageProcess(fsFile, daemonProcess, logger: ILogger) =
    inherit FcsErrorsStageProcessBase(fsFile, daemonProcess)

    let [<Literal>] opName = "TypeCheckErrorsStageProcess"

    override x.ShouldAddDiagnostic(error, range) =
        base.ShouldAddDiagnostic(error, range) && error.Subcategory <> BuildPhaseSubcategory.Parse

    override x.Execute(committer) =
        match fsFile.GetParseAndCheckResults(false, opName) with
        | None -> ()
        | Some results ->

        let projectWarnings, fileErrors =
            results.CheckResults.Errors
            |> Array.partition (fun e ->
                e.StartLineAlternate = 0 && e.EndLineAlternate = 0 && e.Severity = FSharpErrorSeverity.Warning)

        if not (Array.isEmpty projectWarnings) then
            // https://github.com/Microsoft/visualfsharp/issues/4030
            let errors = projectWarnings |> Array.fold (fun result info -> result + "\n" + info.ToString()) ""
            logger.LogMessage(LoggingLevel.WARN, "Project warnings during file typeCheck:" + errors)

        x.Execute(fileErrors, committer)
