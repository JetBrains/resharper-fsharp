module rec JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts

open System
open System.Collections.Generic
open System.Linq
open JetBrains.Application
open JetBrains.Application.Progress
open JetBrains.Application.Threading
open JetBrains.Application.changes
open JetBrains.Application.platforms
open JetBrains.DataFlow
open JetBrains.DocumentManagers
open JetBrains.DocumentManagers.impl
open JetBrains.DocumentModel
open JetBrains.Metadata.Reader.API
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Assemblies.Impl
open JetBrains.ProjectModel.Model2.Assemblies.Impl
open JetBrains.ProjectModel.Model2.Assemblies.Interfaces
open JetBrains.ProjectModel.Transaction
open JetBrains.ProjectModel.model2.Assemblies.Impl
open JetBrains.ReSharper.Plugins.FSharp.Common.Checker
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Impl
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util
open JetBrains.Util.DataStructures
open JetBrains.Util.Dotnet.TargetFrameworkIds
open Microsoft.FSharp.Compiler.SourceCodeServices

/// Provides psi modules for script files with referenced assemblies determined by "#r" directives.
[<SolutionComponent>]
type FSharpScriptPsiModulesProvider
        (lifetime: Lifetime, solution: ISolution, changeManager: ChangeManager, documentManager: DocumentManager,
         checkerService: FSharpCheckerService, platformManager: PlatformManager, assemblyFactory: AssemblyFactory,
         projectFileExtensions, projectFileTypeCoordinator, logger: ILogger) as this =

    /// There may be multiple project files for a path (i.e. linked in multiple projects) and we must distinguish them.
    let scriptsFromProjectFiles = OneToListMap<FileSystemPath, FSharpScriptPsiModule>()

    /// Psi modules for files coming from #load directives and do not present in the project model.
    let scriptsFromPaths = Dictionary<FileSystemPath, FSharpScriptPsiModule>()

    /// References to assemblies and other source files for each known script path.
    let scriptsReferences = Dictionary<FileSystemPath, ScriptReferences>()

    let psiModules = List<IPsiModule>()
    let mutable psiModulesCollection = HybridCollection.Empty

    do
        changeManager.RegisterChangeProvider(lifetime, this)
        changeManager.Changed2.Advise(lifetime, this.Execute)

    let locks = solution.Locks
    let checker = checkerService.Checker

    let targetFrameworkId =
        let platformInfos = platformManager.GetAllPlatformInfos().AsList()
        if platformInfos.IsEmpty() then TargetFrameworkId.Default else

        let platformInfo = platformInfos |> Seq.maxBy (fun info -> info.TargetFrameworkId.Version)
        platformInfo.TargetFrameworkId

    let getScriptOptions (path: FileSystemPath) (document: IDocument) =
        let options, _ = checker.GetProjectOptionsFromScript(path.FullPath, document.GetText()).RunAsTask()
        options

    let getScriptReferences scriptPath scriptOptions =
        let assembliesPaths = HashSet<FileSystemPath>()
        for o in scriptOptions.OtherOptions do
            if o.StartsWith("-r:", StringComparison.Ordinal) then
                let path = FileSystemPath.TryParse(o.Substring(3))
                if not path.IsEmpty then assembliesPaths.Add(path) |> ignore

        let filesPaths = HashSet<FileSystemPath>()
        for file in scriptOptions.SourceFiles do
            let path = FileSystemPath.TryParse(file)
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

    let rec createPsiModule path document id sourceFileCtor (changeBuilder: PsiModuleChangeBuilder) =
        let psiModule = FSharpScriptPsiModule(lifetime, path, solution, sourceFileCtor, id, assemblyFactory, this)

        changeBuilder.AddModuleChange(psiModule, PsiModuleChange.ChangeType.Added)
        changeBuilder.AddFileChange(psiModule.SourceFile, PsiModuleChange.ChangeType.Added)

        let scriptOptions = getScriptOptions path document
        let references = getScriptReferences path scriptOptions
        scriptsReferences.[path] <- references

        for filePath in references.Files do
            createPsiModuleForPath filePath changeBuilder

        for assemblyPath in references.Assemblies do
            psiModule.AddReference(assemblyPath)

        psiModule

    and createPsiModuleForPath (path: FileSystemPath) changeBuilder =
        let modulesForPath = getPsiModulesForPath path
        if modulesForPath.IsEmpty() then
            let fileDocument = documentManager.GetOrCreateDocument(path)
            let moduleId = path.FullPath
            let sourceFileCtor = createSourceFileForPath path
            let psiModule = createPsiModule path fileDocument moduleId sourceFileCtor changeBuilder

            scriptsFromPaths.[path] <- psiModule
            addPsiModule psiModule

    let rec updateReferences (path: FileSystemPath) (document: IDocument) =
        locks.QueueReadLock(lifetime, "Request new F# script references", fun _ ->
            let mutable oldReferences = Unchecked.defaultof<ScriptReferences>
            match scriptsReferences.TryGetValue(path, &oldReferences) with
            | false -> ()
            | _ ->

            let ira =
                InterruptableReadActivityThe
                    (lifetime, locks, fun _ -> lifetime.IsTerminated || locks.ContentModelLocks.IsWriteLockRequested)

            ira.FuncRun <-
                fun _ ->
                    let newOptions = getScriptOptions path document
                    let newReferences = getScriptReferences path newOptions
                    InterruptableActivityCookie.CheckAndThrow()

                    let getDiff oldPaths newPaths =
                        let notChanged = Enumerable.Intersect(newPaths, oldPaths) |> HashSet
                        let filterChanges = Seq.filter (notChanged.Contains >> not) >> List.ofSeq
                        filterChanges newPaths, filterChanges oldPaths

                    match getDiff oldReferences.Assemblies newReferences.Assemblies with
                    | added, removed when
                            not added.IsEmpty || not removed.IsEmpty ||
                            not (newReferences.Files.SetEquals(oldReferences.Files)) ->

                        InterruptableActivityCookie.CheckAndThrow()
                        locks.ExecuteOrQueue(lifetime, "Update F# script references", fun _ ->
                            if not (scriptsReferences.ContainsKey(path)) then () else

                            use cookie = WriteLockCookie.Create()
                            let changeBuilder = PsiModuleChangeBuilder()
                            scriptsReferences.[path] <- newReferences

                            for filePath in newReferences.Files do
                                createPsiModuleForPath filePath changeBuilder

                            for psiModule in getPsiModulesForPath path do
                                for path in added do psiModule.AddReference(path)
                                for path in removed do psiModule.RemoveReference(path)
                                changeBuilder.AddModuleChange(psiModule, PsiModuleChange.ChangeType.Invalidated)

                            changeManager.OnProviderChanged(this, changeBuilder.Result, SimpleTaskExecutor.Instance)

                            if removed.IsEmpty then () else
                            locks.QueueReadLock(lifetime, "AssemblyGC after removing F# script reference", fun _ ->
                                solution.GetComponent<AssemblyGC>().ForceGC()))
                    | _ -> ()

            ira.FuncCancelled <-
                // Reschedule again
                fun _ -> updateReferences path document

            ira.DoStart())

    member x.CreatePsiModuleForProjectFile(projectFile: IProjectFile, changeBuilder: PsiModuleChangeBuilder) =
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
        scriptsFromProjectFiles.GetValuesSafe(path)
        |> Seq.tryFind (fun psiModule -> psiModule.PersistenID = moduleId)
        |> Option.defaultWith (fun _ ->
            let document = projectFile.GetDocument()
            let sourceFileCtor = createSourceFileForProjectFile projectFile
            let psiModule = createPsiModule path document moduleId sourceFileCtor changeBuilder
            scriptsFromProjectFiles.Add(path, psiModule)
            addPsiModule psiModule
            psiModule)

    member x.CreatePsiModuleForPath(path: FileSystemPath, changeBuilder: PsiModuleChangeBuilder) =
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

    member x.RemoveProjectFilePsiModule(moduleToRemove: FSharpScriptPsiModule) = //, changeBuilder: PsiModuleChangeBuilder) =
        let path = moduleToRemove.Path
        scriptsFromProjectFiles.GetValuesSafe(path)
        |> Seq.tryFind (fun psiModule -> psiModule.Path = moduleToRemove.Path)
        |> Option.iter (fun psiModule ->
            scriptsFromProjectFiles.RemoveValue(path, psiModule) |> ignore
            removePsiModule psiModule

            psiModule.LifetimeDefinition.Terminate()
            let changeBuilder = PsiModuleChangeBuilder() 
            changeBuilder.AddModuleChange(psiModule, PsiModuleChange.ChangeType.Removed)
            changeBuilder.AddFileChange(psiModule.SourceFile, PsiModuleChange.ChangeType.Removed)
            changeManager.OnProviderChanged(this, changeBuilder.Result, SimpleTaskExecutor.Instance))

    member x.GetPsiModulesForPath(path) =
        getPsiModulesForPath path 

    member x.TargetFrameworkId =
        targetFrameworkId

    member x.Execute(change: ChangeEventArgs) =
        match change.ChangeMap.GetChange<ProjectFileDocumentCopyChange>(documentManager.ChangeProvider) with
        | null -> ()
        | change ->
            let path = change.ProjectFile.Location
            if scriptsReferences.ContainsKey(path) then 
                updateReferences path change.Document

    interface IProjectPsiModuleProviderFilter with
        member x.OverrideHandler(lifetime, project, handler) =
            let handler =
                FSharpScriptPsiModuleHandler(lifetime, solution, handler, this, projectFileExtensions, changeManager)
            handler :> _, null

    interface IPsiModuleFactory with
        member x.Modules = psiModulesCollection

    interface IChangeProvider with
        member x.Execute(changeMap) = null


