namespace rec JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages

open System.Collections.Generic
open JetBrains.ReSharper.Daemon.Stages
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

[<DaemonStage(StagesBefore = [| typeof<GlobalFileStructureCollectorStage> |], StagesAfter = [| typeof<HighlightIdentifiersStage> |])>]
type ScriptLoadPathsStage() =
    inherit FSharpDaemonStageBase()

        override x.IsSupported(sourceFile, processKind) =
            processKind = DaemonProcessKind.VISIBLE_DOCUMENT && base.IsSupported(sourceFile, processKind)

        override x.CreateStageProcess(fsFile, _, daemonProcess) =
            ScriptLoadPathsStageProcess(fsFile, daemonProcess) :> _


type ScriptLoadPathsStageProcess(fsFile, daemonProcess) =
    inherit FSharpDaemonStageProcessBase(fsFile, daemonProcess)

    override x.Execute(committer) =
        let interruptChecker = x.SeldomInterruptChecker

        let allDirectives = Dictionary<TreeOffset, IHashDirective>()
        let visitor =
            { new TreeNodeVisitor() with
                override x.VisitFSharpFile(fsFile) =
                    for decl in fsFile.ModuleDeclarations do
                        decl.Accept(x)

                override __.VisitNamedModuleDeclaration(decl) =
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

                        interruptChecker.CheckForInterrupt() }

        fsFile.Accept(visitor)
        if allDirectives.IsEmpty() then () else

        match fsFile.CheckerService.FcsProjectProvider.GetProjectOptions(daemonProcess.SourceFile) with
        | Some options when not options.OriginalLoadReferences.IsEmpty ->
            let document = daemonProcess.Document
            let linesCount = document.GetLineCount() |> int
            let loadedDirectives =
                let result = HashSet()
                for range, _, _ in options.OriginalLoadReferences do
                    if range.EndLine < linesCount then
                        result.Add(getTreeStartOffset document range) |> ignore
                result

            let unusedDirectives =
                let result = LocalList<HighlightingInfo>()
                for directive in allDirectives do
                    if not (loadedDirectives.Contains(directive.Key)) then
                        let range = directive.Value.GetDocumentRange()
                        result.Add(HighlightingInfo(range, DeadCodeHighlighting(range)))
                result.ReadOnlyList()

            committer.Invoke(DaemonStageResult(unusedDirectives))
        | _ -> ()
