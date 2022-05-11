namespace JetBrains.ReSharper.Plugins.FSharp.Checker

open System.Collections.Generic
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

    let fcsProjects = Dictionary<IPsiModule, FcsProject>()
    let referencedModules = Dictionary<IPsiModule, ReferencedModule>()
    let projectsPsiModules = OneToSetMap<IModule, IPsiModule>()
    let projectsProjectMarks = Dictionary<IProjectMark, IProject>()
    let projectMarkModules = Dictionary<IPsiModule, IProjectMark>()

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

        for referencedPsiModule in fcsProject.ReferencedModules do
            match tryGetValue referencedPsiModule referencedModules with
            | None -> ()
            | Some referencedModule ->
                referencedModule.ReferencingModules.Remove(referencedPsiModule) |> ignore

        referencedModules.Remove(psiModule) |> ignore
        fcsProjects.Remove(psiModule) |> ignore
        projectsPsiModules.Remove(psiModule.ContainingProjectModule, psiModule) |> ignore

        match tryGetValue psiModule projectMarkModules with
        | None -> ()
        | Some projectMark ->
            projectsProjectMarks.Remove(projectMark) |> ignore
        projectMarkModules.Remove(psiModule) |> ignore

        dirtyModules.Remove(psiModule) |> ignore

        // todo: remove removed psiModules? (don't we remove them anyway?) (standalone projects only?)
        logger.Trace("Done invalidating project: {0}", psiModule)

    let processDirtyFcsProjects () =
        use lock = FcsReadWriteLock.WriteCookie.Create()
        if dirtyModules.IsEmpty() then () else

        logger.Trace("Start invalidating dirty modules")
        let modulesToInvalidate = List(dirtyModules)
        for psiModule in modulesToInvalidate do
            invalidateFcsProject psiModule
        logger.Trace("Done invalidating dirty modules")

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

    let createReferencedModule psiModule =
        ReferencedModule.create modulePathProvider psiModule

    // todo: split getting parsing options from creating references:
    //   we don't want to build the whole FcsProject when HasPairFile info is needed
    let createFcsProject (project: IProject) (psiModule: IPsiModule): FcsProject =
        match tryGetValue psiModule fcsProjects with
        | Some fcsProject -> fcsProject
        | _ ->

        let projectsToCreate = Stack()
        projectsToCreate.Push(psiModule, project, None)

        while projectsToCreate.Count > 0 do
            let psiModule, project, processedReferences = projectsToCreate.Pop()
            match processedReferences with
            | None ->
                let referencedProjectPsiModules =
                    getReferencedModules psiModule
                    |> Seq.filter (fun psiModule ->
                        psiModule.IsValid() && isProjectModule psiModule &&
                        psiModule.ContainingProjectModule != project)
                    |> Seq.toList

                projectsToCreate.Push(psiModule, project, Some(referencedProjectPsiModules))

                referencedProjectPsiModules |> Seq.iter (fun referencedPsiModule ->
                    if not (isFSharpProjectModule referencedPsiModule) then () else
                    if fcsProjects.ContainsKey(referencedPsiModule) then () else

                    let referencedProject = referencedPsiModule.ContainingProjectModule :?> _
                    projectsToCreate.Push(referencedPsiModule, referencedProject, None))

            | Some referencedProjectPsiModules ->
                if fcsProjects.ContainsKey(psiModule) then () else

                let fcsProject = fcsProjectBuilder.BuildFcsProject(psiModule, project)

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
                            | ReferencedAssembly.Ignored -> None
                            | ReferencedAssembly.ProjectOutput reader ->

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

                fcsProjects[psiModule] <- fcsProject
                projectsPsiModules.Add(project, psiModule) |> ignore
                fcsAssemblyReaderShim.RecordDependencies(psiModule)

                let projectMark = project.GetProjectMark()
                projectsProjectMarks[projectMark] <- project
                projectMarkModules[psiModule] <- projectMark

        fcsProjects[psiModule]

    let getOrCreateFcsProject (psiModule: IPsiModule): FcsProject option =
        match tryGetFcsProject psiModule with
        | Some _ as fcsProject -> fcsProject
        | _ ->

        match getModuleProject psiModule with
        | FSharpProject project ->
            use lock = FcsReadWriteLock.WriteCookie.Create()
            let fcsProject = createFcsProject project psiModule
            Some fcsProject

        | _ -> None

    let getOrCreateFcsProjectForFile (sourceFile: IPsiSourceFile) =
        getOrCreateFcsProject sourceFile.PsiModule

    let isScriptLike file =
        fsFileService.IsScriptLike(file) || isMiscModule file.PsiModule || isNull (file.GetProject())

    let getParsingOptionsForSingleFile ([<NotNull>] sourceFile: IPsiSourceFile) isScript =
        { FSharpParsingOptions.Default with
            SourceFiles = [| sourceFile.GetLocation().FullPath |]
            ConditionalCompilationDefines = ImplicitDefines.scriptDefines
            IsInteractive = isScript
            IsExe = isScript }

    let invalidateProject (project: IProject) =
        for psiModule in projectsPsiModules.GetValuesSafe(project) do
            dirtyModules.Add(psiModule) |> ignore

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
            let referenceOwnerProject = referenceChange.ProjectToModuleReference.OwnerModule
            if referenceOwnerProject.IsFSharp then
                invalidateProject referenceOwnerProject
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

             else if project.ProjectProperties.ProjectKind = ProjectKind.SOLUTION_FOLDER then
                 base.VisitDelta(change)

         | :? ISolution -> base.VisitDelta(change)
         | _ -> ()

    interface IFcsProjectProvider with
        member x.GetProjectOptions(sourceFile: IPsiSourceFile) =
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

        member this.GetFcsProject(psiModule) =
            getOrCreateFcsProject psiModule

/// Invalidates psi caches when a non-F# project is built and FCS cached resolve results become stale
[<SolutionComponent>]
type OutputAssemblyChangeInvalidator(lifetime: Lifetime, outputAssemblies: OutputAssemblies, daemon: IDaemon,
        psiFiles: IPsiFiles, fcsProjectProvider: IFcsProjectProvider, scheduler: ISolutionLoadTasksScheduler,
        typeProvidersShim: IProxyExtensionTypingProvider) =
    do
        scheduler.EnqueueTask(SolutionLoadTask("FSharpProjectOptionsProvider", SolutionLoadTaskKinds.StartPsi, fun _ ->
            // todo: track file system changes instead? This currently may be triggered on a project model change too.
            outputAssemblies.ProjectOutputAssembliesChanged.Advise(lifetime, fun (project: IProject) ->
                if not fcsProjectProvider.HasFcsProjects ||
                   project.IsFSharp && not (typeProvidersShim.HasGenerativeTypeProviders(project)) then () else

                if fcsProjectProvider.InvalidateReferencesToProject(project) then
                    psiFiles.IncrementModificationTimestamp(null) // Drop cached values.
                    daemon.Invalidate() // Request files re-highlighting.
            )
        ))
