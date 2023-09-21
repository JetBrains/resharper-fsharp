namespace JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader

open System.Collections.Concurrent
open System.Collections.Generic
open System.Text
open FSharp.Compiler.AbstractIL.ILBinaryReader
open JetBrains.Application.Threading
open JetBrains.Application.changes
open JetBrains.DataFlow
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

[<SolutionComponent>]
type AssemblyReaderShim(lifetime: Lifetime, changeManager: ChangeManager, psiModules: IPsiModules,
        cache: FcsModuleReaderCommonCache, assemblyInfoShim: AssemblyInfoShim, checkerService: FcsCheckerService,
        fsOptionsProvider: FSharpOptionsProvider, symbolCache: ISymbolCache, solution: ISolution,
        locks: IShellLocks, logger: ILogger) as this =
    inherit AssemblyReaderShimBase(lifetime, changeManager)

    // todo: add experimental setting if/when available
    let isEnabledForAssemblies = false

    let isEnabled () =
        fsOptionsProvider.NonFSharpProjectInMemoryReferences ||
        FSharpExperimentalFeatureCookie.IsEnabled(ExperimentalFeature.AssemblyReaderShim)

    do
        if isEnabledForAssemblies then
            checkerService.AssemblyReaderShim <- this
            lifetime.OnTermination(fun _ -> checkerService.AssemblyReaderShim <- Unchecked.defaultof<_>) |> ignore

    // The shim is injected to get the expected shim shadowing chain, it's expected to be unused.
    do assemblyInfoShim |> ignore

    let assemblyReadersByPath = ConcurrentDictionary<VirtualFileSystemPath, ReferencedAssembly>()
    let assemblyReadersByModule = ConcurrentDictionary<IPsiModule, ReferencedAssembly>()

    let getFcsProjectProvider () = solution.GetComponent<IFcsProjectProvider>()

    let getReferencingModules psiModule =
        let fcsProjectProvider = getFcsProjectProvider ()
        match fcsProjectProvider.GetReferencedModule(psiModule) with
        | None -> Seq.empty
        | Some referencedModule -> referencedModule.ReferencingModules

    let isKnownModule (psiModule: IPsiModule) =
        let fcsProjectProvider = getFcsProjectProvider ()

        assemblyReadersByModule.ContainsKey(psiModule) ||
        fcsProjectProvider.GetReferencedModule(psiModule).IsSome

    let mutable invalidateAllReason = None

    let dirtyModules = HashSet()

    // todo: use short names?
    let dirtyTypesInModules = OneToSetMap<IPsiModule, string>()

    let invalidationsSinceLastTestDump = Queue()

    let mutable recordInvalidations = false

    let recordInvalidation invalidation =
        if recordInvalidations then
            invalidationsSinceLastTestDump.Enqueue(invalidation)

    let recordReader path reader =
        assemblyReadersByPath[path] <- reader

        match reader with
        | ReferencedAssembly.ProjectOutput(moduleReader, _) ->
            assemblyReadersByModule[moduleReader.PsiModule] <- reader
        | _ -> ()

    let readRealAssembly (path: VirtualFileSystemPath) =
        if not (this.DebugReadRealAssemblies && path.ExistsFile) then None else

        let readerOptions: ILReaderOptions = 
            { pdbDirPath = None
              reduceMemoryUsage = ReduceMemoryFlag.Yes
              metadataOnly = MetadataOnlyFlag.Yes
              tryGetMetadataSnapshot = fun _ -> None }

        Some(this.DefaultReader.GetILModuleReader(path.FullPath, readerOptions))

    let getOrCreateReaderFromModule (psiModule: IPsiModule) =
        let psiModule = psiModule.As<IProjectPsiModule>()
        if isNull psiModule then ReferencedAssembly.invalid else

        let mutable reader = Unchecked.defaultof<_>
        if assemblyReadersByModule.TryGetValue(psiModule, &reader) then reader else

        use readLockCookie = ReadLockCookie.Create()
        if not (AssemblyReaderShim.isSupportedModule psiModule) then ReferencedAssembly.invalid else

        // todo: is getting primary module needed? should we also replace module->primaryModule everywhere else?
        // todo: test web project with multiple modules
        let path = psiModule.Project.GetOutputFilePath(psiModule.TargetFrameworkId)
        let psiModule = psiModules.GetPrimaryPsiModule(psiModule.Project, psiModule.TargetFrameworkId)
        let realReader = readRealAssembly path
        let reader =
            ReferencedAssembly.ProjectOutput(new ProjectFcsModuleReader(psiModule, cache, path, this), realReader)

        recordReader path reader
        reader

    let getOrCreateReaderFromPath path =
        let mutable reader = Unchecked.defaultof<_>
        if assemblyReadersByPath.TryGetValue(path, &reader) then reader else

        use readLockCookie = ReadLockCookie.Create()

        let reader = 
            match AssemblyReaderShim.getProjectPsiModuleByOutputAssembly psiModules path with
            | null -> ReferencedAssembly.Ignored path
            | psiModule ->
                let realReader = readRealAssembly path
                ReferencedAssembly.ProjectOutput(new ProjectFcsModuleReader(psiModule, cache, path, this), realReader)

        recordReader path reader
        reader

    let tryGetReaderFromModule (psiModule: IPsiModule) (result: outref<_>) =
        let mutable referencedAssembly = Unchecked.defaultof<_>
        if not (assemblyReadersByModule.TryGetValue(psiModule, &referencedAssembly)) then false else

        match referencedAssembly with
        | ReferencedAssembly.Ignored _ -> false
        | ReferencedAssembly.ProjectOutput(reader, _) ->

        result <- reader
        true

    let moduleInvalidated = new Signal<IPsiModule>("AssemblyReaderShim.ModuleInvalidated")

    let rec removeModule (psiModule: IPsiModule) =
        let mutable moduleReader = Unchecked.defaultof<_>
        if tryGetReaderFromModule psiModule &moduleReader then
            assemblyReadersByPath.TryRemove(moduleReader.Path) |> ignore
            assemblyReadersByModule.TryRemove(psiModule) |> ignore

            recordInvalidation psiModule.DisplayName

        for referencingModule in getReferencingModules psiModule do
            removeModule referencingModule
            moduleInvalidated.Fire(referencingModule)

    // todo: invalidate for per-referencing module
    let invalidateDirtyDependencies () =
        let invalidatedModules = HashSet()

        let modulesToInvalidate = Stack()

        for dirtyModule in dirtyTypesInModules.Keys do
            let mutable dirtyModuleReader = Unchecked.defaultof<_>
            if tryGetReaderFromModule dirtyModule &dirtyModuleReader then
                for typeName in dirtyTypesInModules.GetValuesSafe(dirtyModule) do
                    dirtyModuleReader.InvalidateTypeDefs(typeName)

            if invalidatedModules.Add(dirtyModule) then
                modulesToInvalidate.Push(dirtyModule)
                moduleInvalidated.Fire(dirtyModule)

        while modulesToInvalidate.Count > 0 do
            let psiModule = modulesToInvalidate.Pop()

            for referencingModule in getReferencingModules psiModule do
                let mutable referencingModuleReader = Unchecked.defaultof<_>
                if tryGetReaderFromModule referencingModule &referencingModuleReader then
                    referencingModuleReader.MarkDirty()

                if invalidatedModules.Add(referencingModule) then
                    modulesToInvalidate.Push(referencingModule)
                    moduleInvalidated.Fire(referencingModule)

        dirtyTypesInModules.Clear()

    let markDirty (typePart: TypePart) =
        if not (isEnabled ()) then () else

        use lock = FcsReadWriteLock.WriteCookie.Create(locks)

        let typeElement = typePart.TypeElement
        let psiModule = typeElement.Module

        if not (isKnownModule psiModule) then () else

        // todo: use short names
        dirtyTypesInModules.Add(psiModule, typeElement.ShortName) |> ignore

    let invalidateDirty () =
        FcsReadWriteLock.assertWriteAccess()

        logger.Trace("Invalidate dirty: start")

        invalidateAllReason
        |> Option.iter (fun reason ->
            recordInvalidation $"Invalidate all: {reason}"

            invalidateAllReason <- None
            for KeyValue(psiModule, referencedAssembly) in assemblyReadersByModule do
                match referencedAssembly with
                | ReferencedAssembly.ProjectOutput(reader, _) ->
                    reader.InvalidateAllTypeDefs()
                    moduleInvalidated.Fire(psiModule)
                | _ -> ()
        )

        for psiModule in dirtyModules do
            removeModule psiModule

        dirtyModules.Clear()
        invalidateDirtyDependencies ()

        logger.Trace("Invalidate dirty: finish")

    do
        lifetime.Bracket(
            (fun () -> symbolCache.add_OnAfterTypePartAdded(markDirty)),
            (fun () -> symbolCache.remove_OnAfterTypePartAdded(markDirty)))

        lifetime.Bracket(
            (fun () -> symbolCache.add_OnBeforeTypePartRemoved(markDirty)),
            (fun () -> symbolCache.remove_OnBeforeTypePartRemoved(markDirty)))

    abstract DebugReadRealAssemblies: bool
    default this.DebugReadRealAssemblies = false

    member val ModuleInvalidated = moduleInvalidated

    override this.GetLastWriteTime(path) =
        if not isEnabledForAssemblies then base.GetLastWriteTime(path) else
        if not (isEnabled () && AssemblyReaderShim.isAssembly path) then base.GetLastWriteTime(path) else

        match getOrCreateReaderFromPath path with
        | ReferencedAssembly.ProjectOutput(reader, _) -> reader.Timestamp
        | _ -> base.GetLastWriteTime(path)

    override this.ExistsFile(path) =
        if not isEnabledForAssemblies then base.ExistsFile(path) else
        if not (isEnabled () && AssemblyReaderShim.isAssembly path) then base.ExistsFile(path) else

        match getOrCreateReaderFromPath path with
        | ReferencedAssembly.ProjectOutput _ -> true
        | _ -> base.ExistsFile(path)

    override this.GetModuleReader(path, readerOptions) =
        if not isEnabledForAssemblies then base.GetModuleReader(path, readerOptions) else
        if not (isEnabled () && AssemblyReaderShim.isAssembly path) then
            base.GetModuleReader(path, readerOptions) else

        match getOrCreateReaderFromPath path with
        | ReferencedAssembly.Ignored _ -> base.GetModuleReader(path, readerOptions)
        | ReferencedAssembly.ProjectOutput(reader, _) ->

        if this.DebugReadRealAssemblies && reader.RealModuleReader.IsNone then
            try
                reader.RealModuleReader <- Some(this.DefaultReader.GetILModuleReader(path.FullPath, readerOptions))
            with _ -> ()

        reader :> _

    interface IFcsAssemblyReaderShim with
        member this.IsEnabled = isEnabled ()

        member this.GetModuleReader(psiModule) =
            use lock = FcsReadWriteLock.WriteCookie.Create(locks)
            invalidateDirty ()
            getOrCreateReaderFromModule psiModule

        member this.InvalidateDirty() =
            use lock = FcsReadWriteLock.WriteCookie.Create(locks)
            invalidateDirty ()

        member this.InvalidateDirty(psiModule) =
            let mutable reader = Unchecked.defaultof<_>
            if tryGetReaderFromModule psiModule &reader then
                reader.UpdateTimestamp()

        member this.InvalidateModule(psiModule) =
            if isKnownModule psiModule then
                dirtyModules.Add(psiModule) |> ignore

        member this.TestDump =
            use cookie = ReadLockCookie.Create()
            use lock = FcsReadWriteLock.ReadCookie.Create()

            if not (isEnabled ()) then "Shim is disabled" else

            let builder = StringBuilder()

            builder.AppendLine($"Readers by module: {assemblyReadersByModule.Count}") |> ignore
            for psiModule in assemblyReadersByModule.Keys do
                builder.AppendLine($"  {psiModule.DisplayName}, IsValid: {psiModule.IsValid()}") |> ignore

            if assemblyReadersByPath.Count > 0 then
                builder.AppendLine($"Readers by path count: {assemblyReadersByPath.Count}") |> ignore

            let fcsProjectProvider = getFcsProjectProvider ()

            let referencedModules =
                fcsProjectProvider.GetAllReferencedModules()
                |> List.ofSeq
                |> List.filter (fun (KeyValue(psiModule, _)) -> psiModule :? IProjectPsiModule)

            if referencedModules.Length > 0 then
                builder.AppendLine("Dependencies to referencing modules:") |> ignore
                for KeyValue(dependency, referencedModule) in referencedModules do
                    builder.AppendLine($"  {dependency.DisplayName}") |> ignore
                    let referencingModules = referencedModule.ReferencingModules
                    for referencing in referencingModules |> Seq.sortBy (fun psiModule -> psiModule.DisplayName) do
                        builder.AppendLine($"    {referencing.DisplayName}") |> ignore

            if dirtyModules.Count > 0 then
                builder.AppendLine($"Dirty readers: {dirtyModules.Count}") |> ignore
                for psiModule in dirtyModules do
                    builder.AppendLine($"    {psiModule.DisplayName}") |> ignore

            if dirtyTypesInModules.Count > 0 then
                builder.AppendLine("Dirty types in readers:") |> ignore
                for psiModule in dirtyTypesInModules.Keys do
                    builder.AppendLine($"  {psiModule.DisplayName}") |> ignore
                    for typeName in dirtyTypesInModules.GetValuesSafe(psiModule) do
                        builder.AppendLine($"    {typeName}") |> ignore

            if invalidationsSinceLastTestDump.Count > 0 then
                builder.AppendLine("Invalidations since last dump:") |> ignore
                while invalidationsSinceLastTestDump.Count > 0 do
                    builder.AppendLine($"  {invalidationsSinceLastTestDump.Dequeue()}") |> ignore

            builder.ToString()

        member this.RemoveModule(psiModule) =
            let referencedAssembly = assemblyReadersByModule.TryGetValue(psiModule)
            if isNotNull referencedAssembly then
                assemblyReadersByModule.TryRemove(psiModule) |> ignore
                assemblyReadersByPath.TryRemove(referencedAssembly.Path) |> ignore

            dirtyModules.Remove(psiModule) |> ignore
            dirtyTypesInModules.RemoveKey(psiModule) |> ignore

        member this.IsKnownModule(psiModule: IPsiModule) =
            assemblyReadersByModule.ContainsKey(psiModule)

        member this.IsKnownModule(path: VirtualFileSystemPath) =
            assemblyReadersByPath.ContainsKey(path)

        member this.InvalidateAll(reason) =
            use lock = FcsReadWriteLock.WriteCookie.Create(locks)
            invalidateAllReason <- Some reason

        member this.HasDirtyTypes =
            use lock = FcsReadWriteLock.ReadCookie.Create()
            invalidateAllReason.IsSome || not (dirtyTypesInModules.IsEmpty())

        member this.Logger = logger

        member this.RecordInvalidations with set value =
            recordInvalidations <- value

        member this.MarkDirty(psiModule) =
            if not (isKnownModule psiModule) then () else
            dirtyTypesInModules.Add(psiModule, "") |> ignore 


[<SolutionComponent>]
type AssemblyReaderPsiCache(lifetime: Lifetime, psiModules: IPsiModules, changeManager: ChangeManager,
        shim: IFcsAssemblyReaderShim) as this =
    do
        changeManager.RegisterChangeProvider(lifetime, this)
        changeManager.AddDependency(lifetime, this, psiModules)
    
    interface IChangeProvider with
        member this.Execute(map) =
            let change = map.GetChange<PsiModuleChange>(psiModules)
            if isNotNull change && (change.AssemblyChanges.Count > 0 || change.ModuleChanges.Count > 0) then
                shim.InvalidateAll("Assemblies or modules changed")
            null
