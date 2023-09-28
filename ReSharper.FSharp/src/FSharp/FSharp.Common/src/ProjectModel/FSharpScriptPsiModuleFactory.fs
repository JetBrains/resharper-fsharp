namespace rec JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Runtime.InteropServices
open FSharp.Compiler.CodeAnalysis
open JetBrains.Application
open JetBrains.Application.Progress
open JetBrains.Application.Threading
open JetBrains.Application.changes
open JetBrains.DataFlow
open JetBrains.DocumentManagers
open JetBrains.Lifetimes
open JetBrains.Metadata.Reader.API
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Assemblies.Impl
open JetBrains.ProjectModel.Model2.Assemblies.Interfaces
open JetBrains.ProjectModel.Platforms
open JetBrains.ProjectModel.model2.Assemblies.Impl
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Impl
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util
open JetBrains.Util.DataStructures
open JetBrains.Util.Dotnet.TargetFrameworkIds

/// Provides psi modules for script files with referenced assemblies determined by "#r" directives.
[<SolutionComponent>]
type FSharpScriptPsiModulesProvider(lifetime: Lifetime, solution: ISolution, changeManager: ChangeManager,
        documentManager: DocumentManager, scriptOptionsProvider: IScriptFcsProjectProvider,
        platformManager: IPlatformManager, assemblyFactory: AssemblyFactory, projectFileExtensions,
        projectFileTypeCoordinator, checkerService: FcsCheckerService) as this =

    let scriptPsiModuleInvalidated = new Signal<FSharpScriptPsiModule>("ScriptPsiModuleInvalidated")

    /// There may be multiple project files for a path (i.e. linked in multiple projects) and we must distinguish them.
    let scriptsFromProjectFiles = OneToListMap<VirtualFileSystemPath, FSharpScriptPsiModule>()

    /// Psi modules for files coming from #load directives and do not present in the project model.
    let scriptsFromPaths = Dictionary<VirtualFileSystemPath, FSharpScriptPsiModule>()

    /// References to assemblies and other source files for each known script path.
    let scriptsReferences = Dictionary<VirtualFileSystemPath, ScriptReferences>()

    let psiModules = List<IPsiModule>()
    let mutable psiModulesCollection = HybridCollection.Empty

    let locks = solution.Locks

    let targetFrameworkId =
        let platformInfos = platformManager.GetAllCompilePlatforms().AsList()
        if platformInfos.IsEmpty() then TargetFrameworkId.Default else

        let platformInfo = platformInfos |> Seq.maxBy (fun info -> info.TargetFrameworkId.Version)
        platformInfo.TargetFrameworkId

    let getScriptReferences (scriptPath: VirtualFileSystemPath) scriptOptions =
        let assembliesPaths = HashSet<VirtualFileSystemPath>()
        for o in scriptOptions.OtherOptions do
            if o.StartsWith("-r:", StringComparison.Ordinal) then
                let path = VirtualFileSystemPath.TryParse(o.Substring(3), InteractionContext.SolutionContext)
                if not path.IsEmpty then assembliesPaths.Add(path) |> ignore

        let filesPaths = HashSet<VirtualFileSystemPath>()
        for file in scriptOptions.SourceFiles do
            let path = VirtualFileSystemPath.TryParse(file, InteractionContext.SolutionContext)
            if not path.IsEmpty && not (path.Equals(scriptPath)) then
                filesPaths.Add(path) |> ignore

        { Assemblies = assembliesPaths
          Files = filesPaths }

    let getPsiModulesForPath path =
        let projectFilesScripts = scriptsFromProjectFiles.GetValuesSafe(path)

        let mutable pathScript = Unchecked.defaultof<_>
        match scriptsFromPaths.TryGetValue(path, &pathScript) with
        | true ->
            seq {
                yield! projectFilesScripts
                yield  pathScript
            }
        | _ -> projectFilesScripts :> _

    let addPsiModule psiModule =
        psiModules.Add(psiModule)
        psiModulesCollection <- HybridCollection<IPsiModule>(psiModules)

    let removePsiModule psiModule =
        psiModules.Remove(psiModule) |> ignore
        psiModulesCollection <- HybridCollection<IPsiModule>(psiModules)

    let createSourceFileForProjectFile projectFile (psiModule: FSharpScriptPsiModule) =
        PsiProjectFile
            (psiModule, projectFile, (fun _ _ -> ScriptFileProperties.Instance),
             (fun pf _ -> pf.IsValid()), documentManager, psiModule.ResolveContext) :> IPsiSourceFile

    let createSourceFileForPath path (psiModule: FSharpScriptPsiModule) =
        NavigateablePsiSourceFileWithLocation
            (projectFileExtensions, projectFileTypeCoordinator, psiModule, path, (fun _ -> psiModule.IsValid),
             (fun _ -> ScriptFileProperties.Instance), documentManager, psiModule.ResolveContext) :> IPsiSourceFile

    let rec createPsiModule path id sourceFileCtor (changeBuilder: PsiModuleChangeBuilder) =
        let psiModule = FSharpScriptPsiModule(lifetime, path, solution, sourceFileCtor, id, assemblyFactory, this)
        changeBuilder.AddModuleChange(psiModule, PsiModuleChange.ChangeType.Added)
        changeBuilder.AddFileChange(psiModule.SourceFile, PsiModuleChange.ChangeType.Added)
        psiModule

    and createPsiModuleForPath (path: VirtualFileSystemPath) changeBuilder =
        let modulesForPath = getPsiModulesForPath path
        if modulesForPath.IsEmpty() then
            let sourceFileCtor = createSourceFileForPath path
            let psiModule = createPsiModule path path.FullPath sourceFileCtor changeBuilder

            scriptsFromPaths[path] <- psiModule
            addPsiModule psiModule

    and updateReferences path newReferences added (removed: _ list) changeBuilder =
        use cookie = WriteLockCookie.Create()
        scriptsReferences[path] <- newReferences

        for filePath in newReferences.Files do
            createPsiModuleForPath filePath changeBuilder

        for psiModule in getPsiModulesForPath path do
            for path in added do psiModule.AddReference(path)
            for path in removed do psiModule.RemoveReference(path)
            if not scriptOptionsProvider.SyncUpdate then
                changeBuilder.AddModuleChange(psiModule, PsiModuleChange.ChangeType.Invalidated)
                scriptPsiModuleInvalidated.Fire(psiModule)

        solution.GetComponent<IDaemon>().Invalidate()

        if not scriptOptionsProvider.SyncUpdate then
            changeManager.OnProviderChanged(this, changeBuilder.Result, SimpleTaskExecutor.Instance)

        if not removed.IsEmpty then
            locks.QueueReadLock(lifetime, "AssemblyGC after removing F# script reference", fun _ ->
                solution.GetComponent<AssemblyGC>().ForceGC())

    and queueUpdateReferences (path: VirtualFileSystemPath) (newOptions: FSharpProjectOptions) =
        locks.QueueReadLock(lifetime, "Request new F# script references", fun _ ->
            let oldReferences =
                let mutable oldReferences = Unchecked.defaultof<ScriptReferences>
                if scriptsReferences.TryGetValue(path, &oldReferences) then
                    oldReferences
                else
                    ScriptReferences.Empty

            let ira = InterruptableReadActivityThe(lifetime, locks)

            ira.FuncRun <-
                fun _ ->
                    let newReferences = getScriptReferences path newOptions
                    Interruption.Current.CheckAndThrow()

                    let getDiff oldPaths newPaths =
                        let notChanged = Enumerable.Intersect(newPaths, oldPaths) |> HashSet
                        let filterChanges = Seq.filter (notChanged.Contains >> not) >> List.ofSeq
                        filterChanges newPaths, filterChanges oldPaths

                    match getDiff oldReferences.Assemblies newReferences.Assemblies with
                    | [], [] when newReferences.Files.SetEquals(oldReferences.Files) -> ()
                    | added, removed ->

                    Interruption.Current.CheckAndThrow()
                    locks.ExecuteOrQueue(lifetime, "Update F# script references", fun _ ->
                        let changeBuilder = PsiModuleChangeBuilder()
                        updateReferences path newReferences added removed changeBuilder
                    )

            ira.FuncCancelled <-
                // Reschedule again
                fun _ -> queueUpdateReferences path newOptions

            ira.DoStart())

    do
        changeManager.RegisterChangeProvider(lifetime, this)

        if not scriptOptionsProvider.SyncUpdate then
            scriptOptionsProvider.OptionsUpdated.Advise(lifetime, fun (path, options) ->
                queueUpdateReferences path options
            )

    member x.CreatePsiModuleForProjectFile(projectFile: IProjectFile, changeBuilder: PsiModuleChangeBuilder,
            [<Out>] resultModule: byref<FSharpScriptPsiModule>) =

        locks.AssertWriteAccessAllowed()
        let path = projectFile.Location

        // If previously created a psi module for the path then replace it with a psi module for the project file
        let mutable psiModule = Unchecked.defaultof<FSharpScriptPsiModule>
        match scriptsFromPaths.TryGetValue(path, &psiModule) with
        | true ->
            scriptsFromPaths.Remove(path) |> ignore
            removePsiModule psiModule
            psiModule.LifetimeDefinition.Terminate()

            changeBuilder.AddModuleChange(psiModule, PsiModuleChange.ChangeType.Removed)
            changeBuilder.AddFileChange(psiModule.SourceFile, PsiModuleChange.ChangeType.Removed)
        | _ -> ()

        let moduleId = projectFile.GetPersistentID()
        let existingModule =
            scriptsFromProjectFiles.GetValuesSafe(path)
            |> Seq.tryFind (fun psiModule -> psiModule.PersistentID = moduleId)

        match existingModule with
        | Some _ -> false
        | _ ->

        let sourceFileCtor = createSourceFileForProjectFile projectFile
        let psiModule = createPsiModule path moduleId sourceFileCtor changeBuilder 
        scriptsFromProjectFiles.Add(path, psiModule)
        addPsiModule psiModule
        resultModule <- psiModule

        if scriptOptionsProvider.SyncUpdate then
            scriptOptionsProvider.GetFcsProject(psiModule.SourceFile)
            |> Option.iter (fun fcsProject ->
                let references = getScriptReferences path fcsProject.ProjectOptions
                updateReferences path references references.Assemblies [] changeBuilder 
            )

        true

    member x.CreatePsiModuleForPath(path: VirtualFileSystemPath, changeBuilder: PsiModuleChangeBuilder) =
        locks.AssertWriteAccessAllowed()
        createPsiModuleForPath path changeBuilder

    member x.GetReferencedScriptPsiModules(psiModule: FSharpScriptPsiModule) =
        let sameProjectSorter =
            match psiModule.SourceFile with
            | :? IPsiProjectFile as psiProjectFile ->
                let project = psiProjectFile.GetProject()
                fun (psiModule: FSharpScriptPsiModule) ->
                    match psiModule.SourceFile with
                    | :? IPsiProjectFile as psiProjectFile when psiProjectFile.GetProject() = project -> 1
                    | _ -> 0
            | _ -> fun _ -> 0

        let mutable paths = Unchecked.defaultof<ScriptReferences>
        match scriptsReferences.TryGetValue(psiModule.Path, &paths) with
        | true -> paths.Files |> Seq.map (getPsiModulesForPath >> Seq.maxBy sameProjectSorter)
        | _ -> EmptyList.Instance :> _

    member x.RemoveProjectFilePsiModule(moduleToRemove: FSharpScriptPsiModule, changeBuilder: PsiModuleChangeBuilder) =
        let path = moduleToRemove.Path
        scriptsFromProjectFiles.GetValuesSafe(path)
        |> Seq.tryFind (fun psiModule -> psiModule.Path = moduleToRemove.Path)
        |> Option.iter (fun psiModule ->
            match checkerService.GetCachedScriptOptions(path.FullPath) with
            | Some options -> checkerService.InvalidateFcsProject(options)
            | None -> ()

            scriptsFromProjectFiles.RemoveValue(path, psiModule) |> ignore
            removePsiModule psiModule

            psiModule.LifetimeDefinition.Terminate()
            
            changeBuilder.AddModuleChange(psiModule, PsiModuleChange.ChangeType.Removed)
            changeBuilder.AddFileChange(psiModule.SourceFile, PsiModuleChange.ChangeType.Removed)
            scriptPsiModuleInvalidated.Fire(psiModule))

    member x.GetPsiModulesForPath(path) =
        getPsiModulesForPath path

    member x.TargetFrameworkId =
        targetFrameworkId

    member x.Dump(writer: TextWriter) =
        writer.WriteLine("Scripts from paths:")
        for KeyValue (path, _) in scriptsFromPaths do
            writer.WriteLine("  " + path.ToString())

        writer.WriteLine("Scripts from project files:")
        for fsPsiModule in scriptsFromProjectFiles.Values do
            writer.WriteLine("  " + fsPsiModule.SourceFile.ToProjectFile().GetPersistentID())

    member x.ModuleInvalidated = scriptPsiModuleInvalidated

    interface IProjectPsiModuleProviderFilter with
        member x.OverrideHandler(lifetime, _, handler) =
            let handler =
                FSharpScriptPsiModuleHandler(lifetime, solution, handler, this, changeManager)
            handler :> _, null

    interface IPsiModuleFactory with
        member x.Modules = psiModulesCollection

    interface IChangeProvider with
        member x.Execute _ = null


