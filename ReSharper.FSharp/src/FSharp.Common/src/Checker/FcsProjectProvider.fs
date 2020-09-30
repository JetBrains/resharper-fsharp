namespace JetBrains.ReSharper.Plugins.FSharp.Checker

open System.Collections.Generic
open FSharp.Compiler.SourceCodeServices
open JetBrains.Annotations
open JetBrains.Application.Settings
open JetBrains.Application.Threading
open JetBrains.Application.changes
open JetBrains.DataFlow
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Build
open JetBrains.ProjectModel.Tasks
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectItems.ItemsContainer
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.Psi.Files.SandboxFiles
open JetBrains.ReSharper.Psi.Modules
open JetBrains.Threading
open JetBrains.Util


[<AutoOpen>]
module FcsProjectProvider =
    let isProjectModule (psiModule: IPsiModule) =
        psiModule :? IProjectPsiModule 

    let getModuleProject (psiModule: IPsiModule) =
        psiModule.ContainingProjectModule.As<IProject>()

    let isFSharpProject (projectModelModule: IModule) =
        match projectModelModule with
        | :? IProject as project -> project.IsFSharp // todo: check `isOpened`?
        | _ -> false

    let isFSharpProjectModule (psiModule: IPsiModule) =
        psiModule.IsValid() && isFSharpProject psiModule.ContainingProjectModule


[<SolutionComponent>]
type FcsProjectProvider
        (lifetime: Lifetime, solution: ISolution, changeManager: ChangeManager, checkerService: FSharpCheckerService,
         fcsProjectBuilder: FcsProjectBuilder, scriptFcsProjectProvider: IScriptFcsProjectProvider,
         scheduler: ISolutionLoadTasksScheduler, fsFileService: IFSharpFileService, psiModules: IPsiModules,
         locks: IShellLocks, logger: ILogger, fileExtensions: IProjectFileExtensions) as this =
    inherit RecursiveProjectModelChangeDeltaVisitor()

    let locker = JetFastSemiReenterableRWLock()

    let fcsProjects = Dictionary<IPsiModule, FcsProject>()
    let referencedModules = Dictionary<IPsiModule, ReferencedModule>()

    // todo: keep standalone projects
