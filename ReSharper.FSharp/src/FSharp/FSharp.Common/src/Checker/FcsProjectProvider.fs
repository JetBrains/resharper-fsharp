namespace JetBrains.ReSharper.Plugins.FSharp.Checker

open System.Collections.Concurrent
open System.Collections.Generic
open System.IO
open FSharp.Compiler.AbstractIL.ILBinaryReader
open FSharp.Compiler.CodeAnalysis
open JetBrains.Annotations
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Components
open JetBrains.Application.Parts
open JetBrains.Application.Threading
open JetBrains.Application.changes
open JetBrains.DataFlow
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Build
open JetBrains.ProjectModel.Model2.Assemblies.Interfaces
open JetBrains.ProjectModel.ProjectsHost
open JetBrains.ProjectModel.Tasks.Listeners
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
open JetBrains.ReSharper.Plugins.FSharp.Util.FSharpAssemblyUtil
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.Psi.Files.SandboxFiles
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util
open JetBrains.Util.Dotnet.TargetFrameworkIds

[<AutoOpen>]
module FcsProjectProvider =
    let isProjectModule (psiModule: IPsiModule) =
        psiModule :? IProjectPsiModule

    let isProjectReference (reference: IProjectToModuleReference) =
        reference :? IProjectToProjectReference // todo: copy logic from PsiModuleUtil.TryGetPsiModuleReferences 

    let isFSharpProject (projectModelModule: IModule) =
        match projectModelModule with
        | :? IProject as project -> project.IsFSharp
        | _ -> false

    let isFSharpProjectModule (psiModule: IPsiModule) =
        isFSharpProject psiModule.ContainingProjectModule

    let isFSharpProjectOrAssemblyModule psiModule =
        isFSharpProjectModule psiModule ||
        isFSharpAssembly psiModule

    let [<Literal>] invalidateProjectChangeType =
        ProjectModelChangeType.PROPERTIES |||
        ProjectModelChangeType.TARGET_FRAMEWORK |||
        ProjectModelChangeType.REFERENCE_TARGET

    let [<Literal>] invalidateChildChangeType =
        ProjectModelChangeType.ADDED ||| ProjectModelChangeType.REMOVED |||
        ProjectModelChangeType.MOVED_IN ||| ProjectModelChangeType.MOVED_OUT |||
        ProjectModelChangeType.REFERENCE_TARGET

