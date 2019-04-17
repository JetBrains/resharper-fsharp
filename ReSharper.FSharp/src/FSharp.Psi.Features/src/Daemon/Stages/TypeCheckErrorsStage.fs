namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages

open FSharp.Compiler.ErrorLogger
open FSharp.Compiler.SourceCodeServices
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.Util

type TypeCheckErrorsStageProcess(fsFile: IFSharpFile, daemonProcess, logger: ILogger) =
    inherit ErrorsStageProcessBase(fsFile, daemonProcess)

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
            let errors = Array.fold (fun a e -> a + "\n" + e.ToString()) "" projectWarnings
            logger.LogMessage(LoggingLevel.WARN, "Project warnings during file typecheck:" + errors)
        x.Execute(fileErrors, committer)


[<DaemonStage(StagesBefore = [| typeof<SyntaxErrorsStage> |], StagesAfter = [| typeof<HighlightIdentifiersStage> |])>]
type TypeCheckErrorsStage(logger: ILogger) =
    inherit FSharpDaemonStageBase()

    override x.CreateStageProcess(fsFile, _, daemonProcess) =
        TypeCheckErrorsStageProcess(fsFile, daemonProcess, logger) :> _
