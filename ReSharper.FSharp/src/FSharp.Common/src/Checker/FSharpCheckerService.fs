namespace JetBrains.ReSharper.Plugins.FSharp.Common.Checker

open System
open System.Collections.Generic
open System.Linq
open System.Threading
open JetBrains.Annotations
open JetBrains.Application
open JetBrains.Application.Threading
open JetBrains.DataFlow
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ProjectModel.Properties
open JetBrains.ProjectModel.Properties.CSharp
open JetBrains.ProjectModel.Properties.Managed
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.SourceCodeServices

type AssemblySignature =
    | ProjectSignature of Assembly: FSharpAssemblySignature
    | PartialSignature of Assembly: FSharpAssemblySignature * LastKnownFileIndex : int
    
type FSharpParseAndCheckResults = 
    {
      ParseResults: FSharpParseFileResults
      ParseTree: Ast.ParsedInput
      CheckResults: FSharpCheckFileResults
    }

[<ShellComponent>]
type FSharpCheckerService(lifetime, onSolutionCloseNotifier : OnSolutionCloseNotifier) =
    let checker = FSharpChecker.Create(projectCacheSize = 200, keepAllBackgroundResolutions = false)
    let assemblySignatures = Dictionary<IProject, AssemblySignature>()
    do
        onSolutionCloseNotifier.SolutionIsAboutToClose.Advise(lifetime, fun () ->
            checker.ClearLanguageServiceRootCachesAndCollectAndFinalizeAllTransients()
            assemblySignatures.Clear())

    member val OptionsProvider : IFSharpProjectOptionsProvider = null with get, set
    member x.Checker = checker

    member x.ParseFile([<NotNull>] file: IPsiSourceFile) =
        match x.OptionsProvider.GetParsingOptions(file, checker, false) with
        | Some projectOptions as options ->
            let filePath = file.GetLocation().FullPath
            let source = file.Document.GetText()
            options, Some (checker.ParseFile(filePath, source, projectOptions).RunAsTask())
        | _ -> None, None

    member x.HasPairFile([<NotNull>] file: IPsiSourceFile) =
        x.OptionsProvider.HasPairFile(file, checker)

    member x.CheckProject(project: IProject) =
        checker.ParseAndCheckProject(x.OptionsProvider.GetProjectOptions(project, checker).Value)

    member private x.GetProjectSignature(project: IProject) =
        let signature = x.CheckProject(project).RunAsTask().AssemblySignature
        assemblySignatures.add(project, ProjectSignature(signature))
        signature

    member x.GetAssemblySignature([<NotNull>] file: IPsiSourceFile) =
        let project = file.GetProject()
        match assemblySignatures.TryGetValue(project) with
        | true, ProjectSignature(signature) -> signature
        | true, PartialSignature(signature, lastFileIndex)
            when x.OptionsProvider.GetFileIndex(file, checker) <= lastFileIndex -> signature
        | _ -> x.GetProjectSignature(project)

    member x.GetDefines(sourceFile: IPsiSourceFile) =
        match x.OptionsProvider.TryGetFSharpProject(sourceFile, checker) with
        | Some project -> project.ConfigurationDefines
        | _ -> List.empty

    member x.ParseAndCheckFile([<NotNull>] file: IPsiSourceFile, allowStaleResults: bool) =
        match x.OptionsProvider.GetProjectOptions(file, checker, true) with
        | Some options ->
            let filePath = file.GetLocation().FullPath
            let source = file.Document.GetText()
            let parsingResults, checkResults = checker.ParseAndCheckFileInProject(filePath, 0, source, options).RunAsTask()
            match parsingResults.ParseTree, checkResults with
            | Some parseTree, FSharpCheckFileAnswer.Succeeded checkResults ->
                Some { ParseResults = parsingResults; ParseTree = parseTree; CheckResults = checkResults }
            | _ -> None
        | _ -> None