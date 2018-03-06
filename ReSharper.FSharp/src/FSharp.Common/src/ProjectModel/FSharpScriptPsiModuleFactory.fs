module rec JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Linq
open JetBrains.Application.Progress
open JetBrains.Application.Threading
open JetBrains.Application.changes
open JetBrains.Application.platforms
open JetBrains.DataFlow
open JetBrains.DocumentManagers
open JetBrains.DocumentManagers.impl
open JetBrains.DocumentManagers.Transactions
open JetBrains.Metadata.Reader.API
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Assemblies.Impl
open JetBrains.ProjectModel.Model2.Assemblies.Interfaces
open JetBrains.ReSharper.Host.Features.ProjectModel.MiscFiles
open JetBrains.ReSharper.Plugins.FSharp.Common.Checker
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Impl
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Rider.Model
open JetBrains.Threading
open JetBrains.Util
open JetBrains.Util.DataStructures
open Microsoft.FSharp.Compiler.SourceCodeServices

/// Provides separate psi modules for script files with referenced assemblies set determined by "#r" directives.
[<SolutionComponent>]
type FSharpScriptPsiModulesProvider(lifetime: Lifetime, solution: ISolution, changeManager: ChangeManager,
                                    documentManager: DocumentManager, checkerService: FSharpCheckerService,
                                    projectFileTypeCoordinator: PsiProjectFileTypeCoordinator,
                                    platformManager: PlatformManager, assemblyFactory: AssemblyFactory,
                                    projectFileExtensions: ProjectFileExtensions) as this =

    /// There may be multiple project files for a path (i.e. linked in multiple projects) and we must distinguish them.   
    let scriptsFromProjectFiles = OneToListMap<FileSystemPath, LifetimeDefinition * FSharpScriptPsiModule>()

    /// Psi modules for files that came from #load directives and do not present in the project model.
    let scriptsFromPaths = Dictionary<FileSystemPath, LifetimeDefinition * FSharpScriptPsiModule>()

    let referencedPaths = OneToSetMap<FileSystemPath, FileSystemPath>()
    let loadPaths = OneToSetMap<FileSystemPath, FileSystemPath>()

    do
        changeManager.RegisterChangeProvider(lifetime, this)
        changeManager.AddDependency(lifetime, this, documentManager.ChangeProvider)

        lifetime.AddAction(fun _ ->
            for lifetimeDefinition, _ in scriptsFromProjectFiles.Values do
                lifetimeDefinition.Terminate()
            for lifetimeDefinition, _ in scriptsFromPaths.Values do
                lifetimeDefinition.Terminate()) |> ignore

    let locks = solution.Locks
    let checker = checkerService.Checker

    let targetFrameworkId =
        let plaformInfo = platformManager.GetAllPlatformInfos() |> Seq.maxBy (fun info -> info.Version)
        plaformInfo.PlatformID.ToTargetFrameworkId()

    let getScriptOptions (projectFile: IProjectFile) =
        let path = projectFile.Location.FullPath
        let source = projectFile.GetDocument().GetText()
        checker.GetProjectOptionsFromScript(path, source).RunAsTask() |> fst

    let getReferencedPaths (options: FSharpProjectOptions) =
        options.OtherOptions
        |> Seq.choose (fun o ->
            if o.StartsWith("-r:", StringComparison.Ordinal) then
                let path = FileSystemPath.TryParse(o.Substring(3))
                if path.IsEmpty then None else Some path
            else None)
        |> HashSet :> seq<_>

    let getPsiModulesForPath path =
        scriptsFromProjectFiles.GetValuesSafe(path)
        |> Seq.append (tryGetValue scriptsFromPaths path |> Option.toList)
        |> Seq.map snd

    let tryGetReferencedPaths path =
        let getReferences (_, psiModule: IPsiModule) =
            (psiModule :?> FSharpScriptPsiModule).ReferencedAssemblyPaths 

        tryGetValue scriptsFromPaths path
        |> Option.map getReferences
        |> Option.orElseWith (fun _ ->
            scriptsFromProjectFiles.GetValuesSafe(path)
            |> Seq.tryHead
            |> Option.map getReferences)

    let createSourceFileForProjectFile projectFile (psiModule: FSharpScriptPsiModule) =
        PsiProjectFile
            (psiModule, projectFile, (fun _ _ -> ScriptFileProperties.Instance),
             (fun _ _ -> projectFile.IsValid()), documentManager, psiModule.ResolveContext) :> IPsiSourceFile

    let createSourceFileForPath path (psiModule: FSharpScriptPsiModule) =
        NavigateablePsiSourceFileWithLocation
            (projectFileExtensions, projectFileTypeCoordinator, psiModule, path, (fun _ -> psiModule.IsValid),
             (fun _ -> ScriptFileProperties.Instance), documentManager, psiModule.ResolveContext) :> IPsiSourceFile

    let createPsiModule path sourceFileCtor filePaths (changeBuilder: PsiModuleChangeBuilder) =
        let lifetimeDefinition = Lifetimes.Define(lifetime)
        let lifetime = lifetimeDefinition.Lifetime
        let psiModule =
            FSharpScriptPsiModule(lifetime, sourceFileCtor, path, solution, targetFrameworkId, assemblyFactory, filePaths, this)

        changeBuilder.AddModuleChange(psiModule, PsiModuleChange.ChangeType.Added)
        changeBuilder.AddFileChange(psiModule.SourceFile, PsiModuleChange.ChangeType.Added)
        lifetimeDefinition, psiModule

    member x.GetPsiModulesForPath(path) = getPsiModulesForPath path

    member x.CreatePsiModuleForFile(projectFile: IProjectFile, changeBuilder: PsiModuleChangeBuilder) =
        locks.AssertWriteAccessAllowed()

        let path = projectFile.Location
        tryGetValue scriptsFromPaths path
        |> Option.iter (fun (lifetimeDefinition, psiModule) ->
            scriptsFromPaths.Remove(path) |> ignore
            lifetimeDefinition.Terminate()
            changeBuilder.AddModuleChange(psiModule, PsiModuleChange.ChangeType.Removed)
            changeBuilder.AddFileChange(psiModule.SourceFile, PsiModuleChange.ChangeType.Removed))

        let scriptOptions = getScriptOptions projectFile
        let filePaths =
            scriptOptions.SourceFiles
            |> Seq.map FileSystemPath.TryParse
            |> Seq.filter (fun path -> not path.IsEmpty) 

        let sourceFileCtor = createSourceFileForProjectFile projectFile
        let lifetimeDefinition, psiModule = createPsiModule path sourceFileCtor filePaths changeBuilder
        scriptsFromProjectFiles.Add(path, (lifetimeDefinition, psiModule))

        for filePath in filePaths do
            if filePath <> path then
                match getPsiModulesForPath filePath |> Seq.tryHead with
                | None ->
                    let sourceFileCtor = createSourceFileForPath filePath
                    let lifetimeDefinition, psiModule = createPsiModule filePath sourceFileCtor [] changeBuilder
                    scriptsFromPaths.[filePath] <- (lifetimeDefinition, psiModule)
                | _ -> ()

        for path in scriptOptions |> getReferencedPaths do
            psiModule.AddReference(path)

        psiModule

    interface IProjectPsiModuleProviderFilter with
        member x.OverrideHandler(lifetime, _, handler) =
            let handler = FSharpScriptPsiModuleHandler(lifetime, locks, handler, this, projectFileExtensions)
            handler :> _, null

    interface IPsiModuleFactory with
        member x.Modules =
            scriptsFromPaths.Values
            |> Seq.append scriptsFromProjectFiles.Values
            |> Seq.map snd
            |> Seq.cast<IPsiModule>
            |> HybridCollection<IPsiModule>

    interface IChangeProvider with
        member x.Execute(changeMap) =
            let change = changeMap.GetChange<ProjectFileDocumentCopyChange>(documentManager.ChangeProvider)
            if isNull change then null else

            let projectFile = change.ProjectFile
            let path = projectFile.Location

            match tryGetReferencedPaths path with
            | Some oldPaths ->
                let newPaths = getScriptOptions projectFile |> getReferencedPaths
                let added, removed =
                    let notChanged = Enumerable.Intersect(newPaths, oldPaths) |> HashSet
                    let filterChanges = Seq.filter (notChanged.Contains >> not) >> List.ofSeq
                    filterChanges newPaths, filterChanges oldPaths
                if List.isEmpty added && List.isEmpty removed then null else
    
                let changeBuilder = PsiModuleChangeBuilder()
                for psiModule in getPsiModulesForPath path |> Seq.cast<FSharpScriptPsiModule> do
                    for path in added do psiModule.AddReference(path)
                    for path in removed do psiModule.RemoveReference(path)
                    changeBuilder.AddModuleChange(psiModule, PsiModuleChange.ChangeType.Invalidated)
                changeBuilder.Result :> _
            | _ -> null


