namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open System.Collections.Generic
open FSharp.Compiler.SourceCodeServices
open JetBrains.ReSharper.Daemon.UsageChecking
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

[<DaemonStage(StagesBefore = [| typeof<HighlightIdentifiersStage> |], StagesAfter = [| typeof<CollectUsagesStage> |])>]
type UnusedOpensStage() =
    inherit FSharpDaemonStageBase()

    override x.CreateStageProcess(fsFile: IFSharpFile, _, daemonProcess: IDaemonProcess) =
        UnusedOpensStageProcess(fsFile, daemonProcess) :> _


and UnusedOpensStageProcess(fsFile: IFSharpFile, daemonProcess: IDaemonProcess) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    let [<Literal>] opName = "UnusedOpensStageProcess"

    let document = fsFile.GetSourceFile().Document
    let lines = Dictionary<int, string>()

    let getLine line =
        let line = line - 1
        use cookie = ReadLockCookie.Create()
        lines.GetOrCreateValue(line, fun () -> document.GetLineText(docLine line))

    override x.Execute(committer) =
        let highlightings = List()
        let interruptChecker = daemonProcess.CreateInterruptChecker()
        match fsFile.GetParseAndCheckResults(false, opName) with
        | None -> ()
        | Some results ->

        let checkResults = results.CheckResults
        for range in UnusedOpens.getUnusedOpens(checkResults, getLine).RunAsTask(interruptChecker) do
            x.SeldomInterruptChecker.CheckForInterrupt()
            match fsFile.GetNode<IOpenStatement>(document, range) with
            | null -> ()
            | openDirective ->

            // todo: remove after FCS update, https://github.com/dotnet/fsharp/pull/10510
            if isNotNull openDirective.TypeKeyword then () else

            let range = openDirective.GetHighlightingRange()
            highlightings.Add(HighlightingInfo(range, UnusedOpenWarning(openDirective)))
        committer.Invoke(DaemonStageResult(highlightings))