/// Overriding psi module handler for each project (a real project, misc files project, solution folder, etc).
type FSharpScriptPsiModuleHandler(lifetime, solution, handler, modulesProvider, changeManager) as this =
    inherit DelegatingProjectPsiModuleHandler(handler)

    let locks = solution.Locks
    let sourceFiles = Dictionary<VirtualFileSystemPath, IPsiSourceFile>()

    do
        lifetime.OnTermination(fun _ ->
            changeManager.ExecuteAfterChange(fun _ ->
                let changeBuilder = PsiModuleChangeBuilder()
                for sourceFile in sourceFiles.Values do
                    let psiModule = sourceFile.PsiModule :?> FSharpScriptPsiModule
                    psiModule.RemoveProjectHandler(this, changeBuilder)
                changeManager.OnProviderChanged(modulesProvider, changeBuilder.Result, SimpleTaskExecutor.Instance))) |> ignore

    /// Prevents creating default psi source files for scripts and adds new psi modules with source files instead.
    override x.OnProjectFileChanged(projectFile, oldLocation, changeType, changeBuilder) =
        locks.AssertWriteAccessAllowed()
        match changeType with
        | PsiModuleChange.ChangeType.Added when
                projectFile.LanguageType.Is<FSharpScriptProjectFileType>() ->
            let mutable psiModule = Unchecked.defaultof<FSharpScriptPsiModule>
            if modulesProvider.CreatePsiModuleForProjectFile(projectFile, changeBuilder, &psiModule) then
                psiModule.AddProjectHandler(this)
                sourceFiles[projectFile.Location] <- psiModule.SourceFile

        | PsiModuleChange.ChangeType.Removed when sourceFiles.ContainsKey(oldLocation) ->
            let sourceFile = sourceFiles[oldLocation]
            let psiModule = sourceFile.PsiModule :?> FSharpScriptPsiModule
            psiModule.RemoveProjectHandler(this, changeBuilder)
            sourceFiles.Remove(oldLocation) |> ignore

        | _ -> handler.OnProjectFileChanged(projectFile, oldLocation, changeType, changeBuilder)

    override x.GetPsiSourceFilesFor(projectFile) =
        let defaultSourceFiles = handler.GetPsiSourceFilesFor(projectFile)

        let mutable sourceFile = Unchecked.defaultof<IPsiSourceFile>
        match sourceFiles.TryGetValue(projectFile.Location, &sourceFile) with
        | true ->
            seq {
                yield sourceFile
                yield! defaultSourceFiles
            }
        | _ -> defaultSourceFiles

    override x.InternalsVisibleTo(moduleTo, moduleFrom) =
        moduleTo :? FSharpScriptPsiModule && moduleFrom :? FSharpScriptPsiModule ||
        handler.InternalsVisibleTo(moduleTo, moduleFrom)