/// Overriding psi module handler for each project (a real project, misc files project, solution folder, etc). 
type FSharpScriptPsiModuleHandler(lifetime, locks, handler, modulesProvider: FSharpScriptPsiModulesProvider,
                                  projectFileExtensions: ProjectFileExtensions) =
    inherit DelegatingProjectPsiModuleHandler(handler)

    let sourceFiles = Dictionary<FileSystemPath, IPsiSourceFile>()

    /// Prevents creating default psi source files for scripts and adds new psi modules with source files instead.
    override x.OnProjectFileChanged(projectFile, oldLocation, changeType, changeBuilder) =
        locks.AssertWriteAccessAllowed()
        match changeType with
        | PsiModuleChange.ChangeType.Added when
                projectFile.LanguageType.Is<FSharpScriptProjectFileType>() ->

            let psiModule = modulesProvider.CreatePsiModuleForFile(projectFile, changeBuilder)
            sourceFiles.[projectFile.Location] <- psiModule.SourceFile

        | PsiModuleChange.ChangeType.Removed when
                projectFileExtensions.GetFileType(oldLocation).Is<FSharpScriptProjectFileType>() ->

            () // todo: remove project file from list for path

        | _ -> handler.OnProjectFileChanged(projectFile, oldLocation, changeType, changeBuilder)

    override x.GetPsiSourceFilesFor(projectFile) =
        handler.GetPsiSourceFilesFor(projectFile)
        |> Seq.append (tryGetValue sourceFiles projectFile.Location |> Option.toList)


