namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open System
open System.Collections.Generic
open FSharp.Compiler.EditorServices
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

module UnusedOpensStageProcess =
    let [<Literal>] opName = "UnusedOpensStageProcess"

    let lines = Dictionary<int, string>()

    let getUnusedOpens (fsFile: IFSharpFile) (interruptChecker: Action): IOpenStatement[] =
        let document = fsFile.GetSourceFile().Document

        let getLine line =
            let line = line - 1
            use cookie = ReadLockCookie.Create()
            lines.GetOrCreateValue(line, fun () -> document.GetLineText(docLine line))

        let highlightings = List()
        match fsFile.GetParseAndCheckResults(false, opName) with
        | None -> EmptyArray.Instance
        | Some results ->

        let checkResults = results.CheckResults
        for range in UnusedOpens.getUnusedOpens(checkResults, getLine).RunAsTask(interruptChecker) do
            match fsFile.GetNode<IOpenStatement>(document, range) with
            | null -> ()
            | openDirective ->
                // todo: remove this check after FCS update, https://github.com/dotnet/fsharp/pull/10510
                if isNull openDirective.TypeKeyword then
                    highlightings.Add(openDirective)
        highlightings.AsArray()

[<DaemonStage(StagesBefore = [| typeof<HighlightIdentifiersStage> |], StagesAfter = [| typeof<CollectUsagesStage> |])>]
type UnusedOpensStage() =
    inherit FSharpDaemonStageBase()

    override x.CreateStageProcess(fsFile: IFSharpFile, _, daemonProcess: IDaemonProcess) =
        UnusedOpensStageProcess(fsFile, daemonProcess) :> _


and UnusedOpensStageProcess(fsFile: IFSharpFile, daemonProcess: IDaemonProcess) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    override x.Execute(committer) =
        let interruptChecker = daemonProcess.CreateInterruptChecker()
        let unusedOpens = UnusedOpensStageProcess.getUnusedOpens fsFile interruptChecker

        let seldomInterruptChecker = x.SeldomInterruptChecker
        unusedOpens
        |> Array.map (fun openDirective ->
            seldomInterruptChecker.CheckForInterrupt()
            let range = openDirective.GetHighlightingRange()
            HighlightingInfo(range, UnusedOpenWarning(openDirective)))
        |> (DaemonStageResult >> committer.Invoke)
