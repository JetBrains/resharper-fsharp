namespace JetBrains.ReSharper.Plugins.FSharp.Common.Checker

open System
open System.Collections.Generic
open System.Linq
open System.Threading
open System.Reflection
open JetBrains
open JetBrains.Annotations
open JetBrains.Application
open JetBrains.Application.changes
open JetBrains.Application.Components
open JetBrains.Application.Progress
open JetBrains.Application.Threading
open JetBrains.Application.Threading.Tasks
open JetBrains.DataFlow
open JetBrains.Platform.MsBuildModel
open JetBrains.ProjectModel
open JetBrains.ProjectModel.ProjectsHost
open JetBrains.ProjectModel.ProjectsHost.MsBuild
open JetBrains.ProjectModel.ProjectsHost.SolutionHost
open JetBrains.ReSharper.Daemon.Impl
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Common.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.ErrorLogger
open Microsoft.FSharp.Compiler.SourceCodeServices

[<SolutionComponent>]
type FSharpProjectOptionsProvider(lifetime, logger: Util.ILogger, solution: ISolution,
                                  checkerService: FSharpCheckerService, optionsBuilder: FSharpProjectOptionsBuilder,
                                  changeManager: ChangeManager) as this =
    inherit RecursiveProjectModelChangeDeltaVisitor()

    let projects = Dictionary<IProject, FSharpProject>()
    let scriptOptions = Dictionary<FileSystemPath, FSharpProjectOptions>()
    let projectsToInvalidate = JetHashSet<IProject>()
    
    let invalidProject (p: IProject) =
        invalidOp (sprintf "Project %s is not opened" p.ProjectFileLocation.FullPath)
    
    let getProject (file: IPsiSourceFile) =
        let project = file.GetProject()
        match projects.TryGetValue(project) with
        | true, fsProject -> fsProject
        | _ -> invalidProject project

    do
        changeManager.Changed2.Advise(lifetime, this.ProcessChange);
        checkerService.OptionsProvider <- this
        lifetime.AddAction(fun _ -> checkerService.OptionsProvider <- null) |> ignore

    member private x.ProcessChange(obj: ChangeEventArgs) =
        let change = obj.ChangeMap.GetChange<ProjectModelChange>(solution)
        if isNotNull change then
            lock projects (fun _ -> x.VisitDelta change)

    override x.VisitDelta(change: ProjectModelChange) =
        if change.IsClosingSolution then x.InvalidateAll()
        else
            match change.ProjectModelElement with
            | :? IProject as project when isApplicable project ->
                if change.IsRemoved then
                    match projects.TryGetValue project with
                    | true, fsProject ->
                        checkerService.InvalidateProject(fsProject)
                        projects.Remove(project) |> ignore
                        projectsToInvalidate.Remove(project) |> ignore
                        for p in fsProject.ReferencingProjects do
                            projectsToInvalidate.Add(p) |> ignore
                            x.InvalidateReferencingProjects(p)
                    | _ -> ()
                else if project.IsOpened then
                    projectsToInvalidate.add project
                    x.InvalidateReferencingProjects(project)
            | _ -> base.VisitDelta change

    member private x.InvalidateReferencingProjects(project) =
        for p in project.GetReferencingProjects(project.GetCurrentTargetFrameworkId()) do
            if isApplicable project then
                projectsToInvalidate.Add(p) |> ignore
                x.InvalidateReferencingProjects(p)

    member private x.GetProjectOptionsImpl(file: IPsiSourceFile) =
        lock projects (fun _ ->
            x.ProcessInvalidateOptions()
            let project = file.GetProject()
            match projects.TryGetValue(project) with
            | true, fsProject when fsProject.ContainsFile file -> fsProject.Options
            | _ when project.IsOpened -> x.CreateProjectOptions(project)
            | _ -> None)

    member private x.CreateProjectOptions(project) =
        match projects.TryGetValue(project) with
        | true, project -> project.Options
        | _ ->
            let fsProject = optionsBuilder.BuildSingleProjectOptions(project)
            let framework = project.GetCurrentTargetFrameworkId()
            let referencedProjectsOptions =
                seq { for p in project.GetReferencedProjects(framework, transitive = false) do
                          if p.IsOpened && isApplicable p then
                              let outPath = p.GetOutputFilePath(p.GetCurrentTargetFrameworkId()).FullPath
                              yield outPath, x.CreateProjectOptions(p).Value }

            let options = { fsProject.Options.Value with ReferencedProjects = referencedProjectsOptions.ToArray() }
            let options =
                if not (Seq.exists (fun (s: string) -> s.StartsWith "-r:" && s.Contains "FSharp.Core.dll") options.OtherOptions) then
                    { options with OtherOptions = FSharpCoreFix.ensureCorrectFSharpCore options.OtherOptions }
                else options
            let fsProject = { fsProject with Options = Some options }
            projects.[project] <- fsProject
            fsProject.Options

    member private x.ProcessInvalidateOptions() =
        for p in projectsToInvalidate do
            if projects.contains p then
                checkerService.InvalidateProject(projects.[p])
                projects.remove p
        projectsToInvalidate.Clear()

    member private x.GetScriptOptionsImpl(file: IPsiSourceFile, updateScriptOptions: bool) =
        let path = file.GetLocation()
        if updateScriptOptions then x.GetNewScriptOptions(file)
        else
            match scriptOptions.TryGetValue(path) with
            | true, options -> Some options
            | _ -> x.GetNewScriptOptions(file)
    
    member private x.GetNewScriptOptions(file: IPsiSourceFile) =
        let path = file.GetLocation()
        let filePath = path.FullPath
        let source = file.Document.GetText()
        let loadTime = DateTime.Now
        let getScriptOptionsAsync = checkerService.Checker.GetProjectOptionsFromScript(filePath, source, loadTime)
        try
            let options, errors = getScriptOptionsAsync.RunAsTask()
            if not errors.IsEmpty then
                Logger.Warn(logger, "Script options for {0}: {1}", filePath, concatErrors errors)
            let options = x.FixScriptOptions(options)
            scriptOptions.[path] <- options
            Some options
        with
        | :? ProcessCancelledException -> reraise()
        | exn ->
            // todo: replace FCS reference resolver
            Logger.Warn(logger, "Error while getting options for {0}: {1}", filePath, exn.Message)
            None

    member private x.FixScriptOptions(options) =
        { options with OtherOptions = FSharpCoreFix.ensureCorrectFSharpCore options.OtherOptions }

    member private x.InvalidateAll() =
        projects.Clear()
        projectsToInvalidate.Clear() // todo: check?

    interface IFSharpProjectOptionsProvider with
        member x.GetProjectOptions(file, updateScriptOptions) =
            if file.LanguageType.Equals(FSharpScriptProjectFileType.Instance) ||
                    file.PsiModule.IsMiscFilesProjectModule() then
                x.GetScriptOptionsImpl(file, updateScriptOptions)
            else x.GetProjectOptionsImpl(file)

        member x.TryGetFSharpProject(file) =
            let _ = x.GetProjectOptionsImpl(file)
            match projects.TryGetValue(file.GetProject()) with
            | true, fsProject -> Some fsProject
            | _ -> None

        member x.GetFileIndex(file, checker) =
            let _ = x.GetProjectOptionsImpl(file)
            let fsProject = getProject file
            let filePath = file.GetLocation()
            match fsProject.FileIndices.TryGetValue(filePath) with
            | true, index -> index
            | _ -> invalidOp (sprintf "%s doesn't belong to %A" filePath.FullPath fsProject)
            
        member x.HasPairFile(file, checker) =
            let _ = x.GetProjectOptionsImpl(file)
            let fsProject = getProject file 
            fsProject.FilesWithPairs.Contains(file.GetLocation())
        
        member x.GetParsingOptions(file) =
            if file.LanguageType.Equals(FSharpScriptProjectFileType.Instance) ||
               file.PsiModule.IsMiscFilesProjectModule() then
                let scriptParsingOptions =
                    {
                      SourceFiles = Array.ofList [file.GetLocation().FullPath]
                      ConditionalCompilationDefines = []
                      LightSyntax = None
                      CompilingFsLib = false
                      ErrorSeverityOptions = FSharpErrorSeverityOptions.Default
                      IsExe = false
                    }
                Some scriptParsingOptions
            else
                match (x :> IFSharpProjectOptionsProvider).TryGetFSharpProject(file) with
                | Some project when project.Options.IsSome ->
                    match project.ParsingOptions with
                    | None ->
                        let projectOptions = project.Options.Value
                        let parsingOptions, errors = checkerService.Checker.CreateParsingOptions(List.ofArray projectOptions.OtherOptions)
                        let parsingOptions = { parsingOptions with SourceFiles = projectOptions.SourceFiles }
                        if not errors.IsEmpty then
                            Logger.Warn(logger, "Getting parsing options: {0}", concatErrors errors)
                        project.ParsingOptions <- Some parsingOptions
                        Some parsingOptions
                    | options -> options
                | _ -> None