//    let fcsStandaloneProjects = Dictionary<IPsiModule, FSharpProjectOptions>() // todo

    let dirtyModules = HashSet<IPsiModule>()
    let fcsProjectInvalidated = new Signal<IPsiModule>(lifetime, "FcsProjectInvalidated")

    let getReferencingModules (psiModule: IPsiModule) =
        match tryGetValue psiModule referencedModules with
        | None -> Seq.empty
        | Some referencedModule -> referencedModule.ReferencingModules :> _

    let rec invalidateFcsProject (psiModule: IPsiModule) =
        match tryGetValue psiModule fcsProjects with
        | None -> ()
        | Some fcsProject ->

        logger.Trace("Start invalidating project: {0}", psiModule)
        fcsProjectInvalidated.Fire(psiModule)

        // Invalidate FCS projects for the old project options, before creating new ones.
        // todo: try to not invalidate FCS project if a project is not changed actually?
        checkerService.InvalidateFcsProject(fcsProject.ProjectOptions)
        getReferencingModules psiModule |> Seq.iter invalidateFcsProject

        referencedModules.Remove(psiModule) |> ignore
        fcsProjects.Remove(psiModule) |> ignore
        dirtyModules.Remove(psiModule) |> ignore

        if not (psiModule.IsValid()) then
            let project = psiModule.ContainingProjectModule.As<IProject>()
            if isNotNull project then
                solution.GetComponent<FSharpItemsContainer>().RemoveProject(project)

        // todo: remove removed psiModules? (don't we remove them anyway?) (standalone projects only?)
        logger.Trace("Done invalidating project: {0}", psiModule)

    let processDirtyFcsProjects () =
        use lock = locker.UsingWriteLock()
        if dirtyModules.IsEmpty() then () else

        logger.Trace("Start invalidating dirty modules")
        let modulesToInvalidate = List(dirtyModules)
        for psiModule in modulesToInvalidate do
            invalidateFcsProject psiModule
        logger.Trace("Done invalidating dirty modules")

    do
        // Start listening for the changes after project model is updated.
        scheduler.EnqueueTask(SolutionLoadTask("FSharpProjectOptionsProvider", SolutionLoadTaskKinds.StartPsi, fun _ ->
            changeManager.Changed2.Advise(lifetime, this.ProcessChange)))

        checkerService.FcsProjectProvider <- this
        lifetime.OnTermination(fun _ -> checkerService.FcsProjectProvider <- Unchecked.defaultof<_>) |> ignore

    let tryGetFcsProject (psiModule: IPsiModule): FcsProject option =
        use lock = locker.UsingReadLock()
        tryGetValue psiModule fcsProjects

    let rec createFcsProject (project: IProject) (psiModule: IPsiModule): FcsProject =
        match tryGetValue psiModule fcsProjects with
        | Some fcsProject -> fcsProject
        | _ ->

        let fcsProject = fcsProjectBuilder.BuildFcsProject(psiModule, project)

        let referencedProjectPsiModules =
            getReferencedModules psiModule
            |> Seq.filter (fun psiModule ->
                psiModule.IsValid() && isProjectModule psiModule && psiModule.ContainingProjectModule != project)
            |> List

        let referencedFcsProjects =
            referencedProjectPsiModules
            |> Seq.filter isFSharpProjectModule
            |> Seq.map (fun referencedPsiModule ->
                let referencedProject = referencedPsiModule.ContainingProjectModule :?> _
                let referencedFcsProject = createFcsProject referencedProject referencedPsiModule
                referencedFcsProject.OutputPath.FullPath, referencedFcsProject.ProjectOptions)
            |> Seq.toArray

        let fcsProjectOptions = { fcsProject.ProjectOptions with ReferencedProjects = referencedFcsProjects }
        let fcsProject = { fcsProject with ProjectOptions = fcsProjectOptions }

        fcsProjects.[psiModule] <- fcsProject

        for referencedPsiModule in referencedProjectPsiModules do
            let referencedModule = referencedModules.GetOrCreateValue(referencedPsiModule, ReferencedModule.Create)
            referencedModule.ReferencingModules.Add(psiModule) |> ignore

        fcsProject

    let getOrCreateFcsProject (psiModule: IPsiModule): FcsProject option =
        match tryGetFcsProject psiModule with
        | Some _ as fcsProject -> fcsProject
        | _ ->

        match getModuleProject psiModule with
        | FSharpProject project ->
            use lock = locker.UsingWriteLock()
            let fcsProject = createFcsProject project psiModule
            Some fcsProject

        | _ -> None

    let getOrCreateFcsProjectForFile (sourceFile: IPsiSourceFile) =
        getOrCreateFcsProject sourceFile.PsiModule

    let isMiscModule (psiModule: IPsiModule) =
        psiModule.IsMiscFilesProjectModule()

    let isScriptLike file =
        fsFileService.IsScriptLike(file) || isMiscModule file.PsiModule || isNull (file.GetProject())        

    let getParsingOptionsForSingleFile ([<NotNull>] sourceFile: IPsiSourceFile) isScript =
        { FSharpParsingOptions.Default with
            SourceFiles = [| sourceFile.GetLocation().FullPath |]
            ConditionalCompilationDefines = ImplicitDefines.scriptDefines
            IsExe = isScript }

    let getProjectOptions (sourceFile: IPsiSourceFile) =
        let psiModule = sourceFile.PsiModule

        // Scripts belong to separate psi modules even when are in projects, project/misc module check is enough.
        if isProjectModule psiModule && not (isMiscModule psiModule) then
            match getOrCreateFcsProject psiModule with
            | Some fcsProject when fcsProject.IsKnownFile(sourceFile) -> Some fcsProject.ProjectOptions
            | _ -> None

        elif psiModule :? FSharpScriptPsiModule then
            scriptFcsProjectProvider.GetScriptOptions(sourceFile)

        elif psiModule :? SandboxPsiModule then
            let settings = sourceFile.GetSettingsStore()
            if not (settings.GetValue(fun (s: FSharpExperimentalFeatures) -> s.FsiInteractiveEditor)) then None else

            scriptFcsProjectProvider.GetScriptOptions(sourceFile)

        else
            None

    member x.FcsProjectInvalidated = fcsProjectInvalidated

    member private x.ProcessChange(obj: ChangeEventArgs) =
        if not (solution.IsValid()) then () else

        match obj.ChangeMap.GetChange<PsiModuleChange>(psiModules) with
        | null -> ()
        | change ->

        // todo: check if there's a change for a project module when referenced assembly module is changed?
        use lock = locker.UsingWriteLock()

        for moduleChange in change.ModuleChanges do
            // todo: ignore `PsiModuleChange.ChangeType.Invalidated`?
            if moduleChange.Type <> PsiModuleChange.ChangeType.Added then
                let psiModule = moduleChange.Item
                if isMiscModule psiModule then () else

                logger.Trace("Module change: type: {0}, module: {1}", moduleChange.Type, psiModule)
                dirtyModules.Add(psiModule) |> ignore

        for fileChange in change.FileChanges do
            let changeType = fileChange.Type
            if changeType <> PsiModuleChange.ChangeType.Invalidated then
                let sourceFile = fileChange.Item
                let projectFileType = fileExtensions.GetFileType(sourceFile.GetLocation().ExtensionWithDot)
                if not (projectFileType.Is<FSharpProjectFileType>()) then () else

                // todo: make it possible to distinguish fsi sandbox files from sandbox in diffs 
                let psiModule = sourceFile.PsiModule
                if isMiscModule psiModule && not (projectFileType.Is<FSharpScriptProjectFileType>()) then () else

                logger.Trace("File change: type {0}, name: {1}, module: {2}", changeType, sourceFile.Name, psiModule)
                dirtyModules.Add(psiModule) |> ignore

    interface IFcsProjectProvider with
        member x.GetProjectOptions(sourceFile) =
            locks.AssertReadAccessAllowed()
            processDirtyFcsProjects ()

            getProjectOptions sourceFile

        member x.HasPairFile(sourceFile) =
            locks.AssertReadAccessAllowed()
            processDirtyFcsProjects ()

            if isScriptLike sourceFile then false else

            match getOrCreateFcsProjectForFile sourceFile with
            | Some fsProject -> fsProject.ImplementationFilesWithSignatures.Contains(sourceFile.GetLocation())
            | _ -> false

        member x.GetParsingOptions(sourceFile) =
            locks.AssertReadAccessAllowed()
            processDirtyFcsProjects ()

            if isNull sourceFile then sandboxParsingOptions else
            if isScriptLike sourceFile then getParsingOptionsForSingleFile sourceFile true else

            match getOrCreateFcsProjectForFile sourceFile with
            | Some fsProject -> fsProject.ParsingOptions
            | _ -> getParsingOptionsForSingleFile sourceFile false

        member x.GetFileIndex(sourceFile) =
            locks.AssertReadAccessAllowed()
            processDirtyFcsProjects ()

            if isScriptLike sourceFile then 0 else

            getOrCreateFcsProjectForFile sourceFile
            |> Option.bind (fun fsProject ->
                let path = sourceFile.GetLocation()
                tryGetValue path fsProject.FileIndices)
            |> Option.defaultWith (fun _ -> -1)
        
        member x.ModuleInvalidated = x.FcsProjectInvalidated :> _

        member x.InvalidateReferencesToProject(project: IProject) =
            (false, project.GetPsiModules()) ||> Seq.fold (fun invalidated psiModule ->
                tryGetValue psiModule referencedModules
                |> Option.map (fun referencedModule ->
                    (invalidated, referencedModule.ReferencingModules)
                    ||> Seq.fold (fun invalidated psiModule -> dirtyModules.Add(psiModule) || invalidated))
                |> Option.defaultValue invalidated)

        member x.HasFcsProjects = not (fcsProjects.IsEmpty())

        member x.InvalidateDirty() =
            processDirtyFcsProjects ()

/// Invalidates psi caches when a non-F# project is built and FCS cached resolve results become stale
[<SolutionComponent>]
type OutputAssemblyChangeInvalidator
        (lifetime: Lifetime, outputAssemblies: OutputAssemblies, daemon: IDaemon, psiFiles: IPsiFiles,
         fcsProjectProvider: IFcsProjectProvider, scheduler: ISolutionLoadTasksScheduler) =
    do
        scheduler.EnqueueTask(SolutionLoadTask("FSharpProjectOptionsProvider", SolutionLoadTaskKinds.StartPsi, fun _ ->
            // todo: track file system changes instead? This currently may be triggered on a project model change too.
            outputAssemblies.ProjectOutputAssembliesChanged.Advise(lifetime, fun (project: IProject) ->
                if not fcsProjectProvider.HasFcsProjects || project.IsFSharp then () else

                if fcsProjectProvider.InvalidateReferencesToProject(project) then
                    psiFiles.IncrementModificationTimestamp(null) // Drop cached values.
                    daemon.Invalidate() // Request files re-highlighting.
            )
        ))
