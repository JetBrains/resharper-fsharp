namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages

open JetBrains.DocumentModel
open JetBrains.ReSharper.Daemon.Impl
open System.Collections.Generic
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.Util
open Microsoft.FSharp.Compiler

type FormatSpecifiersStageProcess(fsFile: IFSharpFile, daemonProcess) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    let getDocumentRange (range: Range.range) =
        let document = daemonProcess.Document
        let startOffset =  document.GetDocumentOffset(range.StartLine - 1, range.StartColumn)
        let endOffset = document.GetDocumentOffset(range.EndLine - 1, range.EndColumn)
        DocumentRange(document, TextRange(startOffset, endOffset))
    
    override x.Execute(committer) =
        match fsFile.GetParseAndCheckResults(false) with
        | Some results ->
            let highlightings = List()
            for range, _ in results.CheckResults.GetFormatSpecifierLocationsAndArity() do
                let documentRange = getDocumentRange range
                highlightings.Add(HighlightingInfo(documentRange, FormatStringItemHighlighting(documentRange)))
            
            committer.Invoke(DaemonStageResult(highlightings))    
        | _ -> ()

[<DaemonStage(StagesBefore = [| typeof<HighlightIdentifiersStage> |], StagesAfter = [| typeof<UnusedOpensStage> |])>]
type FormatSpecifiersStage(daemonProcess, errors) =
    inherit FSharpDaemonStageBase()

    override x.CreateStageProcess(fsFile: IFSharpFile, _, daemonProcess: IDaemonProcess) =
        FormatSpecifiersStageProcess(fsFile, daemonProcess) :> _
