namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages

open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages

[<AllowNullLiteral>]
type SyntaxErrorsStageProcess(daemonProcess, errors) =
    inherit ErrorsStageProcessBase(daemonProcess, errors)

[<DaemonStage(StagesBefore = [| typeof<DeadCodeHighlightStage> |], StagesAfter = [| typeof<HighlightIdentifiersStage> |])>]
type SyntaxErrorsStage(daemonProcess, errors) =
    inherit FSharpDaemonStageBase()

    override x.CreateProcess(fsFile, daemonProcess) =
        fsFile.ParseResults
        |> Option.map (fun parseResults -> SyntaxErrorsStageProcess(daemonProcess, parseResults.Errors))
        |> Option.defaultValue null :> _

    override x.NeedsErrorStripe(_, _) = ErrorStripeRequest.STRIPE_AND_ERRORS
