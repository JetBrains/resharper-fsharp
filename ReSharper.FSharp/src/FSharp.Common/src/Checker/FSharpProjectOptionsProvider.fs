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
open JetBrains.Platform.MsBuildHost.Models
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Assemblies.Impl
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
type FSharpProjectOptionsProvider(lifetime, logger, solution: ISolution, checkerService: FSharpCheckerService,
                                  optionsBuilder: FSharpProjectOptionsBuilder, changeManager: ChangeManager,
                                  fsFileService: IFSharpFileService) as this =
    inherit RecursiveProjectModelChangeDeltaVisitor()

    let projects = Dictionary<IProject, FSharpProject>()
    let projectsToInvalidate = JetHashSet<IProject>()
    let getScriptOptionsLock = obj()

    do
        changeManager.Changed2.Advise(lifetime, this.ProcessChange)
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
            | :? IProject as project when project.IsFSharp ->
                if change.IsRemoved then
                    match projects.TryGetValue project with
                    | true, fsProject ->
                        checkerService.InvalidateProject(fsProject)
                        projects.Remove(project) |> ignore
                        projectsToInvalidate.Remove(project) |> ignore
                        x.InvalidateReferencingProjects(project)
                    | _ -> ()
                else
                    projectsToInvalidate.add project
                x.InvalidateReferencingProjects(project)
            | _ -> base.VisitDelta change

    member private x.InvalidateReferencingProjects(project) =
        if project.IsOpened then
            for projRef in project.GetComponent<ModuleReferencesResolveStore>().GetReferencesToProject(project) do
                let p = projRef.GetProject()
                if isNotNull p && p.IsFSharp then
                    projectsToInvalidate.Add(p) |> ignore
                    x.InvalidateReferencingProjects(p)

    member private x.GetFSharpProject(file: IPsiSourceFile) =
        x.ProcessInvalidateOptions()
        let project = file.GetProject()
        match projects.TryGetValue(project) with
        | true, fsProject when fsProject.ContainsFile file -> Some fsProject
        | _ when project.IsOpened -> Some (x.CreateProject(project))
        | _ -> None

    member private x.CreateProject(project): FSharpProject =
        match projects.TryGetValue(project) with
        | true, project -> project
        | _ ->
            let fsProject = optionsBuilder.BuildSingleProjectOptions(project)
            let framework = project.GetCurrentTargetFrameworkId()
            let referencedProjectsOptions =
                seq { for p in project.GetReferencedProjects(framework, transitive = false) do
                          if p.IsOpened && p.IsFSharp then
                              let outPath = p.GetOutputFilePath(p.GetCurrentTargetFrameworkId()).FullPath
                              yield outPath, x.CreateProject(p).Options.Value }

            let options = { fsProject.Options.Value with ReferencedProjects = referencedProjectsOptions.ToArray() }
            let options =
                if not (Seq.exists (fun (s: string) -> s.StartsWith "-r:" && s.Contains "FSharp.Core.dll") options.OtherOptions) then
                    { options with OtherOptions = FSharpCoreFix.ensureCorrectFSharpCore options.OtherOptions }
                else options
            let fsProject = { fsProject with Options = Some options }
            projects.[project] <- fsProject
            fsProject

    member private x.ProcessInvalidateOptions() =
        lock projectsToInvalidate (fun _ ->
            for p in projectsToInvalidate do
                if projects.ContainsKey(p) then
                    checkerService.InvalidateProject(projects.[p])
                    projects.Remove(p) |> ignore
            projectsToInvalidate.Clear())

    member private x.GetScriptOptions(file: IPsiSourceFile) =
        let path = file.GetLocation()
        let filePath = path.FullPath
        let source = file.Document.GetText()
        lock getScriptOptionsLock (fun _ ->
        let getScriptOptionsAsync = checkerService.Checker.GetProjectOptionsFromScript(filePath, source)
        try
            let options, errors = getScriptOptionsAsync.RunAsTask()
            if not errors.IsEmpty then
                Logger.Warn(logger, "Script options for {0}: {1}", filePath, concatErrors errors)
            let options = x.FixScriptOptions(options)
            Some options
        with
        | :? ProcessCancelledException -> reraise()
        | exn ->
            // todo: replace FCS reference resolver
            Logger.Warn(logger, "Error while getting options for {0}: {1}", filePath, exn.Message)
            Logger.LogExceptionSilently(logger, exn)
            None)

    member private x.FixScriptOptions(options) =
        { options with OtherOptions = FSharpCoreFix.ensureCorrectFSharpCore options.OtherOptions }

    member private x.InvalidateAll() =
        projects.Clear()
        projectsToInvalidate.Clear() // todo: check?

    interface IFSharpProjectOptionsProvider with
        member x.GetProjectOptions(file) =
            if fsFileService.IsScript(file) then x.GetScriptOptions(file) else
            if file.PsiModule.IsMiscFilesProjectModule() then None else

            lock projects (fun _ ->
                match x.GetFSharpProject(file) with
                | Some fsProject when fsProject.ContainsFile file -> fsProject.Options
                | _ -> None)

        member x.HasPairFile(file) =
            if fsFileService.IsScript(file) || file.PsiModule.IsMiscFilesProjectModule() then false else

            lock projects (fun _ ->
                match x.GetFSharpProject(file) with
                | Some fsProject -> fsProject.FilesWithPairs.Contains(file.GetLocation())
                | _ -> false)

        member x.GetParsingOptions(file) =
            if fsFileService.IsScript(file) || file.PsiModule.IsMiscFilesProjectModule() then
                Some { FSharpParsingOptions.Default with SourceFiles = Array.ofList [file.GetLocation().FullPath] }
            else
                lock projects (fun _ ->
                    match x.GetFSharpProject(file) with
                    | Some project when project.Options.IsSome ->
                        match project.ParsingOptions with
                        | None ->
                            let projectOptions = project.Options.Value
                            let parsingOptions, errors = checkerService.Checker.GetParsingOptionsFromCommandLineArgs(List.ofArray projectOptions.OtherOptions)
                            let parsingOptions = { parsingOptions with SourceFiles = projectOptions.SourceFiles }
                            if not errors.IsEmpty then
                                Logger.Warn(logger, "Getting parsing options: {0}", concatErrors errors)
                            project.ParsingOptions <- Some parsingOptions
                            Some parsingOptions
                        | options -> options
                    | _ -> None)
