namespace JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader

open System.Collections.Concurrent
open System.Collections.Generic
open System.Text
open FSharp.Compiler.AbstractIL.ILBinaryReader
open JetBrains.Application.Parts
open JetBrains.Application.Threading
open JetBrains.Application.changes
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Properties
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Shim.FileSystem
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CSharp
open JetBrains.ReSharper.Psi.Caches
open JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.VB
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util

module AssemblyReaderShim =
    let isSupportedProjectLanguage (language: ProjectLanguage) =
        language = ProjectLanguage.CSHARP || language = ProjectLanguage.VBASIC

    let isSupportedProjectKind (projectKind: ProjectKind) =
        match projectKind with
        | ProjectKind.REGULAR_PROJECT
        | ProjectKind.WEB_SITE -> true
        | _ -> false

    let isSupportedProject (project: IProject) =
        isNotNull project &&

        let projectProperties = project.ProjectProperties

        isSupportedProjectLanguage projectProperties.DefaultLanguage &&
        isSupportedProjectKind projectProperties.ProjectKind

    let isSupportedModule (psiModule: IPsiModule) =
        let projectModule = psiModule.As<IProjectPsiModule>()
        isNotNull projectModule && isSupportedProject projectModule.Project

    let getProjectPsiModuleByOutputAssembly (psiModules: IPsiModules) path =
        let projectAndTargetFrameworkId = psiModules.TryGetProjectAndTargetFrameworkIdByOutputAssembly(path)
        if isNull projectAndTargetFrameworkId then null else

        let project, targetFrameworkId = projectAndTargetFrameworkId
        if not (isSupportedProject project) then null else

        psiModules.GetPrimaryPsiModule(project, targetFrameworkId)

    let isAssembly (path: VirtualFileSystemPath) =
        let extension = path.ExtensionNoDot
        equalsIgnoreCase "dll" extension || equalsIgnoreCase "exe" extension

    [<CompiledName("SupportedLanguages")>]
    let supportedLanguages =
        [| CSharpLanguage.Instance :> PsiLanguageType
           VBLanguage.Instance :> _ |]
        |> HashSet

// todo: support script -> project references

