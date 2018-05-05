namespace rec JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages

open System.Collections.Generic
open System.Collections.ObjectModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util
open System

[<DaemonStage(StagesBefore = [| typeof<DeadCodeHighlightStage> |], StagesAfter = [| typeof<HighlightIdentifiersStage> |])>]
type ScritpLoadPathsStage(daemonProcess, errors) =
    inherit FSharpDaemonStageBase()

        override x.IsSupported(sourceFile, processKind) =
            processKind = DaemonProcessKind.VISIBLE_DOCUMENT && base.IsSupported(sourceFile, processKind)

        override x.CreateProcess(fsFile, daemonProcess) =
            ScriptLoadPathsStageProcess(fsFile, daemonProcess) :> _


type ScriptLoadPathsStageProcess(fsFile, daemonProcess) =
    inherit FSharpDaemonStageProcessBase(daemonProcess)

    override x.Execute(committer) =
        let interruptChecker = x.SeldomInterruptChecker

        let allDirectives = Dictionary<TreeOffset, IHashDirective>()
        let visitor =
            { new TreeNodeVisitor() with
                override x.VisitFSharpFile(fsFile) =
                    for decl in fsFile.Declarations do
                        decl.Accept(x)

                override __.VisitTopLevelModuleDeclaration(decl) =
                    for memberDecl in decl.Members do
                        match memberDecl with
                        | :? IHashDirective as directive ->
                            match directive.HashToken with
                            | null -> ()
                            | token when token.GetText() = "#load" ->
                                // todo: implement for other directives
                                allDirectives.Add(directive.GetTreeStartOffset(), directive)
                            | _ -> ()
                        | _ -> ()

                        interruptChecker.CheckForInterrupt()
            }
        fsFile.Accept(visitor)
        if allDirectives.IsEmpty() then () else

        match fsFile.CheckerService.OptionsProvider.GetProjectOptions(daemonProcess.SourceFile) with
        | Some options when not options.OriginalLoadReferences.IsEmpty ->
            let document = daemonProcess.Document
            let linesCount = document.GetLineCount() |> int
            let loadedDirectives =
                options.OriginalLoadReferences
                |> Seq.filter (fun (range, _) -> range.EndLine < linesCount)
                |> Seq.map (fun (range, _) -> document.GetTreeStartOffset(range))
                |> HashSet

            let unusedDirectives =            
                allDirectives
                |> Seq.filter (fun d -> loadedDirectives.Contains(d.Key) |> not)
                |> Seq.map (fun d ->
                    let range = d.Value.GetDocumentRange()
                    HighlightingInfo(range, DeadCodeHighlighting(range)))

            committer.Invoke(DaemonStageResult(unusedDirectives.AsReadOnlyCollection()))
        | _ -> ()
