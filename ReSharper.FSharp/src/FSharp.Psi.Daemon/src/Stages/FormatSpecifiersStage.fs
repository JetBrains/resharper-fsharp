namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open System.Collections.Generic
open JetBrains.ReSharper.Daemon.Impl
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util

type FormatSpecifiersStageProcess(fsFile: IFSharpFile, daemonProcess) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    let [<Literal>] opName = "FormatSpecifiersStageProcess"

    override x.Execute(committer) =
        match fsFile.GetParseAndCheckResults(false, opName) with
        | None -> ()
        | Some results ->

        let highlightings = List()
        let document = daemonProcess.Document

        for range, _ in results.CheckResults.GetFormatSpecifierLocationsAndArity() do
            let documentRange = getDocumentRange document range
            highlightings.Add(HighlightingInfo(documentRange, FormatStringItemHighlighting(documentRange)))

        committer.Invoke(DaemonStageResult(highlightings))

[<DaemonStage(StagesBefore = [| typeof<HighlightIdentifiersStage> |], StagesAfter = [| typeof<UnusedOpensStage> |])>]
type FormatSpecifiersStage() =
    inherit FSharpDaemonStageBase()

    override x.IsSupported(sourceFile, processKind) =
        processKind = DaemonProcessKind.VISIBLE_DOCUMENT && base.IsSupported(sourceFile, processKind)

    override x.CreateStageProcess(fsFile: IFSharpFile, _, daemonProcess: IDaemonProcess) =
        FormatSpecifiersStageProcess(fsFile, daemonProcess) :> _