[<SolutionComponent(InstantiationEx.LegacyDefault)>]
[<ZoneMarker(typeof<ISinceClr4HostZone>)>]
type FcsProjectProvider(lifetime: Lifetime, solution: ISolution, changeManager: ChangeManager,
        checkerService: FcsCheckerService, fcsProjectBuilder: FcsProjectBuilder,
        scriptFcsProjectProvider: IScriptFcsProjectProvider,
        fsFileService: IFSharpFileService, fsItemsContainer: IFSharpItemsContainer,
        locks: IShellLocks, logger: ILogger, fcsAssemblyReaderShim: ILazy<IFcsAssemblyReaderShim>, psiModules: IPsiModules,
        moduleReferencesResolveStore: IModuleReferencesResolveStore) as this =
    inherit RecursiveProjectModelChangeDeltaVisitor()

    /// The main cache for FCS project model and related things.
    let fcsProjects = Dictionary<FcsProjectKey, FcsProject>()

    /// Prevents invalidating and removing projects that were already seen by FCS.
    /// This allows reusing them if the project is considered unchanged by FCS after the changes.
    /// This collection is emptied on the first FCS request, i.e. after the project model changes are processed.
    let invalidatedFcsProjects = ConcurrentQueue<FcsProject * FcsProjectInvalidationType>()

    let referencedModules = Dictionary<FcsProjectKey, ReferencedModule>()

    // /// Project keys known to Fcs project model as either an F# project or a supported referenced project output
    let projectsToProjectKeys = OneToSetMap<IProject, FcsProjectKey>()

    let outputPathToProjectKey = Dictionary<VirtualFileSystemPath, FcsProjectKey>()

    let dirtyProjects = HashSet<IProject>()
    let fcsProjectInvalidated = new Signal<FcsProjectKey * FcsProject>("FcsProjectInvalidated")

    do
        // todo: schedule listening after project model is ready; create fcs projects for all projects
        changeManager.Changed.Advise(lifetime, this.ProcessChange)
        fsItemsContainer.ProjectLoaded.Advise(lifetime, this.ProcessItemsContainerUpdate)
        fsItemsContainer.ProjectUpdated.Advise(lifetime, this.ProcessItemsContainerUpdate)
        checkerService.FcsProjectProvider <- this
        lifetime.OnTermination(fun _ -> checkerService.FcsProjectProvider <- Unchecked.defaultof<_>) |> ignore

    let getReferencingModules (projectKey: FcsProjectKey) =
        match tryGetValue projectKey referencedModules with
        | None -> Seq.empty
        | Some referencedModule -> referencedModule.ReferencingProjects :> _

    let rec removeProject (projectKey: FcsProjectKey) =
        locks.AssertWriteAccessAllowed()

        for referencingModule in getReferencingModules projectKey do
            dirtyProjects.Add(referencingModule.Project) |> ignore

        match tryGetValue projectKey fcsProjects with
        | None -> ()
        | Some fcsProject ->
            for referencedProjectKey in fcsProject.ReferencedModules do
                match tryGetValue referencedProjectKey referencedModules with
                | None -> ()
                | Some referencedModule ->
                    referencedModule.ReferencingProjects.Remove(projectKey) |> ignore
                    if referencedModule.ReferencingProjects.Count = 0 then
                        referencedModules.Remove(referencedProjectKey) |> ignore

            fcsProjects.Remove(projectKey) |> ignore
            outputPathToProjectKey.Remove(fcsProject.OutputPath) |> ignore
            invalidatedFcsProjects.Enqueue(fcsProject, FcsProjectInvalidationType.Remove)
            fcsProjectInvalidated.Fire((projectKey, fcsProject))

        referencedModules.Remove(projectKey) |> ignore
        projectsToProjectKeys.Remove(projectKey.Project, projectKey) |> ignore

    let areSameForChecking (newProject: FcsProject) (oldProject: FcsProject) =
        let rec loop (newOptions: FSharpProjectOptions) (oldOptions: FSharpProjectOptions) =
            newOptions.ProjectFileName = oldOptions.ProjectFileName &&
            newOptions.SourceFiles = oldOptions.SourceFiles &&
            newOptions.OtherOptions = oldOptions.OtherOptions &&

            newOptions.ReferencedProjects.Length = oldOptions.ReferencedProjects.Length &&
            (newOptions.ReferencedProjects, oldOptions.ReferencedProjects)
            ||> Array.forall2 (fun r1 r2 ->
                match r1, r2 with
                | FSharpReferencedProject.FSharpReference (_, r1),
                  FSharpReferencedProject.FSharpReference (_, r2) ->
                    r1.Stamp = r2.Stamp

                | FSharpReferencedProject.ILModuleReference(_, _, getReader1),
                  FSharpReferencedProject.ILModuleReference(_, _, getReader2) ->
                    getReader1 () = getReader2 ()

                | _ -> false
            )

        loop newProject.ProjectOptions oldProject.ProjectOptions

    let tryGetFcsProject (psiModule: IPsiModule): FcsProject option =
        locks.AssertReadAccessAllowed()
        let projectKey = FcsProjectKey.Create(psiModule)
        tryGetValue projectKey fcsProjects

    let getReferencedPsiModules (project: IProject) (psiModule: IPsiModule) =
        getReferencedModules psiModule
        |> Seq.filter (fun psiModule ->
            psiModule.IsValid() && psiModule.ContainingProjectModule != project)
        |> Seq.toList

    let tryGetReferencedProject (reference: IProjectToModuleReference) =
        match reference.ResolveResult(moduleReferencesResolveStore) with
        | :? IProject as project ->
            match reference.TargetFrameworkId.SelectTargetFrameworkIdToReference(project.TargetFrameworkIds) with
            | null -> None
            | targetFrameworkId -> Some(FcsProjectKey.Create(project, targetFrameworkId))
        | _ -> None

    let getReferencedProjects (projectKey: FcsProjectKey) =
        let moduleReferences = projectKey.Project.GetModuleReferences(projectKey.TargetFrameworkId)
        moduleReferences |> Seq.choose tryGetReferencedProject

    let getNextStamp =
        let mutable stamp = 0L
        fun _ ->
            let result = stamp
            stamp <- stamp + 1L
            result

    let rec getOrCreateFcsProject (projectKey: FcsProjectKey): FcsProject =
        locks.AssertWriteAccessAllowed()

        match tryGetValue projectKey fcsProjects with
        | Some fcsProject -> fcsProject
        | _ ->

        let fcsProject = createProject projectKey
        addProject projectKey fcsProject

    and addProject (projectKey: FcsProjectKey) (fcsProject: FcsProject) =
        match tryGetValue projectKey fcsProjects with
        | Some fcsProject -> fcsProject
        | None ->

        let stamp = Some(getNextStamp ())
        let fcsProject = { fcsProject with ProjectOptions = { fcsProject.ProjectOptions with Stamp = stamp } }

        if logger.IsTraceEnabled() then
            use writer = new StringWriter()
            fcsProject.TestDump(writer)
            logger.Trace($"Adding new project:\n{writer.ToString()}" )

        for referencedProjectKey in getReferencedProjects projectKey do
            let referencedModule = referencedModules.GetOrCreateValue(referencedProjectKey, ReferencedModule.create)
            referencedModule.ReferencingProjects.Add(projectKey) |> ignore

        fcsProjects[projectKey] <- fcsProject
        outputPathToProjectKey[fcsProject.OutputPath] <- projectKey
        projectsToProjectKeys.Add(projectKey.Project, projectKey) |> ignore

        fcsProject

    /// Creates and stores referenced projects.
    /// Then creates a new fcsProject for the current project but doesn't store it in the cache,
    /// so it can be used in `checkDirtyProject` which decides whether the cache needs updating or not. 
    and createProject (initialProjectKey: FcsProjectKey) =
        initialProjectKey.Project.AssertIsValid()

        let projectsToCreate = Stack()
        projectsToCreate.Push(initialProjectKey, None)

        let mutable result = Unchecked.defaultof<_>

        while projectsToCreate.Count > 0 do
            let projectKey, processedReferences = projectsToCreate.Pop()
            match processedReferences with
            | None ->
                let references = projectKey.Project.GetModuleReferences(projectKey.TargetFrameworkId)

                // Process the current project after creating referenced projects.
                projectsToCreate.Push(projectKey, Some(references))

                let referencedProjectKeys = references |> Seq.choose tryGetReferencedProject
                for referencedProjectKey in referencedProjectKeys do
                    if fcsProjects.ContainsKey(referencedProjectKey) then () else
                    if not (isFSharpProject referencedProjectKey.Project) then () else

                    projectsToCreate.Push(referencedProjectKey, None)

            | Some moduleReferences ->
                if fcsProjects.ContainsKey(projectKey) && projectKey <> initialProjectKey then () else

                let fcsProject = fcsProjectBuilder.BuildFcsProject(projectKey)

                let referencedFcsProjects = 
                    moduleReferences
                    |> Seq.choose tryGetReferencedProject
                    |> Seq.choose (fun referencedProjectKey ->
                        let referencedProject = referencedProjectKey.Project
                        if isFSharpProject referencedProject then
                            let referencedFcsProject = getOrCreateFcsProject referencedProjectKey
                            let path = referencedFcsProject.OutputPath.FullPath
                            Some(FSharpReferencedProject.FSharpReference(path, referencedFcsProject.ProjectOptions))

                        elif fcsAssemblyReaderShim.Value.IsEnabled && AssemblyReaderShim.isSupportedProject referencedProject then
                            fcsAssemblyReaderShim.Value.TryGetModuleReader(referencedProjectKey)
                            |> Option.map (fun reader ->
                                let getTimestamp () = reader.Timestamp
                                let getReader () = reader :> ILModuleReader
                                FSharpReferencedProject.ILModuleReference(reader.Path.FullPath, getTimestamp, getReader)
                            )
                        else
                            None
                    )
                    |> Seq.toArray

                fcsProject.ReferencedModules.AddRange(moduleReferences |> Seq.choose tryGetReferencedProject)

                let optionsWithReferences = { fcsProject.ProjectOptions with ReferencedProjects = referencedFcsProjects }
                let fcsProject = { fcsProject with ProjectOptions = optionsWithReferences }

                if projectKey <> initialProjectKey then
                    addProject projectKey fcsProject |> ignore

                result <- fcsProject

        result

    let tryGetFcsProjectForProjectOrScript (psiModule: IPsiModule): FcsProject option =
        locks.AssertReadAccessAllowed()

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
            tryGetFcsProject psiModule


    let isScriptLike (file: IPsiSourceFile) =
        not file.Properties.ProvidesCodeModel ||
        fsFileService.IsScriptLike(file) ||
        file.PsiModule.IsMiscFilesProjectModule() ||
        isNull (file.GetProject())

    let getParsingOptionsForSingleFile ([<NotNull>] sourceFile: IPsiSourceFile) isScript =
        { FSharpParsingOptions.Default with
            SourceFiles = [| sourceFile.GetLocation().FullPath |]
            ConditionalDefines = ImplicitDefines.scriptDefines
            IsInteractive = isScript
            IsExe = isScript }

    /// Checks whether existing FCS project options can be reused for the project.
    /// Dependencies are known to already have been checked due to the processing order inside `invalidateDirtyModules`.
    let checkDirtyProject (projectKey: FcsProjectKey) =
        match tryGetValue projectKey fcsProjects with
        | None ->
            let fcsProject = createProject projectKey
            addProject projectKey fcsProject |> ignore

        | Some existingFcsProject ->
            let fcsProject = createProject projectKey

            if not (areSameForChecking fcsProject existingFcsProject) then
                removeProject projectKey
                addProject projectKey fcsProject |> ignore

    let invalidateDirtyProjects () =
        locks.AssertWriteAccessAllowed()

        let visited = HashSet<FcsProjectKey>()
        let projectsToInvalidate = Stack()

        let loop (project: IProject) =
            if not (isFSharpProject project) then () else

            let existingProjectKeys = projectsToProjectKeys[project] |> List.ofSeq
            let newTargetFrameworks = project.TargetFrameworkIds
            for tfm in newTargetFrameworks do
                let projectKey = FcsProjectKey.Create(project, tfm)
                if visited.Contains(projectKey) then () else

                // Don't process project and its dependencies if it's already removed.
                if not (project.IsValid()) then () else

                projectsToInvalidate.Push(projectKey, false)
                while projectsToInvalidate.Count > 0 do
                    let projectKey, referencedAreChecked = projectsToInvalidate.Pop()
                    visited.Add(projectKey) |> ignore

                    if referencedAreChecked then
                        checkDirtyProject projectKey else

                    let referencedFSharpProjects =
                        getReferencedProjects projectKey
                        |> Seq.filter (fun projectKey ->
                            not (visited.Contains(projectKey)) && isFSharpProject projectKey.Project
                        )

                    if Seq.isEmpty referencedFSharpProjects then
                        checkDirtyProject projectKey
                    else
                        projectsToInvalidate.Push(projectKey, true)
                        for referencedFSharpModule in referencedFSharpProjects do
                            projectsToInvalidate.Push(referencedFSharpModule, false)

            for existingProjectKey in existingProjectKeys do
                if not (newTargetFrameworks.Contains(existingProjectKey.TargetFrameworkId)) then
                    removeProject existingProjectKey

        // Create a new collection, so transitive changes could add new dirty projects,
        // Adding dirty projects would break the enumeration below if the original collection is changed.
        let mutable currentDirtyProjects = null
        while (let result = HashSet(dirtyProjects)
               dirtyProjects.Clear()
               currentDirtyProjects <- result
               result.Count <> 0) do
            for dirtyProject in currentDirtyProjects do
                loop dirtyProject

    let processInvalidatedFcsProjects () =
        if invalidatedFcsProjects.IsEmpty then () else

        lock invalidatedFcsProjects (fun _ ->
            let invalidated = HashSet()
            
            let mutable fcsProjectToInvalidate = Unchecked.defaultof<_>
            while invalidatedFcsProjects.TryDequeue(&fcsProjectToInvalidate) do
                if invalidated.Contains(fcsProjectToInvalidate) then () else

                let fcsProject, invalidationType = fcsProjectToInvalidate
                checkerService.InvalidateFcsProject(fcsProject.ProjectOptions, invalidationType)

                invalidated.Add(fcsProjectToInvalidate) |> ignore
        )

        // fcsProjectInvalidated.Fire((psiModule, fcsProject))
        // fcsAssemblyReaderShim.InvalidateModule(psiModule)
        // todo: notify type providers and fcs symbol cache

    let getProjectItemChangeVisitor =
        let mutable currentProject = Unchecked.defaultof<IProject>
        let visitor =
            { new RecursiveProjectModelChangeDeltaVisitor() with
                override x.VisitDelta(change) =
                    if change.ContainsChangeType(invalidateChildChangeType) then
                        dirtyProjects.Add(currentProject) |> ignore
                    else
                        base.VisitDelta(change) }
        fun project ->
            currentProject <- project
            visitor

    member x.ProcessChange(obj: ChangeEventArgs) =
        Assertion.Assert(dirtyProjects.Count = 0)

        let change = obj.ChangeMap.GetChange<ProjectModelChange>(solution)
        if isNotNull change && not change.IsClosingSolution  then
            x.VisitDelta(change)

        if dirtyProjects.Count = 0 then () else

        use cookie = WriteLockCookie.Create()

        invalidateDirtyProjects ()

    override x.VisitDelta(change: ProjectModelChange) =
        base.VisitDelta(change)

        let project = change.ProjectModelElement.As<IProject>()
        if isNull project then () else

        if project.IsFSharp then
            if change.IsAdded then
                for targetFrameworkId in project.TargetFrameworkIds do
                    let projectKey = FcsProjectKey.Create(project, targetFrameworkId)
                    let fcsProject = createProject projectKey
                    addProject projectKey fcsProject |> ignore

            elif change.IsRemoved then
                fsItemsContainer.RemoveProject(project)

                for projectKey in projectsToProjectKeys[project] do
                    removeProject projectKey

            elif change.ContainsChangeType(invalidateProjectChangeType) then
                dirtyProjects.Add(project) |> ignore

            elif change.IsSubtreeChanged then
                change.Accept(getProjectItemChangeVisitor project)

    override this.VisitProjectReferenceDelta(change) =
        let project = change.GetOldProject()
        if isNotNull project then
            dirtyProjects.Add(project) |> ignore

    member this.ProcessItemsContainerUpdate(projectMark: IProjectMark) =
        let project = solution.GetProjectByMark(projectMark)
        if isNotNull project then
            dirtyProjects.Add(project) |> ignore
            invalidateDirtyProjects ()

    interface IFcsProjectProvider with
        member x.GetProjectOptions(sourceFile: IPsiSourceFile) =
            locks.AssertReadAccessAllowed()
            processInvalidatedFcsProjects ()

            let psiModule = sourceFile.PsiModule
            match psiModule with
            | :? FSharpScriptPsiModule ->
                scriptFcsProjectProvider.GetScriptOptions(sourceFile)

            | :? SandboxPsiModule ->
                let settings = sourceFile.GetSettingsStore()
                if not (settings.GetValue(fun (s: FSharpExperimentalFeatures) -> s.FsiInteractiveEditor)) then None else

                scriptFcsProjectProvider.GetScriptOptions(sourceFile)

            | _ ->

            match tryGetFcsProject psiModule with
            | Some fcsProject when fcsProject.IsKnownFile(sourceFile) -> Some fcsProject.ProjectOptions
            | _ -> None

        member x.GetProjectOptions(psiModule: IPsiModule) =
            locks.AssertReadAccessAllowed()
            processInvalidatedFcsProjects ()

            match tryGetFcsProject psiModule with
            | Some fcsProject -> Some fcsProject.ProjectOptions
            | _ -> None

        member x.HasPairFile(sourceFile) =
            locks.AssertReadAccessAllowed()
            processInvalidatedFcsProjects ()

            if isScriptLike sourceFile then false else

            tryGetFcsProject sourceFile.PsiModule
            |> Option.map (fun project -> project.ImplementationFilesWithSignatures.Contains(sourceFile.GetLocation()))
            |> Option.defaultValue false

        member x.GetParsingOptions(sourceFile) =
            locks.AssertReadAccessAllowed()
            processInvalidatedFcsProjects ()

            if isNull sourceFile then sandboxParsingOptions else
            if isScriptLike sourceFile then getParsingOptionsForSingleFile sourceFile true else

            match tryGetFcsProject sourceFile.PsiModule with
            | None -> getParsingOptionsForSingleFile sourceFile false
            | Some fcsProject ->

            let path = sourceFile.GetLocation().FullPath
            if Array.contains path fcsProject.ParsingOptions.SourceFiles then
                fcsProject.ParsingOptions
            else
                getParsingOptionsForSingleFile sourceFile false

        member x.GetFileIndex(sourceFile) =
            locks.AssertReadAccessAllowed()
            processInvalidatedFcsProjects ()

            if isScriptLike sourceFile then 0 else

            tryGetFcsProject sourceFile.PsiModule
            |> Option.map (fun project -> project.GetIndex(sourceFile))
            |> Option.defaultValue -1

        member x.ProjectRemoved = fcsProjectInvalidated :> _

        member x.InvalidateReferencesToProject(project: IProject) =
            project.TargetFrameworkIds
            |> Seq.map (fun targetFrameworkId -> FcsProjectKey.Create(project, targetFrameworkId))
            |> Seq.exists (fun projectKey ->
                match tryGetValue projectKey referencedModules with
                | None -> false
                | Some referencedModule ->
                    if referencedModule.ReferencingProjects.IsEmpty() then false else

                    for referencingProjectKey in referencedModule.ReferencingProjects do
                        tryGetValue referencingProjectKey fcsProjects
                        |> Option.iter (fun referencingProject ->
                            invalidatedFcsProjects.Enqueue(referencingProject, FcsProjectInvalidationType.Invalidate))

                    true
            )

        member x.HasFcsProjects = not (fcsProjects.IsEmpty())

        member this.GetAllFcsProjects() =
            fcsProjects.Values

        member this.GetFcsProject(psiModule) =
            tryGetFcsProjectForProjectOrScript psiModule

        member this.GetPsiModule(outputPath) =
            tryGetValue outputPath outputPathToProjectKey
            |> Option.map (fun projectKey ->
                psiModules.GetPrimaryPsiModule(projectKey.Project, projectKey.TargetFrameworkId)
            )

        member this.GetReferencedModule(projectKey) =
            tryGetValue projectKey referencedModules

        member this.GetAllReferencedModules() =
            referencedModules

        member this.PrepareAssemblyShim(psiModule) =
            locks.AssertReadAccessAllowed()

            if psiModule :? FSharpScriptPsiModule then () else

            match tryGetFcsProject psiModule with
            | None -> logger.Trace("Could not get fcs project; exiting")
            | Some fcsProject ->

            // todo: remove this
            logger.Trace("Start processing FCS requests")
            FSharpAsyncUtil.ProcessEnqueuedReadRequests()
            logger.Trace("Finish processing FCS requests")

            fcsAssemblyReaderShim.Value.PrepareForFcsRequest(fcsProject)

        member this.IsProjectOutput(outputPath) =
            outputPathToProjectKey.ContainsKey(outputPath)