/// Overriding psi module handler for each project (a real project, misc files project, solution folder, etc). 
type FSharpScriptPsiModuleHandler
        (lifetime, solution, handler, modulesProvider, projectFileExtensions, changeManager) as this =
    inherit DelegatingProjectPsiModuleHandler(handler)

    let locks = solution.Locks
    let psiModules = solution.PsiModules()
    let sourceFiles = Dictionary<FileSystemPath, IPsiSourceFile>()

    do
        lifetime.AddAction(fun _ ->
            changeManager.ExecuteAfterChange(fun _ ->
                let changeBuilder = new PsiModuleChangeBuilder()
                for sourceFile in sourceFiles.Values do
                    let psiModule = sourceFile.PsiModule :?> FSharpScriptPsiModule
                    psiModule.RemoveProjectHandler(this))) |> ignore

    /// Prevents creating default psi source files for scripts and adds new psi modules with source files instead.
    override x.OnProjectFileChanged(projectFile, oldLocation, changeType, changeBuilder) =
        locks.AssertWriteAccessAllowed()
        match changeType with
        | PsiModuleChange.ChangeType.Added when
                projectFile.LanguageType.Is<FSharpScriptProjectFileType>() ->
            let psiModule = modulesProvider.CreatePsiModuleForProjectFile(projectFile, changeBuilder)
            psiModule.AddProjectHandler(this)
            sourceFiles.[projectFile.Location] <- psiModule.SourceFile

        | PsiModuleChange.ChangeType.Removed when sourceFiles.ContainsKey(oldLocation) ->
            let sourceFile = sourceFiles.[oldLocation]
            let psiModule = sourceFile.PsiModule :?> FSharpScriptPsiModule
            psiModule.RemoveProjectHandler(this)
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


