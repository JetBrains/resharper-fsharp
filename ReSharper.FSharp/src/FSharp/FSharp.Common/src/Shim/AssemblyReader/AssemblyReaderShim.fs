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
        cache: FcsModuleReaderCommonCache, assemblyInfoShim: AssemblyInfoShim,
        fsOptionsProvider: FSharpOptionsProvider, symbolCache: ISymbolCache, solution: ISolution,
        locks: IShellLocks, logger: ILogger) as this =
    inherit AssemblyReaderShimBase(lifetime, changeManager)

    let assemblyReadersByModule = ConcurrentDictionary<IPsiModule, IProjectFcsModuleReader>()
    let assemblyReadersByPath = ConcurrentDictionary<VirtualFileSystemPath, IProjectFcsModuleReader>()

    let projectKeyToPsiModules = ConcurrentDictionary<FcsProjectKey, IPsiModule>()

    /// Modules invalidated by symbol cache or are known to read incomplete contents.
    /// Readers need to check up to date before new FCS requests.
    let dirtyProjects = HashSet()

    let dirtyTypesInModules = OneToSetMap<IPsiModule, string>()

    let projectInvalidated = new Signal<FcsProjectKey>("AssemblyReaderShim.ModuleInvalidated")

    do
        // The shim is injected to get the expected shim shadowing chain, it's expected to be unused.
        assemblyInfoShim |> ignore
    
        changeManager.RegisterChangeProvider(lifetime, this)
        changeManager.AddDependency(lifetime, this, psiModules)
    
    let isEnabled () =
        fsOptionsProvider.NonFSharpProjectInMemoryReferences ||
        FSharpExperimentalFeatureCookie.IsEnabled(ExperimentalFeature.AssemblyReaderShim)

    let getFcsProjectProvider () = solution.GetComponent<IFcsProjectProvider>()

    let getReferencingModules (projectKey: FcsProjectKey) =
        let fcsProjectProvider = getFcsProjectProvider ()
        match fcsProjectProvider.GetReferencedModule(projectKey) with
        | None -> Seq.empty
        | Some referencedModule -> referencedModule.ReferencingProjects

    let isKnownModule (psiModule: IPsiModule) =
        if not (psiModule.ContainingProjectModule :? IProject) then false else

        let fcsProjectProvider = getFcsProjectProvider ()

        assemblyReadersByModule.ContainsKey(psiModule) ||

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

        // todo: better sync with project model
        for referencingModule in getReferencingModules projectKey do
            projectInvalidated.Fire(referencingModule)

    // todo: invalidate for per-referencing module
    let markDirtyDependencies () =
        let invalidatedModules = HashSet()

        let modulesToInvalidate = Stack<FcsProjectKey>(dirtyProjects)

        for dirtyModule in dirtyTypesInModules.Keys do
            match dirtyModule.ContainingProjectModule with
            | :? IProject ->
                match tryGetReaderFromModule dirtyModule with
                | None -> ()
                | Some reader ->
                    for typeName in dirtyTypesInModules.GetValuesSafe(dirtyModule) do
                        reader.InvalidateTypeDefs(typeName)

                let projectKey = FcsProjectKey.Create(dirtyModule)
                if invalidatedModules.Add(projectKey) then
                    modulesToInvalidate.Push(projectKey)
                    projectInvalidated.Fire(projectKey)

            | _ -> ()

        while modulesToInvalidate.Count > 0 do
            let projectKey = modulesToInvalidate.Pop()

            for referencingProjectKey in getReferencingModules projectKey do
                let referencingModule = projectKeyToPsiModules.TryGetValue(referencingProjectKey)
                if isNull referencingModule then () else

                tryGetReaderFromModule referencingModule
                |> Option.iter (fun reader -> reader.MarkDirty())

                if invalidatedModules.Add(referencingProjectKey) then
                    modulesToInvalidate.Push(referencingProjectKey)
                    projectInvalidated.Fire(referencingProjectKey)

        dirtyTypesInModules.Clear()

    let markTypePartDirty (typePart: TypePart) =
        if not (isEnabled ()) then () else
        if assemblyReadersByModule.Count = 0 then () else

        let typeElement = typePart.TypeElement
        let psiModule = typeElement.Module

        // todo: filter modules to only listen to C#/VB or referenced F# projects
        dirtyTypesInModules.Add(psiModule, typeElement.ShortName) |> ignore

    let invalidateDirty () =
        locks.AssertReadAccessAllowed()

        markDirtyDependencies ()
        dirtyProjects.Clear()

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

        member this.ProjectInvalidated = projectInvalidated

        member this.TryGetModuleReader(projectKey: FcsProjectKey) =
            locks.AssertWriteAccessAllowed()

            getOrCreateReaderFromModule projectKey

        member this.InvalidateDirty() =
            locks.AssertReadAccessAllowed()

            invalidateDirty ()

        member this.InvalidateDirty(psiModule) =
            tryGetReaderFromModule psiModule
            |> Option.iter (fun reader -> reader.UpdateTimestamp())
            

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

            if dirtyProjects.Count > 0 then
                builder.AppendLine($"Dirty projects: {dirtyProjects.Count}") |> ignore
                for projectKey in dirtyProjects do
                    builder.AppendLine($"    {projectKey.Project.Name}, IsValid: {projectKey.Project.IsValid()}") |> ignore

            if dirtyTypesInModules.Count > 0 then
                builder.AppendLine("Dirty types in readers:") |> ignore
                for psiModule in dirtyTypesInModules.Keys do
                    builder.AppendLine($"  {psiModule.DisplayName}, IsValid: {psiModule.IsValid()}") |> ignore
                    for typeName in dirtyTypesInModules.GetValuesSafe(psiModule) do
                        builder.AppendLine($"    {typeName}") |> ignore

            // if invalidationsSinceLastTestDump.Count > 0 then
            //     builder.AppendLine("Invalidations since last dump:") |> ignore
            //     while invalidationsSinceLastTestDump.Count > 0 do
            //         builder.AppendLine($"  {invalidationsSinceLastTestDump.Dequeue()}") |> ignore

            builder.ToString()

        member this.IsKnownModule(psiModule: IPsiModule) =
            assemblyReadersByModule.ContainsKey(psiModule)

        member this.IsKnownModule(path: VirtualFileSystemPath) =
            assemblyReadersByPath.ContainsKey(path)

        member this.HasDirtyModules =
            locks.AssertReadAccessAllowed()

            not (dirtyProjects.IsEmpty() && dirtyTypesInModules.IsEmpty())

        member this.Logger = logger

        member this.MarkDirty(psiModule) =
            if isKnownModule psiModule then
                let projectKey = FcsProjectKey.Create(psiModule)
                dirtyProjects.Add(projectKey) |> ignore

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
                    let projectKey = FcsProjectKey.Create(change.Item)
                    dirtyProjects.Add(projectKey) |> ignore

                | PsiModuleChange.ChangeType.Removed ->
                    removeModule change.Item

                | _ -> ()

            null
