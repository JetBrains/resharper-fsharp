namespace JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader

open System.Collections.Concurrent
open System.Collections.Generic
open System.Text
open JetBrains.Application.changes
open JetBrains.DataFlow
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.Metadata.Reader.API
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Properties
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
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
open JetBrains.Threading
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

    [<CompiledName("IsEnabled")>]
    let isEnabled settingsStore =
        SettingsUtil.getValue<FSharpOptions, bool> settingsStore "NonFSharpProjectInMemoryAnalysis"

    [<CompiledName("SupportedLanguages")>]
    let supportedLanguages =
        [| CSharpLanguage.Instance :> PsiLanguageType
           VBLanguage.Instance :> _ |]
        |> HashSet

// todo: support script -> project references

[<SolutionComponent>]
type AssemblyReaderShim(lifetime: Lifetime, changeManager: ChangeManager, psiModules: IPsiModules,
        cache: FcsModuleReaderCommonCache, assemblyInfoShim: AssemblyInfoShim, checkerService: FcsCheckerService,
        fsOptionsProvider: FSharpOptionsProvider, symbolCache: ISymbolCache) as this =
    inherit AssemblyReaderShimBase(lifetime, changeManager)

    // todo: add experimental setting if/when available
    let isEnabledForAssemblies = false

    let isEnabled () =
        FSharpExperimentalFeatureCookie.IsEnabled(ExperimentalFeature.AssemblyReaderShim) ||
        fsOptionsProvider.NonFSharpProjectInMemoryAnalysis.Value

    do
        if isEnabledForAssemblies then
            checkerService.AssemblyReaderShim <- this
            lifetime.OnTermination(fun _ -> checkerService.AssemblyReaderShim <- Unchecked.defaultof<_>) |> ignore

    // The shim is injected to get the expected shim shadowing chain, it's expected to be unused.
    do assemblyInfoShim |> ignore

    let locker = JetFastSemiReenterableRWLock()

    let assemblyReadersByPath = ConcurrentDictionary<VirtualFileSystemPath, ReferencedAssembly>()
    let assemblyReadersByModule = ConcurrentDictionary<IPsiModule, ReferencedAssembly>()

    // todo: record empty set in nonLazyModuleDependencies somehow, remove this set
    let moduleDependenciesRecorded = HashSet<IPsiModule>()

    /// F# project module dependencies requiring non-lazy module readers.
    let nonLazyDependenciesForModule = OneToSetMap<IPsiModule, IPsiModule>()

    let dependenciesToReferencingModules = OneToSetMap<IPsiModule, IPsiModule>()

    let isKnownModule (psiModule: IPsiModule) =
        assemblyReadersByModule.ContainsKey(psiModule) ||
        dependenciesToReferencingModules.ContainsKey(psiModule)

    // todo: F#->F#->C# references
    //   change in F#->F# is not seen by C# now

    let dirtyModules = HashSet()

    // todo: use short names?
    let dirtyTypesInModules = OneToSetMap<IPsiModule, IClrTypeName>()

    let transitiveReferencedProjectModules (psiModule: IPsiModule) =
        let visited = HashSet()
        let projectModules = HashSet()
        let mutable hasFSharpReferences = false

        let rec loop (psiModule: IPsiModule) =
            getReferencedModules psiModule
            |> Seq.iter (fun referencedModule ->
                match referencedModule with
                | :? IProjectPsiModule as referencedModule ->
                    if visited.Contains(referencedModule) then () else

                    projectModules.Add(referencedModule) |> ignore
                    if referencedModule.Project.IsFSharp then
                        hasFSharpReferences <- true

                    visited.Add(referencedModule) |> ignore
                    loop referencedModule
                | _ -> ())

        loop psiModule
        projectModules, hasFSharpReferences

    let rec recordDependencies (psiModule: IPsiModule): unit =
        if moduleDependenciesRecorded.Contains(psiModule) then () else

        if not (psiModule :? IProjectPsiModule) then () else

        // todo: filter by primary module? test on web projects containing multiple modules 
        for referencedModule in getReferencedModules psiModule do
            if not (referencedModule :? IProjectPsiModule) then () else

            let referencedProjectModules, hasFSharpReferences = transitiveReferencedProjectModules referencedModule

            if hasFSharpReferences then
                nonLazyDependenciesForModule.Add(psiModule, referencedModule) |> ignore

            dependenciesToReferencingModules.Add(referencedModule, psiModule) |> ignore

            for referencedProjectModule in referencedProjectModules do
                recordDependencies referencedProjectModule

        moduleDependenciesRecorded.Add(psiModule) |> ignore

    let recordReader path reader =
        assemblyReadersByPath[path] <- reader

        match reader with
        | ReferencedAssembly.ProjectOutput moduleReader ->
            assemblyReadersByModule[moduleReader.PsiModule] <- reader
        | _ -> ()

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
        let reader = ReferencedAssembly.ProjectOutput(new ProjectFcsModuleReader(psiModule, cache, this, path))

        recordReader path reader
        reader

    let getOrCreateReaderFromPath path =
        let mutable reader = Unchecked.defaultof<_>
        if assemblyReadersByPath.TryGetValue(path, &reader) then reader else

        use readLockCookie = ReadLockCookie.Create()

        let reader = 
            match AssemblyReaderShim.getProjectPsiModuleByOutputAssembly psiModules path with
            | null -> ReferencedAssembly.Ignored path
            | psiModule -> ReferencedAssembly.ProjectOutput(new ProjectFcsModuleReader(psiModule, cache, this, path))

        recordReader path reader
        reader

    let tryGetReaderFromModule (psiModule: IPsiModule) (result: outref<_>) =
        let mutable referencedAssembly = Unchecked.defaultof<_>
        if not (assemblyReadersByModule.TryGetValue(psiModule, &referencedAssembly)) then false else

        match referencedAssembly with
        | ReferencedAssembly.Ignored _ -> false
        | ReferencedAssembly.ProjectOutput(reader) ->

        result <- reader
        true

    let moduleInvalidated = new Signal<IPsiModule>(lifetime, "AssemblyReaderShim.ModuleInvalidated")

    let rec invalidateModule (psiModule: IPsiModule) =
        let mutable moduleReader = Unchecked.defaultof<_>
        if not (tryGetReaderFromModule psiModule &moduleReader) then () else

        assemblyReadersByPath.TryRemove(moduleReader.Path) |> ignore
        assemblyReadersByModule.TryRemove(psiModule) |> ignore

        for referencingModule in dependenciesToReferencingModules.GetValuesSafe(psiModule) do
            invalidateModule referencingModule
            moduleInvalidated.Fire(referencingModule)

        dependenciesToReferencingModules.RemoveKey(psiModule) |> ignore

    // todo: invalidate for per-referencing module
    let invalidateDirtyDependencies () =
        Assertion.Assert(locker.IsWriteLockHeld, "locker.IsWriteLockHeld")

        let invalidatedModules = HashSet()

        for dirtyModule in dirtyTypesInModules.Keys do
            let mutable dirtyModuleReader = Unchecked.defaultof<_>
            if not (tryGetReaderFromModule dirtyModule &dirtyModuleReader) then () else

            for typeName in dirtyTypesInModules.GetValuesSafe(dirtyModule) do
                dirtyModuleReader.InvalidateTypeDef(typeName)

            for referencingModule in dependenciesToReferencingModules.GetValuesSafe(dirtyModule) do
                let mutable referencingModuleReader = Unchecked.defaultof<_>
                if not (tryGetReaderFromModule referencingModule &referencingModuleReader) then () else

                for typeName in dirtyTypesInModules.GetValuesSafe(dirtyModule) do
                    referencingModuleReader.InvalidateReferencingTypes(typeName.ShortName)

                referencingModuleReader.InvalidateTypesReferencingFSharpModule(dirtyModule)

                if invalidatedModules.Add(referencingModule) then
                    moduleInvalidated.Fire(referencingModule)

        dirtyTypesInModules.Clear()

    let markDirty (typePart: TypePart) =
        use lock = locker.UsingWriteLock()

        let typeElement = typePart.TypeElement
        let psiModule = typeElement.Module

        if not (isKnownModule psiModule) then () else

        // todo: use short names
        dirtyTypesInModules.Add(psiModule, typeElement.GetClrName().GetPersistent()) |> ignore

    let invalidateDirty () =
        Assertion.Assert(locker.IsWriteLockHeld)
        
        for psiModule in dirtyModules do
            invalidateModule psiModule
        dirtyModules.Clear()
        invalidateDirtyDependencies ()

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
        | ReferencedAssembly.ProjectOutput reader -> reader.Timestamp
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
        | ReferencedAssembly.ProjectOutput reader ->

        if this.DebugReadRealAssemblies && reader.RealModuleReader.IsNone then
            try
                reader.RealModuleReader <- Some(this.DefaultReader.GetILModuleReader(path.FullPath, readerOptions))
            with _ -> ()

        reader :> _

    interface IFcsAssemblyReaderShim with
        member this.IsEnabled = isEnabled ()

        member this.GetModuleReader(psiModule) =
            use lock = locker.UsingWriteLock()
            invalidateDirty ()
            getOrCreateReaderFromModule psiModule

        member this.InvalidateDirty() =
            use lock = locker.UsingWriteLock()
            invalidateDirty ()

        member this.RecordDependencies(psiModule) =
            recordDependencies psiModule

        member this.InvalidateModule(psiModule) =
            if isKnownModule psiModule then
                dirtyModules.Add(psiModule) |> ignore

        member this.TestDump =
            use cookie = ReadLockCookie.Create()
            use lock = locker.UsingReadLock()

            let builder = StringBuilder()

            builder.AppendLine($"Readers by module: {assemblyReadersByModule.Count}") |> ignore
            for psiModule in assemblyReadersByModule.Keys do
                builder.AppendLine($"  {psiModule.DisplayName}") |> ignore

            if assemblyReadersByPath.Count > 0 then
                builder.AppendLine($"Readers by path count: {assemblyReadersByPath.Count}") |> ignore

            if dirtyModules.Count > 0 then
                builder.AppendLine($"Dirty readers: {dirtyModules.Count}") |> ignore
                for psiModule in dirtyModules do
                    builder.AppendLine($"    {psiModule.DisplayName}") |> ignore

            if dirtyTypesInModules.Count > 0 then
                builder.AppendLine("Dirty types in readers:") |> ignore
                for psiModule in dirtyTypesInModules.Keys do
                    builder.AppendLine($"  {psiModule.DisplayName}") |> ignore
                    for typeName in dirtyTypesInModules.GetValuesSafe(psiModule) do
                        builder.AppendLine($"    {typeName.FullName}") |> ignore

            builder.ToString()

        member this.RemoveModule(psiModule) =
            let referencedAssembly = assemblyReadersByModule.TryGetValue(psiModule)
            if isNotNull referencedAssembly then
                assemblyReadersByModule.TryRemove(psiModule) |> ignore
                assemblyReadersByPath.TryRemove(referencedAssembly.Path) |> ignore

            dirtyModules.Remove(psiModule) |> ignore
            dirtyTypesInModules.RemoveKey(psiModule) |> ignore