[<SolutionComponent(InstantiationEx.LegacyDefault)>]
type AssemblyReaderShim(lifetime: Lifetime, changeManager: ChangeManager, psiModules: IPsiModules,
        cache: FcsModuleReaderCommonCache, assemblyInfoShim: AssemblyInfoShim,
        fsOptionsProvider: FSharpOptionsProvider, symbolCache: ISymbolCache, solution: ISolution,
        locks: IShellLocks, logger: ILogger) as this =
    inherit AssemblyReaderShimBase(lifetime, changeManager)

    let assemblyReadersByModule = ConcurrentDictionary<IPsiModule, IProjectFcsModuleReader>()
    let assemblyReadersByPath = ConcurrentDictionary<VirtualFileSystemPath, IProjectFcsModuleReader>()

    let projectKeyToPsiModules = ConcurrentDictionary<FcsProjectKey, IPsiModule>()

    /// Modules invalidated by symbol cache or are known to read incomplete contents.
    /// Readers need to check up to date before new FCS requests.
    let dirtyModules = HashSet<IPsiModule>()

    let mutable lastSyncedWriteLockTimestamp = locks.ContentModelLocks.WriteLockTimestamp

    do
        // The shim is injected to get the expected shim shadowing chain, it's expected to be unused.
        assemblyInfoShim |> ignore

        changeManager.RegisterChangeProvider(lifetime, this)
        changeManager.AddDependency(lifetime, this, psiModules)

    let isEnabled () =
        fsOptionsProvider.NonFSharpProjectInMemoryReferences ||
        FSharpExperimentalFeatureCookie.IsEnabled(ExperimentalFeature.AssemblyReaderShim)

    let getFcsProjectProvider () = solution.GetComponent<IFcsProjectProvider>()

    let getReferencingModules (psiModule: IPsiModule) =
        let projectKey = FcsProjectKey.Create(psiModule)
        let fcsProjectProvider = getFcsProjectProvider ()
        match fcsProjectProvider.GetReferencedModule(projectKey) with
        | None -> Seq.empty
        | Some referencedModule -> referencedModule.ReferencingProjects

    let isKnownModule (psiModule: IPsiModule) =
        if not (psiModule.ContainingProjectModule :? IProject) then false else

        assemblyReadersByModule.ContainsKey(psiModule) ||

        let fcsProjectProvider = getFcsProjectProvider ()
        let projectKey = FcsProjectKey.Create(psiModule)
        fcsProjectProvider.GetReferencedModule(projectKey).IsSome


    let readRealAssembly (path: VirtualFileSystemPath) =
        if not (this.DebugReadRealAssemblies && path.ExistsFile) then None else

        let readerOptions: ILReaderOptions =
            { pdbDirPath = None
              reduceMemoryUsage = ReduceMemoryFlag.Yes
              metadataOnly = MetadataOnlyFlag.Yes
              tryGetMetadataSnapshot = fun _ -> None }

        Some(this.DefaultReader.GetILModuleReader(path.FullPath, readerOptions))

    let getOrCreateReaderFromModule (projectKey: FcsProjectKey) =
        locks.AssertWriteAccessAllowed()

        let project = projectKey.Project
        let targetFrameworkId = projectKey.TargetFrameworkId

        // todo: test web project with multiple modules
        let psiModule = psiModules.GetPrimaryPsiModule(project, targetFrameworkId)
        let psiModule = psiModule.As<IProjectPsiModule>()
        if isNull psiModule then None else

        let mutable reader = Unchecked.defaultof<_>
        if assemblyReadersByModule.TryGetValue(psiModule, &reader) then Some(reader) else

        if not (AssemblyReaderShim.isSupportedProject project) then None else

        let path = psiModule.Project.GetOutputFilePath(targetFrameworkId)
        let realReader = readRealAssembly path
        let reader = new ProjectFcsModuleReader(psiModule, cache, path, this, realReader)

        assemblyReadersByModule[psiModule] <- reader
        assemblyReadersByPath[path] <- reader
        projectKeyToPsiModules[projectKey] <- psiModule
        Some(reader)

    let tryGetReaderFromModule (psiModule: IPsiModule) =
        tryGetValue psiModule assemblyReadersByModule

    let rec removeModule (psiModule: IPsiModule) =
        let projectKey = FcsProjectKey.Create(psiModule)

        tryGetReaderFromModule psiModule
        |> Option.iter (fun reader ->
            assemblyReadersByPath.TryRemove(reader.Path) |> ignore
            assemblyReadersByModule.TryRemove(psiModule) |> ignore
            projectKeyToPsiModules.TryRemove(projectKey) |> ignore
        )

    let markDirtyReaders () =
        let invalidatedModules = HashSet<IPsiModule>()
        let modulesToInvalidate = Stack<IPsiModule>(dirtyModules)

        while modulesToInvalidate.Count > 0 do
            let psiModule = modulesToInvalidate.Pop()

            if not (psiModule.ContainingProjectModule :? IProject) then () else
            if not (invalidatedModules.Add(psiModule)) then () else

            match tryGetReaderFromModule psiModule with
            | None -> ()
            | Some reader -> reader.MarkDirty()

            for referencingProjectKey in getReferencingModules psiModule do
                let referencingModule = projectKeyToPsiModules.TryGetValue(referencingProjectKey)
                if not (isNull referencingModule) then
                    modulesToInvalidate.Push(referencingModule)

        dirtyModules.Clear()

    let markTypePartDirty (typePart: TypePart) =
        if isEnabled () && assemblyReadersByModule.Count <> 0 then
            dirtyModules.Add(typePart.TypeElement.Module) |> ignore

    do
        lifetime.Bracket(
            (fun () -> symbolCache.add_OnAfterTypePartAdded(markTypePartDirty)),
            (fun () -> symbolCache.remove_OnAfterTypePartAdded(markTypePartDirty)))

        lifetime.Bracket(
            (fun () -> symbolCache.add_OnBeforeTypePartRemoved(markTypePartDirty)),
            (fun () -> symbolCache.remove_OnBeforeTypePartRemoved(markTypePartDirty)))

    abstract DebugReadRealAssemblies: bool
    default this.DebugReadRealAssemblies = false

    interface IFcsAssemblyReaderShim with
        member this.IsEnabled = isEnabled ()

        member this.TryGetModuleReader(projectKey: FcsProjectKey) =
            locks.AssertWriteAccessAllowed()

            getOrCreateReaderFromModule projectKey

        member this.TestDump =
            use cookie = ReadLockCookie.Create()

            if not (isEnabled ()) then "Shim is disabled" else

            let builder = StringBuilder()

            builder.AppendLine($"Readers by module: {assemblyReadersByModule.Count}") |> ignore
            for psiModule in assemblyReadersByModule.Keys do
                builder.AppendLine($"  {psiModule.DisplayName}, IsValid: {psiModule.IsValid()}") |> ignore

            let fcsProjectProvider = getFcsProjectProvider ()

            let referencedModules =
                fcsProjectProvider.GetAllReferencedModules()
                |> List.ofSeq

            if referencedModules.Length > 0 then
                builder.AppendLine("Dependencies to referencing modules:") |> ignore
                for KeyValue(dependency, referencedModule) in referencedModules do
                    builder.AppendLine($"  {dependency.Project.Name}, IsValid: {dependency.Project.IsValid()}") |> ignore
                    let referencingModules = referencedModule.ReferencingProjects
                    for referencing in referencingModules |> Seq.sortBy (fun projectKey -> projectKey.Project.Name) do
                        builder.AppendLine($"    {referencing.Project.Name}") |> ignore

            if dirtyModules.Count > 0 then
                builder.AppendLine("Dirty types in readers:") |> ignore
                for psiModule in dirtyModules do
                    builder.AppendLine($"  {psiModule.DisplayName}, IsValid: {psiModule.IsValid()}") |> ignore

            builder.ToString()

        member this.IsKnownModule(psiModule: IPsiModule) =
            assemblyReadersByModule.ContainsKey(psiModule)

        member this.IsKnownModule(path: VirtualFileSystemPath) =
            assemblyReadersByPath.ContainsKey(path)

        member this.Logger = logger

        member this.MarkDirty(psiModule) =
            // todo: do we need this check? Should we just check that the module comes from a project?
            if isKnownModule psiModule then
                dirtyModules.Add(psiModule) |> ignore

        member this.PrepareForFcsRequest(fcsProject) =
            locks.AssertReadAccessAllowed()

            if not (isEnabled ()) then () else
            if lastSyncedWriteLockTimestamp = locks.ContentModelLocks.WriteLockTimestamp then () else

            lock this (fun _ ->
                if lastSyncedWriteLockTimestamp = locks.ContentModelLocks.WriteLockTimestamp then () else

                markDirtyReaders ()
                lastSyncedWriteLockTimestamp <- locks.ContentModelLocks.WriteLockTimestamp
            )

            use barrier = locks.Tasks.CreateBarrier(lifetime)
            for referencedProjectKey in fcsProject.ReferencedModules do
                let referencedModule = projectKeyToPsiModules.TryGetValue(referencedProjectKey)
                if isNotNull referencedModule && assemblyReadersByModule.ContainsKey(referencedModule) then
                    barrier.EnqueueJob(fun _ ->
                        tryGetReaderFromModule referencedModule |> Option.iter _.UpdateTimestamp()
                    )

            logger.Trace("Finish invalidating assembly reader")

    interface IChangeProvider with
        member this.Execute(map) =
            locks.AssertWriteAccessAllowed()

            let change = map.GetChange<PsiModuleChange>(psiModules)
            if isNull change then null else

            for change in change.ModuleChanges do
                if not (change.Item :? IProjectPsiModule) then () else

                match change.Type with
                | PsiModuleChange.ChangeType.Modified
                | PsiModuleChange.ChangeType.Invalidated ->
                    dirtyModules.Add(change.Item) |> ignore

                | PsiModuleChange.ChangeType.Removed ->
                    removeModule change.Item

                | _ -> ()

            null
