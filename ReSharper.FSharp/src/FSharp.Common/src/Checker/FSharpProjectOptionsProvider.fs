namespace rec JetBrains.ReSharper.Plugins.FSharp.Common.Checker

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
open JetBrains.Metadata.Reader.API
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
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectItems.ItemsContainer
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.Threading
open JetBrains.Util
open Microsoft.FSharp.Compiler
open Microsoft.FSharp.Compiler.ErrorLogger
open Microsoft.FSharp.Compiler.SourceCodeServices

[<SolutionComponent>]
type FSharpProjectOptionsProvider
        (lifetime, solution: ISolution, changeManager: ChangeManager, checkerService: FSharpCheckerService,
         optionsBuilder: FSharpProjectOptionsBuilder, scriptOptionsProvider: FSharpScriptOptionsProvider,
         fsFileService: IFSharpFileService, referenceResolveStore: ModuleReferencesResolveStore, logger: ILogger,
         psiModules: IPsiModules, psiModulesResolveContextManager: PsiModuleResolveContextManager) as this =
    inherit RecursiveProjectModelChangeDeltaVisitor()

    let scriptDefines = ["INTERACTIVE"]

    let invalidatingProjectChangeType =
        ProjectModelChangeType.PROPERTIES ||| ProjectModelChangeType.TARGET_FRAMEWORK

    let invalidatingChildChangeType =
        ProjectModelChangeType.ADDED ||| ProjectModelChangeType.REMOVED |||
        ProjectModelChangeType.MOVED_IN ||| ProjectModelChangeType.MOVED_OUT |||
        ProjectModelChangeType.REFERENCE_TARGET

    let projects = Dictionary<IProject, Dictionary<TargetFrameworkId, FSharpProject>>()
    let checker = checkerService.Checker
    let locker = JetFastSemiReenterableRWLock()
    do
        changeManager.Changed2.Advise(lifetime, this.ProcessChange)
        checkerService.OptionsProvider <- this
        lifetime.AddAction(fun _ -> checkerService.OptionsProvider <- null) |> ignore

    let tryGetFSharpProject (project: IProject) (targetFrameworkId: TargetFrameworkId) =
        use lock = locker.UsingReadLock()
        tryGetValue project projects 
        |> Option.bind (tryGetValue targetFrameworkId)

    let rec createFSharpProject (project: IProject) (psiModule: IPsiModule) =
        let targetFrameworkId = psiModule.TargetFrameworkId
        let fsProjectsForProject = projects.GetOrCreateValue(project, fun () -> Dictionary())
        fsProjectsForProject.GetOrCreateValue(targetFrameworkId, fun () ->
            logger.Info("Creating options for {0} {1}", project, psiModule)
            let fsProject = optionsBuilder.BuildSingleProjectOptions(project, psiModule)

            let referencedProjectsOptions = seq {
                let resolveContext =
                    psiModulesResolveContextManager
                        .GetOrCreateModuleResolveContext(project, psiModule, targetFrameworkId)

                let referencedProjectsPsiModules =
                    psiModules.GetModuleReferences(psiModule, resolveContext)
                    |> Seq.choose (fun reference ->
                        match reference.Module.ContainingProjectModule with
                        | FSharpProject project when project.IsOpened -> Some reference.Module
                        | _ -> None)

                for referencedPsiModule in referencedProjectsPsiModules do
                    let project = referencedPsiModule.ContainingProjectModule :?> IProject
                    let outPath = project.GetOutputFilePath(referencedPsiModule.TargetFrameworkId).FullPath
                    let fsProject = createFSharpProject project referencedPsiModule
                    yield outPath, fsProject.Options }

            let options = { fsProject.Options with ReferencedProjects = Array.ofSeq referencedProjectsOptions }
            let fsProject = { fsProject with Options = options }
            fsProject)

    let getOrCreateFSharpProject (file: IPsiSourceFile) =
        match file.GetProject() with
        | FSharpProject project ->
            let psiModule = file.PsiModule
            tryGetFSharpProject project psiModule.TargetFrameworkId
            |> Option.orElseWith (fun _ ->
                use lock = locker.UsingWriteLock()
                Some (createFSharpProject project psiModule))
        | _ -> None

    let rec invalidateProject (project: IProject) =
        logger.Info("Invalidating {0}", project)
        tryGetValue project projects
        |> Option.iter (fun fsProjectsForProject ->
            for fsProject in fsProjectsForProject.Values do
                checker.InvalidateConfiguration(fsProject.Options, false)
            fsProjectsForProject.Clear())

        // todo: keep referencing project for invalidating removed projects
        let referencesToProject = referenceResolveStore.GetReferencesToProject(project)
        if not (referencesToProject.IsEmpty()) then
            logger.Info("Invalidatnig reverencing projects")
            for reference in referencesToProject do
                match reference.GetProject() with
                | FSharpProject project -> invalidateProject project
                | _ -> ()
            logger.Info("Done invalidating {0}", project)


    let isScriptLike file =
        fsFileService.IsScriptLike(file) || file.PsiModule.IsMiscFilesProjectModule() || isNull (file.GetProject())        

    let getParsingOptionsForSingleFile (file: IPsiSourceFile) =
        { FSharpParsingOptions.Default with SourceFiles = [| file.GetLocation().FullPath |] }

    member private x.ProcessChange(obj: ChangeEventArgs) =
        match obj.ChangeMap.GetChange<ProjectModelChange>(solution) with
        | null -> ()
        | change ->
            if not change.IsClosingSolution then
                use lock = locker.UsingWriteLock()
                x.VisitDelta(change)

    override x.VisitDelta(change: ProjectModelChange) =
        match change.ProjectModelElement with
        | :? IProject as project ->
            if project.IsFSharp then
                if change.ContainsChangeType(invalidatingProjectChangeType) then
                    invalidateProject project

                else if change.IsSubtreeChanged then
                    let mutable shouldInvalidate = false
                    let changeVisitor =
                        { new RecursiveProjectModelChangeDeltaVisitor() with
                            member x.VisitDelta(change) =
                                if change.ContainsChangeType(invalidatingChildChangeType) then
                                    shouldInvalidate <- true

                                if not shouldInvalidate then
                                    base.VisitDelta(change) }

                    change.Accept(changeVisitor)
                    if shouldInvalidate then
                        invalidateProject project
    
                if change.IsRemoved then
                    let projectMark = project.GetProjectMark()
                    solution.GetComponent<FSharpItemsContainer>().RemoveProject(projectMark)
                    projects.Remove(project) |> ignore
                    logger.Info("Removing {0}", project)

            else if project.ProjectProperties.ProjectKind = ProjectKind.SOLUTION_FOLDER then
                base.VisitDelta(change)

        | :? ISolution -> base.VisitDelta(change)
        | _ -> ()

    interface IFSharpProjectOptionsProvider with
        member x.GetProjectOptions(file) =
            if fsFileService.IsScriptLike(file) then scriptOptionsProvider.GetScriptOptions(file) else
            if file.PsiModule.IsMiscFilesProjectModule() then None else

            getOrCreateFSharpProject file
            |> Option.map (fun fsProject -> fsProject.Options)

        member x.HasPairFile(file) =
            if isScriptLike file then false else

            getOrCreateFSharpProject file
            |> Option.map (fun fsProject -> fsProject.FilesWithPairs.Contains(file.GetLocation()))
            |> Option.defaultValue false

        member x.GetParsingOptions(file) =
            if isScriptLike file then { getParsingOptionsForSingleFile file with IsExe = true } else

            getOrCreateFSharpProject file
            |> Option.map (fun fsProject -> fsProject.ParsingOptions)
            |> Option.defaultWith (fun _ -> getParsingOptionsForSingleFile file)


[<SolutionComponent>]
type FSharpScriptOptionsProvider(logger: ILogger, checkerService: FSharpCheckerService) =
    let getScriptOptionsLock = obj()

    member x.GetScriptOptions(file: IPsiSourceFile) =
        let path = file.GetLocation()
        let filePath = path.FullPath
        let source = file.Document.GetText()
        lock getScriptOptionsLock (fun _ ->
        let getScriptOptionsAsync = checkerService.Checker.GetProjectOptionsFromScript(filePath, source)
        try
            let options, errors = getScriptOptionsAsync.RunAsTask()
            if not errors.IsEmpty then
                logger.Warn("Script options for {0}: {1}", filePath, concatErrors errors)
            let options = x.FixScriptOptions(options)
            Some options
        with
        | :? OperationCanceledException -> reraise()
        | exn ->
            logger.Warn("Error while getting script options for {0}: {1}", filePath, exn.Message)
            logger.LogExceptionSilently(exn)
            None)

    member private x.FixScriptOptions(options) =
        { options with OtherOptions = FSharpCoreFix.ensureCorrectFSharpCore options.OtherOptions }
