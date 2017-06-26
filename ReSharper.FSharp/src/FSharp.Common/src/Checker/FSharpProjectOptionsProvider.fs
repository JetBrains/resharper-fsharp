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
open JetBrains
open Microsoft.FSharp.Compiler.SourceCodeServices

type Logger = Util.ILoggerEx
type LoggingLevel = Util.LoggingLevel

[<SolutionComponent>]
type FSharpProjectOptionsProvider(lifetime, logger : Util.ILogger, solution : ISolution,
                                  checkerService : FSharpCheckerService, optionsBuilder : FSharpProjectOptionsBuilder,
                                  changeManager : ChangeManager) as this =
    inherit RecursiveProjectModelChangeDeltaVisitor()

    let projects = Dictionary<IProject, FSharpProject>()
    let scriptOptions = Dictionary<FileSystemPath, FSharpProjectOptions>()
    let projectsToInvalidate = JetHashSet<IProject>()
    let mutable fsCorePath : string = null
    
    let invalidProject (p : IProject) =
        invalidOp (sprintf "Project %s is not opened" p.ProjectFileLocation.FullPath)
    
    let getProject (file : IPsiSourceFile) =
        let project = file.GetProject()
        match projects.TryGetValue(project) with
        | true, fsProject -> fsProject
        | _ -> invalidProject project

    do
        changeManager.Changed2.Advise(lifetime, this.ProcessChange);
        checkerService.OptionsProvider <- this
        lifetime.AddAction(fun _ -> checkerService.OptionsProvider <- null) |> ignore

        if Util.PlatformUtil.IsRunningOnMono then
            let fsCoreAssembly = Assembly.GetAssembly(typeof<Unit>)
            if isNotNull fsCoreAssembly then
                let path = Util.FileSystemPath.TryParse(fsCoreAssembly.Location)
                if isNotNull path then fsCorePath <- "-r:" + path.FullPath

    member private x.ProcessChange(obj : ChangeEventArgs) =
        let change = obj.ChangeMap.GetChange<ProjectModelChange>(solution)
        if isNotNull change then
            lock projects (fun _ -> x.VisitDelta change)

    override x.VisitDelta(change : ProjectModelChange) =
        if change.IsClosingSolution then x.InvalidateAll()
        else
            match change.ProjectModelElement with
            | :? IProject as project when isApplicable project ->
                if change.IsRemoved then
                    match projects.TryGetValue project with
                    | true, fsProject ->
                        checkerService.Checker.InvalidateConfiguration(fsProject.Options.Value)
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

    member private x.GetProjectOptionsImpl(file : IPsiSourceFile, checker : FSharpChecker) =
        lock projects (fun _ ->
            x.ProcessInvalidateOptions(checker)
            let project = file.GetProject()
            match projects.TryGetValue(project) with
            | true, fsProject when fsProject.ContainsFile file -> fsProject.Options
            | _ when project.IsOpened -> x.CreateProjectOptions(project, checker)
            | _ -> None)

    member private x.CreateProjectOptions(project, checker) =
        match projects.TryGetValue(project) with
        | true, project -> project.Options
        | _ ->
            let fsProject = optionsBuilder.BuildSingleProjectOptions(project)
            let framework = project.GetCurrentTargetFrameworkId()
            let referencedProjectsOptions =
                seq { for p in project.GetReferencedProjects(framework, transitive = false) do
                          if p.IsOpened && isApplicable p then
                              let outPath = p.GetOutputFilePath(p.GetCurrentTargetFrameworkId()).FullPath
                              yield outPath, x.CreateProjectOptions(p, checker).Value }

            let options' = { fsProject.Options.Value with ReferencedProjects = referencedProjectsOptions.ToArray() }
            let fsProject = { fsProject with Options = Some options' }
            projects.[project] <- fsProject
            fsProject.Options

    member private x.ProcessInvalidateOptions(checker) =
        for p in projectsToInvalidate do
            if projects.contains p then
                checker.InvalidateConfiguration(projects.[p].Options.Value)
                projects.remove p
        projectsToInvalidate.Clear()

    member private x.GetScriptOptionsImpl(file : IPsiSourceFile, checker : FSharpChecker, updateScriptOptions : bool) =
        let path = file.GetLocation()
        if updateScriptOptions then x.GetNewScriptOptions(file, checker)
        else
            match scriptOptions.TryGetValue(path) with
            | true, options -> Some options
            | _ -> x.GetNewScriptOptions(file, checker)
    
    member private x.GetNewScriptOptions(file : IPsiSourceFile, checker : FSharpChecker) =
        let path = file.GetLocation()
        let filePath = path.FullPath
        let source = file.Document.GetText()
        let loadTime = DateTime.Now
        let getScriptOptionsAsync = checker.GetProjectOptionsFromScript(filePath, source, loadTime)
        try
            let options, errors = getScriptOptionsAsync.RunAsTask()
            if not errors.IsEmpty then logger.LogFSharpErrors("Script options for " + filePath) errors
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
        if isNull fsCorePath then options
        else { options with OtherOptions = Array.append options.OtherOptions [| fsCorePath |] }

    member private x.InvalidateAll() =
        projects.Clear()
        projectsToInvalidate.Clear() // todo: check?

    interface IFSharpProjectOptionsProvider with
        member x.GetProjectOptions(file, checker, updateScriptOptions) =
            if file.LanguageType.Equals(FSharpScriptProjectFileType.Instance) ||
                    file.PsiModule.IsMiscFilesProjectModule() then
                x.GetScriptOptionsImpl(file, checker, updateScriptOptions)
            else x.GetProjectOptionsImpl(file, checker)

        member x.GetProjectOptions(project) =
            match projects.TryGetValue(project) with
            | true, fsProject -> fsProject.Options
            | _ -> invalidProject project

        member x.TryGetFSharpProject(project : IProject) =
            match projects.TryGetValue project with
            | true, fsProject -> Some fsProject
            | _ -> None

        member x.GetFileIndex(file) =
            let fsProject = getProject file
            let filePath = file.GetLocation()
            match fsProject.FileIndices.TryGetValue(filePath) with
            | true, index -> index
            | _ -> invalidOp (sprintf "%s doesn't belong to %A" filePath.FullPath fsProject)
            
        member x.HasPairFile(file) =
            let fsProject = getProject file 
            fsProject.FilesWithPairs.Contains(file.GetLocation())
