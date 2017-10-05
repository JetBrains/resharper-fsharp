namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages

type SyntaxErrorsStageProcess(daemonProcess, errors) =
    inherit ErrorsStageProcessBase(daemonProcess, errors)

[<DaemonStage(StagesBefore = [| typeof<SyntaxHighlightingStage> |], StagesAfter = [| typeof<SetResolvedSymbolsStage> |])>]
type SyntaxErrorsStage(daemonProcess, errors) =
    inherit FSharpDaemonStageBase()

    override x.CreateProcess(fsFile, daemonProcess) =
        let errors =
            match fsFile.ParseResults with
            | Some parseResults -> parseResults.Errors
            | _ -> [| |]
        SyntaxErrorsStageProcess(daemonProcess, errors) :> _

    override x.NeedsErrorStripe(_, _) = ErrorStripeRequest.STRIPE_AND_ERRORS
