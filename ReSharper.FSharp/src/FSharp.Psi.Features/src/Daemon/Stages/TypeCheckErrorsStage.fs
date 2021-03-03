namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages

open FSharp.Compiler.Diagnostics
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
        base.ShouldAddDiagnostic(error, range) && error.Subcategory <> "parse"

    override x.Execute(committer) =
        match fsFile.GetParseAndCheckResults(false, opName) with
        | None -> ()
        | Some results ->

        let projectWarnings, fileErrors =
            results.CheckResults.Diagnostics
            |> Array.partition (fun e ->
                e.StartLine = 0 && e.EndLine = 0 && e.Severity = FSharpDiagnosticSeverity.Warning)

        if not (Array.isEmpty projectWarnings) then
            // https://github.com/Microsoft/visualfsharp/issues/4030
            let errors = projectWarnings |> Array.fold (fun result info -> result + "\n" + info.ToString()) ""
            logger.LogMessage(LoggingLevel.WARN, "Project warnings during file typeCheck:" + errors)

        x.Execute(fileErrors, committer)
