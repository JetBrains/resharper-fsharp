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
open Microsoft.FSharp.Compiler.SourceCodeServices

type AssemblySignature =
    | ProjectSignature of Assembly : FSharpAssemblySignature
    | PartialSignature of Assembly : FSharpAssemblySignature * LastKnownFileIndex : int

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

    member x.ParseFile([<NotNull>] file : IPsiSourceFile) =
        match x.OptionsProvider.GetProjectOptions(file, checker) with
        | Some projectOptions as options ->
            let filePath = file.GetLocation().FullPath
            let source = file.Document.GetText()
            options, Some (checker.ParseFileInProject(filePath, source, projectOptions).RunAsTask())
        | _ -> None, None

    member x.CheckFile([<NotNull>] file : IFile, parseResults : FSharpParseFileResults, ?interruptChecker) =
        match file.GetSourceFile() with
        | sourceFile when isNotNull sourceFile ->
            match x.OptionsProvider.GetProjectOptions(sourceFile, checker) with
            | Some options ->
                let filePath = sourceFile.GetLocation().FullPath
                let source = sourceFile.Document.GetText()
                let checkAsync = checker.CheckFileInProject(parseResults, filePath, 0, source, options)
                match checkAsync.RunAsTask(?interruptChecker = interruptChecker) with
                | FSharpCheckFileAnswer.Succeeded results -> Some results
                | _ -> None
            | _ -> None
        | _ -> None

    member x.HasPairFile([<NotNull>] file : IPsiSourceFile) =
        x.OptionsProvider.HasPairFile(file)

    member x.InvalidateAssemblySignature([<NotNull>] project : IProject, invalidateReferencing : bool) =
        assemblySignatures.remove project
        if invalidateReferencing then
            for p in project.GetReferencingProjects(project.GetCurrentTargetFrameworkId()) do
                if p.IsOpened && isApplicable p then x.InvalidateAssemblySignature(p, invalidateReferencing)

    member x.CheckProject(project : IProject) =
        checker.ParseAndCheckProject(x.OptionsProvider.GetProjectOptions(project).Value)

    member private x.GetProjectSignature(project : IProject) =
        let signature = x.CheckProject(project).RunAsTask().AssemblySignature
        assemblySignatures.add(project, ProjectSignature(signature))
        signature

    member x.GetAssemblySignature([<NotNull>] file : IPsiSourceFile) =
        let project = file.GetProject()
        match assemblySignatures.TryGetValue(project) with
        | true, ProjectSignature(signature) -> signature
        | true, PartialSignature(signature, lastFileIndex)
            when x.OptionsProvider.GetFileIndex(file) <= lastFileIndex -> signature
        | _ -> x.GetProjectSignature(project)

    member x.GetDefines(sourceFile : IPsiSourceFile) =
        match x.OptionsProvider.TryGetFSharpProject(sourceFile.GetProject()) with
        | Some project -> project.ConfigurationDefines
        | _ -> List.empty

    member x.GetOrCreateParseResults([<NotNull>] file : IPsiSourceFile) =
        snd (x.ParseFile file)