/// Invalidates psi caches when either a non-F# project or F# project containing generative type providers is built
/// which makes FCS cached resolve results stale
[<SolutionComponent(Instantiation.DemandAnyThreadUnsafe)>]
type OutputAssemblyChangeInvalidator(lifetime: Lifetime, outputAssemblies: OutputAssemblies, daemon: IDaemon,
        psiFiles: IPsiFiles, fcsProjectProvider: IFcsProjectProvider, typeProvidersShim: ITypeProvidersShim,
        fcsAssemblyReaderShim: ILazy<IFcsAssemblyReaderShim>) =

    interface ISolutionLoadTasksStartPsiListener2 with
        member this.OnSolutionLoadStartPsi(_: OuterLifetime) =
            // todo: track file system changes instead? This currently may be triggered on a project model change too.
            outputAssemblies.ProjectOutputAssembliesChanged.Advise(lifetime, fun (project: IProject) ->
                // No FCS caches to invalidate.
                if not fcsProjectProvider.HasFcsProjects then () else

                // Only invalidate on F# project changes when project contains generative type providers
                if project.IsFSharp && not (typeProvidersShim.HasGenerativeTypeProviders(project)) then () else

                // The project change is visible to FCS without build.
                if fcsAssemblyReaderShim.Value.IsEnabled && AssemblyReaderShim.isSupportedProject project then () else

                if fcsProjectProvider.InvalidateReferencesToProject(project) then
                    // Drop cached values.
                    psiFiles.IncrementModificationTimestamp(null)

                    // Request files re-highlighting.
                    daemon.Invalidate($"Project {project.Name} contains F# generative type providers")
            )

            EmptyList<SolutionLoadTasksListenerExecutionStep>.Enumerable
