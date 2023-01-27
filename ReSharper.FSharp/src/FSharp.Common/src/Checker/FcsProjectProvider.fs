namespace JetBrains.ReSharper.Plugins.FSharp.Checker

open System.Collections.Generic
open System.IO
open FSharp.Compiler.AbstractIL.ILBinaryReader
open FSharp.Compiler.CodeAnalysis
open JetBrains.Annotations
open JetBrains.Application.Settings
open JetBrains.Application.Threading
open JetBrains.Application.changes
open JetBrains.DataFlow
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Build
open JetBrains.ProjectModel.ProjectsHost
open JetBrains.ProjectModel.Tasks
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Host.ProjectItems.ItemsContainer
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader
open JetBrains.ReSharper.Plugins.FSharp.Shim.TypeProviders
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.Psi.Files.SandboxFiles
open JetBrains.ReSharper.Psi.Modules
open JetBrains.Util

[<AutoOpen>]
module FcsProjectProvider =
    let isProjectModule (psiModule: IPsiModule) =
        psiModule :? IProjectPsiModule

    let isMiscModule (psiModule: IPsiModule) =
        psiModule.IsMiscFilesProjectModule()

    let isFSharpProject (projectModelModule: IModule) =
        match projectModelModule with
        | :? IProject as project -> project.IsFSharp // todo: check `isOpened`?
        | _ -> false

    let isFSharpProjectModule (psiModule: IPsiModule) =
        psiModule.IsValid() && isFSharpProject psiModule.ContainingProjectModule // todo: remove isValid check?

    let [<Literal>] invalidateProjectChangeType =
        ProjectModelChangeType.PROPERTIES ||| ProjectModelChangeType.TARGET_FRAMEWORK |||
        ProjectModelChangeType.REFERENCE_TARGET ||| ProjectModelChangeType.REMOVED

    let [<Literal>] invalidateChildChangeType =
        ProjectModelChangeType.ADDED ||| ProjectModelChangeType.REMOVED |||
        ProjectModelChangeType.MOVED_IN ||| ProjectModelChangeType.MOVED_OUT |||
        ProjectModelChangeType.REFERENCE_TARGET