type FSharpScriptPsiModule(lifetime, path, solution, sourceFileCtor, moduleId, assemblyFactory,
        modulesProvider) as this =
    inherit ConcurrentUserDataHolder()

    let lifetimeDefinition = Lifetime.Define(lifetime)
    let lifetime = lifetimeDefinition.Lifetime

    let psiServices = solution.GetPsiServices()
    let psiModules = solution.PsiModules()

    let containingModule = FSharpScriptModule(path, solution)
    let sourceFile = lazy sourceFileCtor this
    let resolveContext = lazy PsiModuleResolveContext(this, modulesProvider.TargetFrameworkId, null)

    // There is a separate project handler for each target framework.
    // Each overriden handler provides a psi module and a source file for each script project file.
    // We create at most one psi module for each project file and update list of handlers pointing to it.
    let projectHandlers = List<IProjectPsiModuleHandler>()

    let assemblyCookies = DictionaryEvents<VirtualFileSystemPath, IAssemblyCookie>(moduleId)

    do
        assemblyCookies.AddRemove.Advise_Remove(lifetime, fun (AddRemoveArgs (KeyValue (_, assemblyCookie))) ->
            solution.Locks.AssertWriteAccessAllowed()
            assemblyCookie.Dispose())

        lifetime.OnTermination(fun _ ->
            use lock = WriteLockCookie.Create()
            assemblyCookies.Clear()) |> ignore

    member x.Path = path
    member x.SourceFile: IPsiSourceFile = sourceFile.Value
    member x.ResolveContext = resolveContext.Value
    member x.PersistentID = moduleId
    member x.LifetimeDefinition: LifetimeDefinition = lifetimeDefinition

    member x.PsiServices = psiServices
    member x.PsiModules = solution.PsiModules()

    member x.IsValid = psiServices.Modules.HasModule(this)

    member x.AddReference(path: VirtualFileSystemPath) =
        solution.Locks.AssertWriteAccessAllowed()
        if not (assemblyCookies.ContainsKey(path)) then
            assemblyCookies.Add(path, assemblyFactory.AddRef(AssemblyLocation(path), moduleId, this.ResolveContext))

    member x.RemoveReference(path: VirtualFileSystemPath) =
        solution.Locks.AssertWriteAccessAllowed()
        if assemblyCookies.ContainsKey(path) then
            assemblyCookies.Remove(path) |> ignore

    member x.AddProjectHandler(handler: FSharpScriptPsiModuleHandler) =
        projectHandlers.Add(handler)

    member x.RemoveProjectHandler(handler: FSharpScriptPsiModuleHandler, changeBuilder: PsiModuleChangeBuilder) =
        projectHandlers.Remove(handler) |> ignore
        if projectHandlers.IsEmpty() then
            modulesProvider.RemoveProjectFilePsiModule(this, changeBuilder)

    override x.ToString() =
        let typeName = this.GetType().Name
        sprintf "%s(%s)" typeName path.FullPath

    interface IPsiModule with
        member x.Name = "F# script: " + path.Name
        member x.DisplayName = "F# script: " + path.Name
        member x.GetPersistentID() = moduleId

        member x.GetSolution() = solution
        member x.GetPsiServices() = x.PsiServices

        member x.SourceFiles = [x.SourceFile] :> _
        member x.TargetFrameworkId = modulesProvider.TargetFrameworkId
        member x.ContainingProjectModule = containingModule :> _

        member x.GetReferences _ =
            solution.Locks.AssertReadAccessAllowed()
            let result = LocalList<IPsiModuleReference>()
            for assemblyCookie in assemblyCookies.Values do
                match psiModules.GetPrimaryPsiModule(assemblyCookie.Assembly, modulesProvider.TargetFrameworkId) with
                | null -> ()
                | psiModule -> result.Add(PsiModuleReference(psiModule))

            for psiModule in modulesProvider.GetReferencedScriptPsiModules(this) do
                 result.Add(PsiModuleReference(psiModule))

            result.ResultingList() :> _

        member x.PsiLanguage = FSharpLanguage.Instance :> _
        member x.ProjectFileType = UnknownProjectFileType.Instance :> _

        member x.GetAllDefines() = EmptyList.InstanceList :> _
        member x.IsValid() = x.IsValid