type FSharpScriptPsiModule(lifetime, sourceFileCtor, path, solution: ISolution, targetFrameworkId, assemblyFactory, filePaths, moduleProvider) as this =
    inherit ConcurrentUserDataHolder()

    let locker = JetFastSemiReenterableRWLock()
    let psiServices = solution.GetPsiServices()
    let psiModules = solution.PsiModules()

    let scriptModule = FSharpScriptModule(path, solution)
    let assemblyCookies = Dictionary<FileSystemPath, IAssemblyCookie>()

    let resolveContext = lazy (PsiModuleResolveContext(this, targetFrameworkId, null))
    let sourceFile = lazy (sourceFileCtor this)

    member x.SourceFile = sourceFile.Value
    member x.ResolveContext = resolveContext.Value

    member x.IsValid = psiServices.Modules.HasModule(this)

    member x.ReferencedFilePaths = filePaths
    member x.ReferencedAssemblyPaths =
        use lock = locker.UsingReadLock()
        assemblyCookies.Keys

    member x.AddReference(path: FileSystemPath) =
        use lock = locker.UsingWriteLock()
        if assemblyCookies.ContainsKey(path) then () else

        let assemblyCookie = assemblyFactory.AddRef(path, path.FullPath, this.ResolveContext)
        assemblyCookies.Add(path, assemblyCookie)
        lifetime.AddAction(fun _ ->
            use lock = WriteLockCookie.Create()
            assemblyCookie.Dispose()) |> ignore

    member x.RemoveReference(path: FileSystemPath) =
        use lock = locker.UsingWriteLock()
        assemblyCookies.Remove(path) |> ignore

    interface IPsiModule with
        member x.Name = "F# script: " + path.Name
        member x.DisplayName = "F# script: " + path.Name
        member x.GetPersistentID() = "FSharpScriptModule:" + path.FullPath

        member x.GetSolution() = solution
        member x.GetPsiServices() = psiServices

        member x.TargetFrameworkId = targetFrameworkId
        member x.ContainingProjectModule = scriptModule :> _

        member x.PsiLanguage = FSharpLanguage.Instance :> _
        member x.ProjectFileType = UnknownProjectFileType.Instance :> _

        member x.SourceFiles = [x.SourceFile] :> _ 

        member x.GetReferences(resolveContext) =
            use lock = locker.UsingReadLock()
            assemblyCookies.Values
            |> Seq.choose (fun assemblyCookie ->
                psiModules.GetPrimaryPsiModule(assemblyCookie.Assembly, targetFrameworkId)
                |> Option.ofObj
                |> Option.map (fun psiModule -> PsiModuleReference(psiModule) :> IPsiModuleReference))
            |> Seq.append (filePaths |> Seq.choose (fun path ->
                moduleProvider.GetPsiModulesForPath(path)
                |> Seq.tryHead
                |> Option.map (fun psiModule -> PsiModuleReference(psiModule) :> _)))

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

        member x.GetPreImportedNamespaces() = Seq.empty
        member x.GetDefaultNamespace() = String.Empty
        member x.GetDefines() = EmptyList.Instance :> _

    static member Instance = ScriptFileProperties() :> IPsiSourceFileProperties
