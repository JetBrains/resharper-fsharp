namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages

open JetBrains.ReSharper.Daemon.UsageChecking
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages

type TypeCheckErrorsStageProcess(daemonProcess, errors) =
    inherit ErrorsStageProcessBase(daemonProcess, errors)

[<DaemonStage(StagesBefore = [| typeof<SyntaxErrorsStage> |], StagesAfter = [| typeof<HighlightOpenExpressionsStage> |])>]
type TypeCheckErrorsStage(daemonProcess, errors) =
    inherit FSharpDaemonStageBase()

    override x.CreateProcess(fsFile, daemonProcess) =
        let errors =
            match fsFile.GetParseAndCheckResults(false) with
            | Some results -> results.CheckResults.Errors
            | _ -> [| |]
        TypeCheckErrorsStageProcess(daemonProcess, errors) :> _

    override x.NeedsErrorStripe(_, _) = ErrorStripeRequest.STRIPE_AND_ERRORS
