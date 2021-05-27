namespace JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader

open System
open System.Collections.Concurrent
open System.Collections.Generic
open JetBrains.Application.Settings
open JetBrains.Application.changes
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

[<RequireQualifiedAccess>]
type ReferencedAssembly =
    /// An output of a psi source project except for F# projects.
    | ProjectOutput of ProjectFcsModuleReader

    /// Not supported file or output assembly for F# project.
    | Ignored

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

    let getProjectPsiModuleByOutputAssembly (psiModules: IPsiModules) path =
        let projectAndTargetFrameworkId = psiModules.TryGetProjectAndTargetFrameworkIdByOutputAssembly(path)
        if isNull projectAndTargetFrameworkId then null else

        let project, targetFrameworkId = projectAndTargetFrameworkId 
        if not (isSupportedProject project) then null else

        psiModules.GetPrimaryPsiModule(project, targetFrameworkId)

    let isAssembly (path: FileSystemPath) =
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

[<SolutionComponent>]
type AssemblyReaderShim(lifetime: Lifetime, changeManager: ChangeManager, psiModules: IPsiModules,
        cache: FcsModuleReaderCommonCache, assemblyInfoShim: AssemblyInfoShim, isEnabled: bool,
        checkerService: FcsCheckerService) as this =
    inherit AssemblyReaderShimBase(lifetime, changeManager, isEnabled)

    do
        checkerService.AssemblyReaderShim <- this
        lifetime.OnTermination(fun _ -> checkerService.AssemblyReaderShim <- Unchecked.defaultof<_>) |> ignore

    // The shim is injected to get the expected shim shadowing chain, it's expected to be unused. 
    do assemblyInfoShim |> ignore

    let locker = JetFastSemiReenterableRWLock()

    let assemblyReadersByPath = ConcurrentDictionary<FileSystemPath, ReferencedAssembly>()
    let assemblyReadersByModule = ConcurrentDictionary<IPsiModule, ReferencedAssembly>()

    /// Dependencies that require non-lazy module readers.
    let moduleDependencies = OneToSetMap<IPsiModule, IPsiModule>()

    let dependenciesToModules = OneToSetMap<IPsiModule, IPsiModule>()

    let dirtyModules = OneToSetMap<IPsiModule, IClrTypeName>()

    let createReader (path: FileSystemPath) =
        use readLockCookie = ReadLockCookie.Create()
        match AssemblyReaderShim.getProjectPsiModuleByOutputAssembly psiModules path with
        | null -> ReferencedAssembly.Ignored
        | psiModule -> ReferencedAssembly.ProjectOutput(new ProjectFcsModuleReader(psiModule, cache))

    let rec hasTransitiveReferencesToFSharpProjects (psiModule: IPsiModule) =
        let visited = HashSet()

        getReferencedModules psiModule
        |> Seq.filter (fun referencedModule ->
            match referencedModule with
            | :? IProjectPsiModule as referencedModule ->
                if visited.Contains(referencedModule) then false else

                let hasReference = 
                    referencedModule.Project.IsFSharp ||
                    hasTransitiveReferencesToFSharpProjects referencedModule

                if hasReference then true else

                visited.Add(referencedModule) |> ignore
                false

            | _ -> false)
        |> Seq.isEmpty
        |> not

    let recordDependencies (psiModule: IPsiModule) =
        for referencedModule in getReferencedModules psiModule do
            let referencedModule = referencedModule.As<IProjectPsiModule>()
            if isNull referencedModule then () else

            let projectLanguage = referencedModule.Project.ProjectProperties.DefaultLanguage
            if not (AssemblyReaderShim.isSupportedProjectLanguage projectLanguage) then () else

            if not (hasTransitiveReferencesToFSharpProjects referencedModule) then () else

            // todo: add transitive dependencies
            moduleDependencies.Add(psiModule, referencedModule) |> ignore
            dependenciesToModules.Add(referencedModule, psiModule) |> ignore

    let getOrCreateReader path =
        let mutable reader = Unchecked.defaultof<_>
        if assemblyReadersByPath.TryGetValue(path, &reader) then reader else

        let reader = createReader path
        assemblyReadersByPath.[path] <- reader

        match reader with
        | ReferencedAssembly.ProjectOutput moduleReader ->
            assemblyReadersByModule.[moduleReader.PsiModule] <- reader
        | _ -> ()

        reader

    new (lifetime: Lifetime, changeManager: ChangeManager, psiModules: IPsiModules, cache: FcsModuleReaderCommonCache,
            assemblyInfoShim: AssemblyInfoShim, checkerService: FcsCheckerService, settingsStore: ISettingsStore) =
        let isEnabled = AssemblyReaderShim.isEnabled settingsStore
        AssemblyReaderShim(lifetime, changeManager, psiModules, cache, assemblyInfoShim, isEnabled, checkerService)

    abstract DebugReadRealAssemblies: bool
    default this.DebugReadRealAssemblies = false

    override this.GetLastWriteTime(path) =
        if not (this.IsEnabled && AssemblyReaderShim.isAssembly path) then base.GetLastWriteTime(path) else

        match getOrCreateReader path with
        | ReferencedAssembly.ProjectOutput reader -> reader.Timestamp
        | _ -> base.GetLastWriteTime(path)

    override this.ExistsFile(path) =
        if not (this.IsEnabled && AssemblyReaderShim.isAssembly path) then base.ExistsFile(path) else

        match getOrCreateReader path with
        | ReferencedAssembly.ProjectOutput _ -> true
        | _ -> base.ExistsFile(path)

    override this.GetModuleReader(path, readerOptions) =
        if not (this.IsEnabled && AssemblyReaderShim.isAssembly path) then
            base.GetModuleReader(path, readerOptions) else

        match getOrCreateReader path with
        | ReferencedAssembly.Ignored -> base.GetModuleReader(path, readerOptions)
        | ReferencedAssembly.ProjectOutput reader ->

        if this.DebugReadRealAssemblies && reader.RealModuleReader.IsNone then
            try
                reader.RealModuleReader <- Some(this.DefaultReader.GetILModuleReader(path.FullPath, readerOptions))
            with _ -> ()

        reader :> _

    member this.GetModuleReader(pm: IPsiModule): ReferencedAssembly =
        let mutable reader = Unchecked.defaultof<_>
        if assemblyReadersByModule.TryGetValue(pm, &reader) then reader else ReferencedAssembly.Ignored

    member this.MarkDirty(typePart: TypePart) =
        use lock = locker.UsingWriteLock()

        let typeElement = typePart.TypeElement
        let psiModule = typeElement.Module

        if dependenciesToModules.ContainsKey(psiModule) then
            dirtyModules.Add(psiModule, typeElement.GetClrName().GetPersistent()) |> ignore

    member this.InvalidateDirtyDependencies(psiModule: IPsiModule) =
        Assertion.Assert(locker.IsWriteLockHeld, "locker.IsWriteLockHeld")

        if dirtyModules.IsEmpty() then () else

        for KeyValue(dirtyModule, dirtyTypeNames) in List.ofSeq dirtyModules do
            // todo: always invalidate all?
            if not (moduleDependencies.ContainsPair(psiModule, dirtyModule)) then () else

            let mutable referencedAssembly = Unchecked.defaultof<_>
            if not (assemblyReadersByModule.TryGetValue(dirtyModule, &referencedAssembly)) then () else

            match referencedAssembly with
            | ReferencedAssembly.Ignored -> ()
            | ReferencedAssembly.ProjectOutput(reader) ->

            for typeName in dirtyTypeNames do
                reader.InvalidateTypeDef(typeName)

            dirtyModules.RemoveKey(dirtyModule) |> ignore

    member this.ForceCreateTypeDefs(psiModule: IPsiModule): unit =
        let psiModule = psiModule.As<IProjectPsiModule>()
        if isNull psiModule then () else

        let path = psiModule.Project.GetOutputFilePath(psiModule.TargetFrameworkId)
        match getOrCreateReader path with
        | ReferencedAssembly.ProjectOutput(reader) -> reader.ForceCreateTypeDefs()
        | _ -> ()

    interface IFcsAssemblyReaderShim with
        member this.PrepareDependencies(psiModule) =
            if not this.IsEnabled then () else

            use lock = locker.UsingWriteLock()

            if not (moduleDependencies.ContainsKey(psiModule)) then
                recordDependencies psiModule

            this.InvalidateDirtyDependencies(psiModule)
            for dependencyModule in moduleDependencies.GetValuesSafe(psiModule) do
                this.ForceCreateTypeDefs(dependencyModule)


[<SolutionComponent>]
type SymbolCacheListener(lifetime: Lifetime, symbolCache: ISymbolCache, readerShim: AssemblyReaderShim) =
    let typePartChanged = Action<_>(readerShim.MarkDirty)
    do
        lifetime.Bracket(
            (fun () -> symbolCache.add_OnAfterTypePartAdded(typePartChanged)),
            (fun () -> symbolCache.remove_OnAfterTypePartAdded(typePartChanged)))

        lifetime.Bracket(
            (fun () -> symbolCache.add_OnBeforeTypePartRemoved(typePartChanged)),
            (fun () -> symbolCache.remove_OnBeforeTypePartRemoved(typePartChanged)))
