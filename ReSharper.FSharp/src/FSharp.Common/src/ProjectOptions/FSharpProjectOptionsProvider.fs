namespace JetBrains.ReSharper.Plugins.FSharp.Common.ProjectOptions

open System
open System.Collections.Generic
open System.Linq
open System.Threading
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
open JetBrains.ProjectModel.Properties
open JetBrains.ProjectModel.Properties.CSharp
open JetBrains.ProjectModel.Properties.Managed
open JetBrains.ReSharper.Daemon.Impl
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.Plugins.FSharp.Common
open JetBrains.ReSharper.Plugins.FSharp.Common.CheckerService
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.Util
open Microsoft.FSharp.Compiler.SourceCodeServices

[<SolutionComponent>]
type FSharpProjectOptionsProvider(lifetime, logger : ILogger, solution : ISolution, shellLocks : ShellLocks,
                                  checkerService : FSharpCheckerService, optionsBuilder : FSharpProjectOptionsBuilder,
                                  changeManager : ChangeManager) as this =
    inherit RecursiveProjectModelChangeDeltaVisitor()

    let projects = Dictionary<IProject, IDisposable>() // keeps FCS project alive until disposed
    let projectsOptions = Dictionary<IProject, FSharpProjectOptions>()
    let projectsToInvalidate = JetHashSet<IProject>()

    let scriptsOptions = Dictionary<string, FSharpProjectOptions>()
    let configurationDefines = Dictionary<IProject, string list>()
    let taskHost = shellLocks.Tasks
    let mutable cts : CancellationTokenSource = null
    let isRunningOnMono = PlatformUtil.IsRunningOnMono

    do
        changeManager.Changed2.Advise(lifetime, this.ProcessChange);
        checkerService.OptionsProvider <- this

    member private x.ProcessChange(obj : ChangeEventArgs) =
        let change = obj.ChangeMap.GetChange<ProjectModelChange>(solution)
        if isNotNull change then x.VisitDelta change

    override x.VisitDelta(change : ProjectModelChange) =
        if change.IsClosingSolution then x.InvalidateAll()
        else
            match change.ProjectModelElement with
            | :? IProject as project when x.IsApplicable(project.ProjectProperties) ->
                if change.IsRemoved then
                    if projects.contains project then projects.[project].Dispose()
                    projects.remove project
                    configurationDefines.remove project
                    projectsToInvalidate.remove project
                else
                    use cookie = ReadLockCookie.Create()
                    projectsToInvalidate.add project
                    x.InvalidateReferencingProjects(project)
            | _ -> base.VisitDelta change

    member private x.IsApplicable([<NotNull>] properties : IProjectProperties) =
        match properties with
        | :? FSharpProjectProperties -> true
        | :? ProjectKCSharpProjectProperties as coreProperties ->
            // todo: remove when ProjectK properties replaced with DotNetCoreProjectFlavour
            coreProperties.ProjectTypeGuids.Contains(FSharpProjectPropertiesFactory.FSharpProjectTypeGuid)
        | _ -> false

    member private x.InvalidateReferencingProjects(project) =
        for p in project.GetReferencingProjects(project.GetCurrentTargetFrameworkId()) do
            if x.IsApplicable(project.ProjectProperties) then
                projectsToInvalidate.Add(p) |> ignore
                x.InvalidateReferencingProjects(p)

    member private x.GetProjectOptionsAux(file : IPsiSourceFile, checker : FSharpChecker) =
        match file.ToProjectFile() with
        | projectFile when
            isNotNull projectFile &&
            projectFile.LanguageType.Equals(FSharpProjectFileType.Instance) &&
            projectFile.Properties.BuildAction.IsCompile() ->
                match file.GetProject() with
                | project when isNotNull project && project.IsOpened ->
                    x.ProcessInvalidateOptions(checker)
                    use cookie = ReadLockCookie.Create()
                    Some (x.GetOrCreateProjectOptions(project, checker))
                | _ -> None
        | _ -> None

    member private x.GetOrCreateProjectOptions(project, checker) =
        if projectsOptions.contains project then projectsOptions.[project]
        else
            let options, defines = optionsBuilder.BuildSingleProjectOptions(project)
            configurationDefines.[project] <- defines

            let framework = project.GetCurrentTargetFrameworkId()
            let referencedProjectsOptions =
                seq { for p in project.GetReferencedProjects(framework, transitive = false) do
                          if p.IsOpened && x.IsApplicable(p.ProjectProperties) then
                              let outPath = p.GetOutputFilePath(p.GetCurrentTargetFrameworkId()).FullPath
                              yield outPath, x.GetOrCreateProjectOptions(p, checker) }

            let options' = { options with ReferencedProjects = referencedProjectsOptions.ToArray() }
            projects.[project] <- checker.KeepProjectAlive(options').RunAsTask()
            projectsOptions.[project] <- options'
            options'

    member private x.ProcessInvalidateOptions(checker) =
        lock projectsToInvalidate (fun _ ->
            for p in projectsToInvalidate do
                if projectsOptions.contains p then checker.InvalidateConfiguration(projectsOptions.[p])
                if projects.contains p then projects.[p].Dispose()
                configurationDefines.remove p
                projectsOptions.remove p
                projects.remove p
            projectsToInvalidate.Clear())

    member private x.GetScriptOptionsAux(file : IPsiSourceFile, checker : FSharpChecker, shouldTryUpdate : bool) =
        let filePath = file.GetLocation().FullPath
        let source = file.Document.GetText()
        let loadTime = DateTime.Now
        let getScriptOptionsAsync = checker.GetProjectOptionsFromScript(filePath, source, loadTime)
        if not (scriptsOptions.ContainsKey(filePath)) then
            try
                let options, errors = getScriptOptionsAsync.RunAsTask()
                if not errors.IsEmpty then logger.LogFSharpErrors("Script options for " + filePath) errors

                scriptsOptions.[filePath] <- options
                Some options // todo: fix options on mono
            with
            | :? ProcessCancelledException -> reraise()
            | exn ->
                // todo: replace FCS reference resolver
                let message = sprintf "Getting options for %s: %s" filePath exn.Message
                logger.LogMessage(LoggingLevel.WARN, message)
                None
        else
            // todo: check working
            let existingOptions = scriptsOptions.[filePath]
            if shouldTryUpdate then
                if isNotNull cts then cts.Cancel()
                cts <- new CancellationTokenSource()
                taskHost.StartNew(lifetime, Scheduling.FreeThreaded, fun () ->
                    try
                        let cancellationToken = cts.Token
                        let options, errors = getScriptOptionsAsync |> Async.RunSynchronously
                        if not errors.IsEmpty then
                            logger.LogFSharpErrors("Script options for " + filePath) errors

                        if not (ArrayUtil.Equals(options.OtherOptions, existingOptions.OtherOptions)) then
                            scriptsOptions.[filePath] <- options
                            use cookie = ReadLockCookie.Create()
                            file.GetSolution().GetComponent<DaemonImpl>().ForceReHighlight(file.Document) |> ignore
                        cts <- null
                    with
                    | :? ProcessCancelledException -> reraise()
                    | exn ->
                        // todo: replace FCS reference resolver
                        let msg = sprintf "Getting options for %s: %s" filePath exn.Message
                        logger.LogMessage(LoggingLevel.WARN, msg)) |> ignore
            Some existingOptions

    member private x.InvalidateAll() =
        for p in projects.Values do p.Dispose()
        projects.Clear()
        projectsOptions.Clear()
        scriptsOptions.Clear()
        configurationDefines.Clear()
        projectsToInvalidate.Clear()

    member private x.GetDefinesAux(sourceFile : IPsiSourceFile) =
        match sourceFile.GetProject() with
        | project when isNotNull project && configurationDefines.ContainsKey(project) ->
            configurationDefines.[project]
        | _ -> List.empty

    interface IFSharpProjectOptionsProvider with
        member x.GetProjectOptions(file, checker, updateScriptOptions) =
            if file.PsiModule.IsMiscFilesProjectModule() then None
            else
                if file.LanguageType.Equals(FSharpScriptProjectFileType.Instance)
                then x.GetScriptOptionsAux(file, checker, updateScriptOptions)
                else x.GetProjectOptionsAux(file, checker)

        member x.GetDefines(sourceFile : IPsiSourceFile) =
            x.GetDefinesAux(sourceFile)