type FSharpScriptPsiModule
        (lifetime, path, solution, sourceFileCtor, moduleId, assemblyFactory, modulesProvider) as this =
    inherit ConcurrentUserDataHolder()

    let lifetimeDefinition = Lifetimes.Define(lifetime)
    let lifetime = lifetimeDefinition.Lifetime 

    let locks = solution.Locks
    let psiServices = solution.GetPsiServices()
    let psiModules = solution.PsiModules()

    let containingModule = FSharpScriptModule(path, solution)
    let targetFrameworkId = modulesProvider.TargetFrameworkId
    let sourceFile = lazy (sourceFileCtor this)
    let resolveContext = lazy (PsiModuleResolveContext(this, targetFrameworkId, null))

    // There is a separate project handler for each target framework.
    // Each overriden handler provides a psi module and a source file for each script project file.
    // We create at most one psi module for each project file and update list of handlers pointing to it.
    let projectHandlers = List<IProjectPsiModuleHandler>()

    let assemblyCookies = DictionaryEvents<FileSystemPath, IAssemblyCookie>(lifetime, moduleId)

    do
        assemblyCookies.AddRemove.Advise_Remove(lifetime, fun (AddRemoveArgs (KeyValuePair (_, assemblyCookie))) ->
            locks.AssertWriteAccessAllowed()
            assemblyCookie.Dispose())

        lifetime.AddAction(fun _ ->
            use lock = WriteLockCookie.Create()
            assemblyCookies.Clear()) |> ignore

    member x.Path = path
    member x.SourceFile: IPsiSourceFile = sourceFile.Value
    member x.ResolveContext = resolveContext.Value
    member x.PersistenID = moduleId
    member x.LifetimeDefinition: LifetimeDefinition = lifetimeDefinition

    member x.IsValid = psiServices.Modules.HasModule(this)

    member x.AddReference(path: FileSystemPath) =
        locks.AssertWriteAccessAllowed()
        if not (assemblyCookies.ContainsKey(path)) then
            assemblyCookies.Add(path, assemblyFactory.AddRef(path, moduleId, this.ResolveContext))

    member x.RemoveReference(path: FileSystemPath) =
        locks.AssertWriteAccessAllowed()
        if assemblyCookies.ContainsKey(path) then
            assemblyCookies.Remove(path) |> ignore

    member x.AddProjectHandler(handler: FSharpScriptPsiModuleHandler) =
        projectHandlers.Add(handler)

    member x.RemoveProjectHandler(handler: FSharpScriptPsiModuleHandler) =
        projectHandlers.Remove(handler) |> ignore
        if projectHandlers.IsEmpty() then
            modulesProvider.RemoveProjectFilePsiModule(this)

    interface IPsiModule with
        member x.Name = "F# script: " + path.Name
        member x.DisplayName = "F# script: " + path.Name
        member x.GetPersistentID() = moduleId

        member x.GetSolution() = solution
        member x.GetPsiServices() = psiServices

        member x.SourceFiles = [x.SourceFile] :> _
        member x.TargetFrameworkId = targetFrameworkId
        member x.ContainingProjectModule = containingModule :> _

        member x.GetReferences(resolveContext) =
            locks.AssertReadAccessAllowed()
            let result = LocalList<IPsiModuleReference>()
            for assemblyCookie in assemblyCookies.Values do
                match psiModules.GetPrimaryPsiModule(assemblyCookie.Assembly, targetFrameworkId) with
                | null -> ()
                | psiModule -> result.Add(PsiModuleReference(psiModule))

            for psiModule in modulesProvider.GetReferencedScriptPsiModules(this) do
                 result.Add(PsiModuleReference(psiModule))

            result.ResultingList() :> _

        member x.PsiLanguage = FSharpLanguage.Instance :> _
        member x.ProjectFileType = UnknownProjectFileType.Instance :> _

        member x.GetAllDefines() = EmptyList.InstanceList :> _
        member x.IsValid() = x.IsValid


/// Holder for psi module resolve context.
type FSharpScriptModule(path: FileSystemPath, solution: ISolution) =
    inherit UserDataHolder()

    let path = path.MakeRelativeTo(solution.SolutionFilePath.Directory)

    interface IModule with
        member x.Presentation = path.Name
        member x.Name = path.FullPath

        member x.IsValid() = solution.IsValid()
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

    static member Instance = ScriptFileProperties() :> _


type ScriptReferences =
    { Assemblies: ISet<FileSystemPath>
      Files: ISet<FileSystemPath> }