[<SolutionComponent>]
type FcsProjectProvider(lifetime: Lifetime, solution: ISolution, changeManager: ChangeManager,
        checkerService: FcsCheckerService, fcsProjectBuilder: FcsProjectBuilder,
        scriptFcsProjectProvider: IScriptFcsProjectProvider, scheduler: ISolutionLoadTasksScheduler,
        fsFileService: IFSharpFileService, fsItemsContainer: FSharpItemsContainer,
        modulePathProvider: ModulePathProvider, locks: IShellLocks, logger: ILogger,
        fcsAssemblyReaderShim: IFcsAssemblyReaderShim) as this =
    inherit RecursiveProjectModelChangeDeltaVisitor()

    /// The main cache for FCS project model and related things.
    let fcsProjects = Dictionary<IPsiModule, FcsProject>()

    /// Fcs projects with no references to other projects and assemblies.
    ///
    /// Can be used for parsing and getting info relevant to caching (file index, has fsi file),
    /// when project is changed during project model modification,
    /// but references aren't ready yet, e.g. during file rename.
    ///
    /// FcsProject is removed from this map when building full project with (new) references.
    let fcsProjectsWithoutReferences = Dictionary<IPsiModule, FcsProject>()

    let referencedModules = Dictionary<IPsiModule, ReferencedModule>()

    /// Psi modules known to Fcs project model as either an F# project or a supported referenced project output
    let projectsPsiModules = OneToSetMap<IModule, IPsiModule>()

    /// Used to synchronize project model changes with FSharpItemsContainer
    let projectsProjectMarks = Dictionary<IProjectMark, IProject>()

    /// Used to synchronize project model changes with FSharpItemsContainer
    let projectMarkModules = Dictionary<IPsiModule, IProjectMark>()
    let outputPathToPsiModule = Dictionary<VirtualFileSystemPath, IPsiModule>()

    /// Bool value forces FCS invalidation even when project options are not changed.
    /// FCS is not trying to check previously not-found assemblies after a builder creation.
    /// When assembly module reader is disabled, we invalidate FCS when referenced C# project output is changed.
    let dirtyModules = Dictionary<IPsiModule, bool>()
    let fcsProjectInvalidated = new Signal<IPsiModule * FcsProject>("FcsProjectInvalidated")

    let getReferencingModules (psiModule: IPsiModule) =
        match tryGetValue psiModule referencedModules with
        | None -> Seq.empty
        | Some referencedModule -> referencedModule.ReferencingModules :> _

    let rec invalidateFcsProject
            forceInvalidateFcs
            (deletedProjects: IDictionary<string, IPsiModule * FcsProject * bool>)
            (psiModule: IPsiModule) =
        logger.Trace("Start invalidating referencing modules of psiModule: {0}", psiModule)

        getReferencingModules psiModule |> Seq.iter (invalidateFcsProject forceInvalidateFcs deletedProjects)
        referencedModules.Remove(psiModule) |> ignore
        projectsPsiModules.Remove(psiModule.ContainingProjectModule, psiModule) |> ignore

        logger.Trace("Done invalidating referencing modules of psiModule: {0}", psiModule)

        fcsProjectsWithoutReferences.Remove(psiModule) |> ignore

        match tryGetValue psiModule fcsProjects with
        | None -> ()
        | Some fcsProject ->
            logger.Trace("Start invalidating FcsProject: {0}", psiModule)

            deletedProjects[psiModule.GetPersistentID()] <- psiModule, fcsProject, forceInvalidateFcs

            for referencedPsiModule in fcsProject.ReferencedModules do
                match tryGetValue referencedPsiModule referencedModules with
                | None -> ()
                | Some referencedModule -> referencedModule.ReferencingModules.Remove(referencedPsiModule) |> ignore

            fcsProjects.Remove(psiModule) |> ignore

            match tryGetValue psiModule projectMarkModules with
            | None -> ()
            | Some projectMark -> projectsProjectMarks.Remove(projectMark) |> ignore

            projectMarkModules.Remove(psiModule) |> ignore
            outputPathToPsiModule.Remove(fcsProject.OutputPath) |> ignore

            // todo: remove removed psiModules? (don't we remove them anyway?) (standalone projects only?)
            logger.Trace("Done invalidating FcsProject: {0}", psiModule)

        dirtyModules.Remove(psiModule) |> ignore

    let areSameForChecking (newProject: FcsProject) (oldProject: FcsProject) =
        let getReferencedProjectOutputs (options: FSharpProjectOptions) =
            options.ReferencedProjects |> Array.map (fun project -> project.OutputFile)

        let newOptions = newProject.ProjectOptions
        let oldOptions = oldProject.ProjectOptions

        newOptions.ProjectFileName = oldOptions.ProjectFileName &&
        newOptions.SourceFiles = oldOptions.SourceFiles &&
        newOptions.OtherOptions = oldOptions.OtherOptions &&
        getReferencedProjectOutputs newOptions = getReferencedProjectOutputs oldOptions &&

        // todo: referenced project options public in FCS
        oldOptions.ReferencedProjects
        |> Array.forall (fun project ->
            let path = VirtualFileSystemPath.TryParse(project.OutputFile, InteractionContext.SolutionContext)
            not path.IsEmpty &&

            match tryGetValue path outputPathToPsiModule with
            | None -> fcsAssemblyReaderShim.IsKnownModule(path)
            | Some referencedPsiModule ->

            match tryGetValue referencedPsiModule fcsProjects with
            | None -> false
            | Some referencedFcsProject ->

            match referencedFcsProject.ProjectOptions.Stamp, oldProject.ProjectOptions.Stamp with
            | Some referencedStamp, Some oldStamp -> referencedStamp < oldStamp
            | _ -> false
        )

    do
        // Start listening for the changes after project model is ready.
        scheduler.EnqueueTask(SolutionLoadTask("FSharpProjectOptionsProvider", SolutionLoadTaskKinds.StartPsi, fun _ ->
            changeManager.Changed2.Advise(lifetime, this.ProcessChange)
            fsItemsContainer.FSharpProjectLoaded.Advise(lifetime, this.ProcessFSharpProjectLoaded)))

        checkerService.FcsProjectProvider <- this
        lifetime.OnTermination(fun _ -> checkerService.FcsProjectProvider <- Unchecked.defaultof<_>) |> ignore

    let tryGetFcsProject (psiModule: IPsiModule): FcsProject option =
        use lock = FcsReadWriteLock.ReadCookie.Create()
        tryGetValue psiModule fcsProjects

    let tryGetFcsProjectWithoutReferences (psiModule: IPsiModule): FcsProject option =
        use lock = FcsReadWriteLock.ReadCookie.Create()
        tryGetValue psiModule fcsProjectsWithoutReferences

    let createReferencedModule psiModule =
        ReferencedModule.create modulePathProvider psiModule

    let createFcsProjectWithoutReferences psiModule: FcsProject =
        use lock = FcsReadWriteLock.WriteCookie.Create()
        let fcsProject = fcsProjectBuilder.BuildFcsProject(psiModule, psiModule.ContainingProjectModule.As())
        fcsProjectsWithoutReferences[psiModule] <- fcsProject
        projectsPsiModules.Add(psiModule.ContainingProjectModule, psiModule) |> ignore
        fcsProject

    let createOrRecoverFcsProject (project: IProject) (psiModule: IPsiModule) (recentlyDeletedProjects: IDictionary<string, IPsiModule * FcsProject * bool>): FcsProject =
        psiModule.AssertIsValid()

        match tryGetValue psiModule fcsProjects with
        | Some fcsProject -> fcsProject
        | _ ->

        let projectsToCreate = Stack()
        projectsToCreate.Push(psiModule, project, None)

        while projectsToCreate.Count > 0 do
            let psiModule, project, processedReferences = projectsToCreate.Pop()
            match processedReferences with
            | None ->
                let referencedPsiModules =
                    getReferencedModules psiModule
                    |> Seq.filter (fun psiModule ->
                        psiModule.IsValid() && psiModule.ContainingProjectModule != project)
                    |> Seq.toList

                projectsToCreate.Push(psiModule, project, Some(referencedPsiModules))

                referencedPsiModules |> Seq.iter (fun referencedPsiModule ->
                    if not (isFSharpProjectModule referencedPsiModule) then () else
                    if fcsProjects.ContainsKey(referencedPsiModule) then () else

                    let referencedProject = referencedPsiModule.ContainingProjectModule :?> _
                    projectsToCreate.Push(referencedPsiModule, referencedProject, None))

            | Some referencedPsiModules ->
                if fcsProjects.ContainsKey(psiModule) then () else

                let fcsProject =
                    match tryGetValue psiModule fcsProjectsWithoutReferences with
                    | None -> fcsProjectBuilder.BuildFcsProject(psiModule, project)
                    | Some fcsProject ->

                    fcsProjectsWithoutReferences.Remove(psiModule) |> ignore
                    fcsProject

                let fcsProject = fcsProjectBuilder.AddReferences(fcsProject, referencedPsiModules)

                let referencedProjectPsiModules = referencedPsiModules |> Seq.filter isProjectModule

                for referencedPsiModule in referencedProjectPsiModules do
                    let referencedModule = referencedModules.GetOrCreateValue(referencedPsiModule, createReferencedModule)
                    referencedModule.ReferencingModules.Add(psiModule) |> ignore

                let referencedFcsProjects = 
                    referencedProjectPsiModules
                    |> Seq.choose (fun psiModule ->
                        if isFSharpProjectModule psiModule then
                            let referencedFcsProject = fcsProjects[psiModule]
                            let path = referencedFcsProject.OutputPath.FullPath
                            Some(FSharpReferencedProject.CreateFSharp(path, referencedFcsProject.ProjectOptions))

                        elif fcsAssemblyReaderShim.IsEnabled && AssemblyReaderShim.isSupportedModule psiModule then
                            match fcsAssemblyReaderShim.GetModuleReader(psiModule) with
                            | ReferencedAssembly.Ignored _ -> None
                            | ReferencedAssembly.ProjectOutput(reader, _) ->

                            projectsPsiModules.Add(psiModule.ContainingProjectModule, psiModule) |> ignore 

                            let referencedModule = referencedModules[psiModule]
                            let path = referencedModule.ReferencedPath.FullPath

                            let getTimestamp () = reader.Timestamp
                            let getReader () = reader :> ILModuleReader

                            Some(FSharpReferencedProject.CreateFromILModuleReader(path, getTimestamp, getReader))

                        else
                            None)
                    |> Seq.toArray

                let fcsProjectOptions = { fcsProject.ProjectOptions with ReferencedProjects = referencedFcsProjects }
                let fcsProject = { fcsProject with ProjectOptions = fcsProjectOptions }

                if logger.IsTraceEnabled() then
                    use writer = new StringWriter()
                    fcsProject.TestDump(writer)
                    logger.Trace($"Creating FCS project:\n{writer.ToString()}" )

                let tryRecoverProjectStamp () =
                    let moduleId = psiModule.GetPersistentID()
                    match tryGetValue moduleId recentlyDeletedProjects with
                    | None -> fcsProject
                    | Some(_, deletedProject, _) ->

                    if areSameForChecking fcsProject deletedProject then
                        let newStamp = fcsProject.ProjectOptions.Stamp
                        let oldStamp = deletedProject.ProjectOptions.Stamp
                        logger.Trace($"Recover FCS project stamp: {newStamp.Value} -> {oldStamp.Value}")

                        let newOptions = { fcsProject.ProjectOptions with Stamp = oldStamp }
                        { fcsProject with ProjectOptions = newOptions }
                    else
                        fcsProject

                let fcsProject = tryRecoverProjectStamp ()

                fcsProjects[psiModule] <- fcsProject
                outputPathToPsiModule[fcsProject.OutputPath] <- psiModule

                projectsPsiModules.Add(project, psiModule) |> ignore

                let projectMark = project.GetProjectMark()
                projectsProjectMarks[projectMark] <- project
                projectMarkModules[psiModule] <- projectMark

        fcsProjects[psiModule]

    let createFcsProject (project: IProject) (psiModule: IPsiModule): FcsProject =
        createOrRecoverFcsProject project psiModule EmptyDictionary.Instance

    let getOrCreateFcsProject (psiModule: IPsiModule): FcsProject option =
        match tryGetFcsProject psiModule with
        | Some _ as fcsProject -> fcsProject
        | _ ->

        match getModuleProject psiModule with
        | FSharpProject project ->
            use lock = FcsReadWriteLock.WriteCookie.Create()
            let fcsProject = createFcsProject project psiModule
            Some fcsProject

        | _ ->
            match psiModule with
            | :? FSharpScriptPsiModule as scriptModule ->
                let path = scriptModule.Path
                let sourceFile = scriptModule.SourceFile
                match scriptFcsProjectProvider.GetScriptOptions(sourceFile) with
                | None -> None
                | Some projectOptions ->

                let parsingOptions = 
                    { FSharpParsingOptions.Default with
                        SourceFiles = [| sourceFile.GetLocation().FullPath |]
                        ConditionalDefines = ImplicitDefines.scriptDefines
                        IsInteractive = true
                        IsExe = true }

                let indices = Dictionary()

                { OutputPath = path
                  ProjectOptions = projectOptions
                  ParsingOptions = parsingOptions
                  FileIndices = indices
                  ImplementationFilesWithSignatures = EmptySet.Instance
                  ReferencedModules = EmptySet.Instance }
                |> Some

            | _ ->
                None

    let getOrCreateFcsProjectForParsing (sourceFile: IPsiSourceFile): FcsProject =
        match tryGetFcsProject sourceFile.PsiModule with
        | Some fcsProject -> fcsProject
        | _ ->

        match tryGetFcsProjectWithoutReferences sourceFile.PsiModule with
        | Some fcsProject -> fcsProject
        | _ ->

        createFcsProjectWithoutReferences sourceFile.PsiModule

    /// Try to reuse the unique stamp from existing fcs project options
    /// and to not invalidate FCS project when it is the same for checking.
    let tryRecoverFcsProjects (deletedProjects: IDictionary<string, IPsiModule * FcsProject * bool>) =
        for KeyValue(moduleId, (deletedPsiModule, deletedFcsProject, forceInvalidateFcs)) in List.ofSeq deletedProjects do
            let psiModule = 
                if deletedPsiModule.IsValid() then
                    deletedPsiModule
                else
                    solution.PsiModules().GetById(deletedPsiModule.GetPersistentID())

            if isNull psiModule then () else

            let project = psiModule.ContainingProjectModule.As<IProject>()
            if isNull project then () else

            let fcsProject = createOrRecoverFcsProject project psiModule deletedProjects
            if not forceInvalidateFcs && fcsProject.ProjectOptions.Stamp = deletedFcsProject.ProjectOptions.Stamp then
                deletedProjects.Remove(moduleId) |> ignore

    let processDirtyFcsProjects () =
        use lock = FcsReadWriteLock.WriteCookie.Create()
        if dirtyModules.IsEmpty() then () else

        logger.Trace("Start invalidating dirty modules")

        let deletedProjects = Dictionary()
        let modulesToInvalidate = List(dirtyModules)
        for KeyValue(psiModule, forceInvalidateFcs) in modulesToInvalidate do
            invalidateFcsProject forceInvalidateFcs deletedProjects psiModule

        tryRecoverFcsProjects deletedProjects

        for KeyValue(_, (psiModule, fcsProject, _)) in deletedProjects do
            fcsProjectInvalidated.Fire((psiModule, fcsProject))
            fcsAssemblyReaderShim.InvalidateModule(psiModule)
            checkerService.InvalidateFcsProject(fcsProject.ProjectOptions)

        logger.Trace("Done invalidating dirty modules")

    let isScriptLike (file: IPsiSourceFile) =
        not file.Properties.ProvidesCodeModel ||
        fsFileService.IsScriptLike(file) ||
        isMiscModule file.PsiModule ||
        isNull (file.GetProject())

    let getParsingOptionsForSingleFile ([<NotNull>] sourceFile: IPsiSourceFile) isScript =
        { FSharpParsingOptions.Default with
            SourceFiles = [| sourceFile.GetLocation().FullPath |]
            ConditionalDefines = ImplicitDefines.scriptDefines
            IsInteractive = isScript
            IsExe = isScript }

    let markDirty forceInvalidateFcs psiModule =
        match tryGetValue psiModule dirtyModules with
        | None ->
            dirtyModules[psiModule] <- forceInvalidateFcs
            true

        | Some oldDisableRecovery ->
            dirtyModules[psiModule] <- forceInvalidateFcs || oldDisableRecovery
            forceInvalidateFcs = oldDisableRecovery

    let invalidateProject (project: IProject) =
        for psiModule in projectsPsiModules.GetValuesSafe(project) do
            markDirty false psiModule |> ignore
            fcsAssemblyReaderShim.InvalidateModule(psiModule)

    member x.FcsProjectInvalidated = fcsProjectInvalidated

    member private this.ProcessFSharpProjectLoaded(projectMark: IProjectMark) =
        use lock = FcsReadWriteLock.WriteCookie.Create()

        tryGetValue projectMark projectsProjectMarks
        |> Option.iter invalidateProject

    member x.ProcessChange(obj: ChangeEventArgs) =
        let change = obj.ChangeMap.GetChange<ProjectModelChange>(solution)
        if isNull change || change.IsClosingSolution then () else

        use lock = FcsReadWriteLock.WriteCookie.Create()
        match change with
        | :? ProjectReferenceChange as referenceChange ->
            invalidateProject referenceChange.ProjectToModuleReference.OwnerModule
        | change ->
            x.VisitDelta(change)

    override x.VisitDelta(change: ProjectModelChange) =
        match change.ProjectModelElement with
         | :? IProject as project ->
             if project.IsFSharp then
                 if change.ContainsChangeType(invalidateProjectChangeType) then
                     invalidateProject project

                     if change.IsRemoved then
                        fsItemsContainer.RemoveProject(project)

                 elif change.IsSubtreeChanged then
                     let mutable invalidate = false
                     let changeVisitor =
                         { new RecursiveProjectModelChangeDeltaVisitor() with
                             member x.VisitDelta(change) =
                                 if change.ContainsChangeType(invalidateChildChangeType) then
                                     invalidate <- true
                                 else
                                     base.VisitDelta(change) }

                     change.Accept(changeVisitor)
                     if invalidate then
                         invalidateProject project

             elif fcsAssemblyReaderShim.IsEnabled && AssemblyReaderShim.isSupportedProject project then
                 if change.ContainsChangeType(invalidateProjectChangeType) then
                     invalidateProject project

                 if change.IsRemoved then
                     for psiModule in projectsPsiModules.GetValuesSafe(project) do
                         fcsAssemblyReaderShim.RemoveModule(psiModule)

             elif project.ProjectProperties.ProjectKind = ProjectKind.SOLUTION_FOLDER then
                 base.VisitDelta(change)

         | :? ISolution -> base.VisitDelta(change)
         | _ -> ()

    interface IFcsProjectProvider with
        member x.GetProjectOptions(sourceFile: IPsiSourceFile) =
            locks.AssertReadAccessAllowed()
            processDirtyFcsProjects ()

            let psiModule = sourceFile.PsiModule

            // Scripts belong to separate psi modules even when are in projects, project/misc module check is enough.
            if isFSharpProjectModule psiModule then
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

        member x.GetProjectOptions(psiModule: IPsiModule) =
            locks.AssertReadAccessAllowed()
            processDirtyFcsProjects ()

            match getOrCreateFcsProject psiModule with
            | Some fcsProject -> Some fcsProject.ProjectOptions
            | _ -> None

        member x.HasPairFile(sourceFile) =
            locks.AssertReadAccessAllowed()
            processDirtyFcsProjects ()

            if isScriptLike sourceFile then false else

            let fcsProject = getOrCreateFcsProjectForParsing sourceFile
            fcsProject.ImplementationFilesWithSignatures.Contains(sourceFile.GetLocation())

        member x.GetParsingOptions(sourceFile) =
            locks.AssertReadAccessAllowed()
            processDirtyFcsProjects ()

            if isNull sourceFile then sandboxParsingOptions else
            if isScriptLike sourceFile then getParsingOptionsForSingleFile sourceFile true else

            let fcsProject = getOrCreateFcsProjectForParsing sourceFile
            fcsProject.ParsingOptions

        member x.GetFileIndex(sourceFile) =
            locks.AssertReadAccessAllowed()
            processDirtyFcsProjects ()

            if isScriptLike sourceFile then 0 else

            let path = sourceFile.GetLocation()

            let fcsProject = getOrCreateFcsProjectForParsing sourceFile

            tryGetValue path fcsProject.FileIndices
            |> Option.defaultWith (fun _ -> -1)

        member x.ModuleInvalidated = x.FcsProjectInvalidated :> _

        member x.InvalidateReferencesToProject(project: IProject, forceInvalidateFcs) =
            (false, project.GetPsiModules()) ||> Seq.fold (fun invalidated psiModule ->
                tryGetValue psiModule referencedModules
                |> Option.map (fun referencedModule ->
                    (invalidated, referencedModule.ReferencingModules)
                    ||> Seq.fold (fun invalidated psiModule -> markDirty forceInvalidateFcs psiModule || invalidated))
                |> Option.defaultValue invalidated)

        member x.HasFcsProjects = not (fcsProjects.IsEmpty())

        member this.GetAllFcsProjects() =
            fcsProjects.Values

        member x.InvalidateDirty() =
            processDirtyFcsProjects ()

        member this.GetFcsProject(psiModule) =
            getOrCreateFcsProject psiModule

        member this.GetPsiModule(outputPath) =
            tryGetValue outputPath outputPathToPsiModule

        member this.GetReferencedModule(psiModule) =
            tryGetValue psiModule referencedModules

        member this.GetAllReferencedModules() =
            referencedModules

        member this.PrepareAssemblyShim(psiModule) =
            if not fcsAssemblyReaderShim.IsEnabled then () else

            FSharpAsyncUtil.ProcessEnqueuedReadRequests()

            lock this (fun _ ->
                // todo: check that dirty modules are visible to the current one
                if not fcsAssemblyReaderShim.HasDirtyTypes then () else

                match tryGetFcsProject psiModule with
                | None -> ()
                | Some fcsProject ->

                fcsAssemblyReaderShim.InvalidateDirty()
                use barrier = locks.Tasks.CreateBarrier(lifetime)
                for referencedModule in fcsProject.ReferencedModules do
                    if isProjectModule referencedModule && not (isFSharpProjectModule referencedModule) then
                        barrier.EnqueueJob(fun _ -> fcsAssemblyReaderShim.InvalidateDirty(referencedModule)))


