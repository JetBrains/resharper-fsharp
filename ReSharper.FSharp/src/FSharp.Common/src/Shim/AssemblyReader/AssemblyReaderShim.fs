namespace JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader

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

    let isEnabled settingsStore =
        SettingsUtil.getValue<FSharpOptions, bool> settingsStore "NonFSharpProjectInMemoryAnalysis" ||
        Shell.Instance.IsTestShell

[<SolutionComponent>]
type AssemblyReaderShim(lifetime: Lifetime, changeManager: ChangeManager, psiModules: IPsiModules,
        cache: FcsModuleReaderCommonCache, assemblyInfoShim: AssemblyInfoShim, settingsStore: ISettingsStore) =
    inherit AssemblyReaderShimBase(lifetime, changeManager, AssemblyReaderShim.isEnabled settingsStore)

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

    override this.GetLastWriteTime(path) =
        if not (this.IsEnabled && AssemblyReaderShim.isAssembly path) then base.GetLastWriteTime(path) else

        match getOrCreateReader path with
        | ReferencedAssembly.ProjectOutput reader -> reader.Timestamp
        | _ -> base.GetLastWriteTime(path)

    override this.Exists(path) =
        if not (this.IsEnabled && AssemblyReaderShim.isAssembly path) then base.Exists(path) else

        match getOrCreateReader path with
        | ReferencedAssembly.ProjectOutput _ -> true
        | _ -> base.Exists(path)

    override this.GetModuleReader(path, readerOptions) =
        if not (this.IsEnabled && AssemblyReaderShim.isAssembly path) then
            base.GetModuleReader(path, readerOptions) else

        match getOrCreateReader path with
        | ReferencedAssembly.ProjectOutput reader -> reader :> _
        | _ -> base.GetModuleReader(path, readerOptions)