type IFSharpFileService =
    /// True when file is script or an IntelliJ scratch file.
    abstract member IsScriptLike: IPsiSourceFile -> bool

    /// True when file is an IntelliJ scratch file.
    abstract member IsScratchFile: VirtualFileSystemPath -> bool


/// Holder for psi module resolve context.
type FSharpScriptModule(path: VirtualFileSystemPath, solution: ISolution) =
    inherit UserDataHolder()

    static let scratchesPath = RelativePath.TryParse("Scratches")

    let path: IPath =
        if Shell.Instance.GetComponent<IFSharpFileService>().IsScratchFile(path) then
            scratchesPath / path.Name :> _
        else
            let driveName = path.GetDriveName()
            let solutionDriveName = solution.SolutionDirectory.GetDriveName()

            if driveName <> solutionDriveName then path :> _ else
            path.MakeRelativeTo(solution.SolutionDirectory) :> _

    interface IModule with
        member x.Presentation = path.Name
        member x.Name = path.FullPath

        member x.IsValid() = solution.IsValid()
        member x.IsValidAndAlive() = solution.IsValidAndAlive()
        member x.GetSolution() = solution
        member x.Accept(visitor) = visitor.VisitProjectModelElement(x)
        member x.MarshallerType = null

        member x.GetProperty(key) = base.GetData(key)
        member x.SetProperty(key, value) = base.PutData(key, value)


