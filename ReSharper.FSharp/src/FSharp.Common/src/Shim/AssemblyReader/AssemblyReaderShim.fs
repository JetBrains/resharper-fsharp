namespace JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader

open System
open System.Collections.Concurrent
open JetBrains.Application.Settings
open JetBrains.Application.changes
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Properties
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Shim.FileSystem
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.Caches
open JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Resources.Shell

[<RequireQualifiedAccess>]
type ReferencedAssembly =
    /// An output of a psi source project except for F# projects.
    | ProjectOutput of ProjectFcsModuleReader

    /// Not supported file or output assembly for F# project.
    | Ignored

module AssemblyReaderShim =
    let isSupportedLanguage (language: ProjectLanguage) =
        language = ProjectLanguage.CSHARP || language = ProjectLanguage.VBASIC

    let isSupportedProjectKind (projectKind: ProjectKind) =
        match projectKind with
        | ProjectKind.REGULAR_PROJECT
        | ProjectKind.WEB_SITE -> true
        | _ -> false

    let isSupportedProject (project: IProject) =
        isNotNull project &&

        let projectProperties = project.ProjectProperties

        isSupportedLanguage projectProperties.DefaultLanguage &&
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


[<SolutionComponent>]
type AssemblyReaderShim(lifetime: Lifetime, changeManager: ChangeManager, psiModules: IPsiModules,
        cache: FcsModuleReaderCommonCache, assemblyInfoShim: AssemblyInfoShim, isEnabled: bool) =
    inherit AssemblyReaderShimBase(lifetime, changeManager, isEnabled)

    static let debugReadRealAssemblies = false

    // The shim is injected to get the expected shim shadowing chain, it's expected to be unused. 
    do assemblyInfoShim |> ignore

    let assemblyReadersByPath = ConcurrentDictionary<FileSystemPath, ReferencedAssembly>()
    let assemblyReadersByModule = ConcurrentDictionary<IPsiModule, ReferencedAssembly>()

    let createReader (path: FileSystemPath) =
        use readLockCookie = ReadLockCookie.Create()
        match AssemblyReaderShim.getProjectPsiModuleByOutputAssembly psiModules path with
        | null -> ReferencedAssembly.Ignored
        | psiModule -> ReferencedAssembly.ProjectOutput(new ProjectFcsModuleReader(psiModule, cache))

    let getOrCreateReader path =
        match assemblyReadersByPath.TryGetValue(path) with
        | true, reader -> reader
        | _ ->

        let reader = createReader path
        assemblyReadersByPath.[path] <- reader

        match reader with
        | ReferencedAssembly.ProjectOutput moduleReader ->
            assemblyReadersByModule.[moduleReader.PsiModule] <- reader
        | _ -> ()

        reader

    new (lifetime: Lifetime, changeManager: ChangeManager, psiModules: IPsiModules, cache: FcsModuleReaderCommonCache,
            assemblyInfoShim: AssemblyInfoShim, settingsStore: ISettingsStore) =
        let isEnabled = SettingsUtil.getValue<FSharpOptions, bool> settingsStore "NonFSharpProjectInMemoryAnalysis"
        AssemblyReaderShim(lifetime, changeManager, psiModules, cache, assemblyInfoShim, isEnabled)

    abstract DebugReadRealAssemblies: bool
    default this.DebugReadRealAssemblies = true

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

        if debugReadRealAssemblies && reader.RealModuleReader.IsNone then
            try
                reader.RealModuleReader <- Some(this.DefaultReader.GetILModuleReader(path.FullPath, readerOptions))
            with _ -> ()

        reader :> _

    member this.GetModuleReader(pm: IPsiModule): ReferencedAssembly =
        match assemblyReadersByModule.TryGetValue(pm) with
        | true, reader -> reader
        | _ -> ReferencedAssembly.Ignored

[<SolutionComponent>]
type SymbolCacheListener(lifetime: Lifetime, symbolCache: ISymbolCache, readerShim: AssemblyReaderShim) =
    let typePartChanged =
        Action<_>(fun (typePart: TypePart) ->
            match readerShim.GetModuleReader(typePart.GetPsiModule()) with
            | ReferencedAssembly.ProjectOutput reader ->
                let clrTypeName = typePart.TypeElement.GetClrName()
                reader.InvalidateTypeDef(clrTypeName)
            | _ -> ())
    do
        lifetime.Bracket(
            Func<_>(fun _ -> symbolCache.add_OnAfterTypePartAdded(typePartChanged)),
            Action(fun _-> symbolCache.remove_OnAfterTypePartAdded(typePartChanged)))

        lifetime.Bracket(
            Func<_>(fun _ -> symbolCache.add_OnBeforeTypePartRemoved(typePartChanged)),
            Action(fun _ -> symbolCache.remove_OnBeforeTypePartRemoved(typePartChanged)))