/// Invalidates psi caches when either a non-F# project or F# project containing generative type providers is built
/// which makes FCS cached resolve results stale
[<SolutionComponent>]
type OutputAssemblyChangeInvalidator(lifetime: Lifetime, outputAssemblies: OutputAssemblies, daemon: IDaemon,
        psiFiles: IPsiFiles, fcsProjectProvider: IFcsProjectProvider, scheduler: ISolutionLoadTasksScheduler,
        typeProvidersShim: IProxyExtensionTypingProvider, fcsAssemblyReaderShim: IFcsAssemblyReaderShim) =
    do
        scheduler.EnqueueTask(SolutionLoadTask("FcsProjectProvider", SolutionLoadTaskKinds.StartPsi, fun _ ->
            // todo: track file system changes instead? This currently may be triggered on a project model change too.
            outputAssemblies.ProjectOutputAssembliesChanged.Advise(lifetime, fun (project: IProject) ->
                // No FCS caches to invalidate.
                if not fcsProjectProvider.HasFcsProjects then () else

                // Only invalidate on F# project changes when project contains generative type providers
                if project.IsFSharp && not (typeProvidersShim.HasGenerativeTypeProviders(project)) then () else

                // The project change is visible to FCS without build.
                if fcsAssemblyReaderShim.IsEnabled && AssemblyReaderShim.isSupportedProject project then () else

                if fcsProjectProvider.InvalidateReferencesToProject(project, true) then
                    psiFiles.IncrementModificationTimestamp(null) // Drop cached values.
                    daemon.Invalidate() // Request files re-highlighting.
            )
        ))