type ScriptFileProperties() =
    interface IPsiSourceFileProperties with
        member x.ShouldBuildPsi = true
        member x.ProvidesCodeModel = true
        member x.IsICacheParticipant = true
        member x.IsGeneratedFile = false
        member x.IsNonUserFile = false

        member x.GetPreImportedNamespaces() = EmptyList.Instance :> _
        member x.GetDefaultNamespace() = String.Empty
        member x.GetDefines() = EmptyList.Instance :> _

    static member val Instance = ScriptFileProperties() :> IPsiSourceFileProperties


type ScriptReferences =
    { Assemblies: ISet<VirtualFileSystemPath>
      Files: ISet<VirtualFileSystemPath> }

    static member Empty =
        { Assemblies = EmptySet.Instance
          Files = EmptySet.Instance }


[<SolutionFeaturePart>]
type FSharpScriptLanguageLevelProvider(scriptSettingsProvider: FSharpScriptSettingsProvider) =
    let getLanguageLevel () =
        FSharpLanguageLevel.ofLanguageVersion scriptSettingsProvider.LanguageVersion.Value

    interface ILanguageLevelProvider<FSharpLanguageLevel, FSharpLanguageVersion> with
        member this.IsApplicable(psiModule) =
            psiModule :? FSharpScriptPsiModule

        member this.GetLanguageLevel _ =
            getLanguageLevel ()

        member this.ConvertToLanguageLevel(languageVersion, _) =
            FSharpLanguageLevel.ofLanguageVersion languageVersion

        member this.ConvertToLanguageVersion(languageLevel) =
            FSharpLanguageLevel.toLanguageVersion languageLevel

        member this.IsAvailable(languageLevel: FSharpLanguageLevel, _: IPsiModule): bool =
            languageLevel <= getLanguageLevel ()

        member this.TryGetLanguageVersion _ =
            Nullable(scriptSettingsProvider.LanguageVersion.Value)

        member this.IsAvailable(_: FSharpLanguageVersion, _: IPsiModule): bool = failwith "todo"
        member this.LanguageLevelOverrider = failwith "todo"
        member this.LanguageVersionModifier = failwith "todo"
        member this.GetLatestAvailableLanguageLevel _ = failwith "todo"
