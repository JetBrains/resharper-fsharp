module rec JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts

open System
open System.Collections.Generic
open System.Linq
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
open JetBrains.ProjectModel.Model2.Assemblies.Interfaces
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
open Microsoft.FSharp.Compiler.SourceCodeServices

/// Provides psi modules for script files with referenced assemblies determined by "#r" directives.
[<SolutionComponent>]
type FSharpScriptPsiModulesProvider
        (lifetime: Lifetime, solution: ISolution, changeManager: ChangeManager, documentManager: DocumentManager,
         checkerService: FSharpCheckerService, platformManager: PlatformManager, assemblyFactory: AssemblyFactory,
         projectFileExtensions, projectFileTypeCoordinator, logger: ILogger) as this =

    /// There may be multiple project files for a path (i.e. linked in multiple projects) and we must distinguish them.   
    let scriptsFromProjectFiles = OneToListMap<FileSystemPath, LifetimeDefinition * FSharpScriptPsiModule>()

    /// Psi modules for files that came from #load directives and do not present in the project model.
    let scriptsFromPaths = Dictionary<FileSystemPath, LifetimeDefinition * FSharpScriptPsiModule>()

    /// References to assemblies and other source files for each known script.
    let referencedPaths = Dictionary<FileSystemPath, ScriptReferences>()

    do
        changeManager.RegisterChangeProvider(lifetime, this)
        changeManager.AddDependency(lifetime, this, documentManager.ChangeProvider)

        lifetime.AddAction(fun _ ->
            for lifetimeDefinition, _ in Seq.append scriptsFromPaths.Values scriptsFromProjectFiles.Values do
                lifetimeDefinition.Terminate()) |> ignore

    let locks = solution.Locks
    let checker = checkerService.Checker
    let targetFrameworkId =
        let platformInfos = platformManager.GetAllPlatformInfos().AsList()
        if platformInfos.IsEmpty() then TargetFrameworkId.Default else

        let platformInfo = platformInfos |> Seq.maxBy (fun info -> info.Version)
        platformInfo.PlatformID.ToTargetFrameworkId()

    // todo: use script options provider
    let getScriptOptions (path: FileSystemPath) (document: IDocument) =
        let source = document.GetText()
        let options, diagnostics = checker.GetProjectOptionsFromScript(path.FullPath, source).RunAsTask()
        if not diagnostics.IsEmpty then
            logger.Warn("Getting script options for {0}: {1}", path, concatErrors diagnostics)
        options

    let getScriptReferences scriptPath scriptOptions =
        let assembliesPaths =
            scriptOptions.OtherOptions
            |> Seq.choose (fun o ->
                if o.StartsWith("-r:", StringComparison.Ordinal) then
                    let path = FileSystemPath.TryParse(o.Substring(3))
                    if path.IsEmpty then None else Some path
                else None)

        let filesPaths =
            scriptOptions.SourceFiles
            |> Seq.map FileSystemPath.TryParse
            |> Seq.filter (fun path -> not path.IsEmpty && path <> scriptPath)

        { Assemblies = HashSet(assembliesPaths)
          Files = HashSet(filesPaths) }

    let getPsiModulesForPath path =
        scriptsFromProjectFiles.GetValuesSafe(path)
        |> Seq.append (tryGetValue path scriptsFromPaths |> Option.toList)
        |> Seq.map snd

    let createSourceFileForProjectFile projectFile (psiModule: FSharpScriptPsiModule) =
        PsiProjectFile
            (psiModule, projectFile, (fun _ _ -> ScriptFileProperties.Instance),
             (fun pf _ -> pf.IsValid()), documentManager, psiModule.ResolveContext) :> IPsiSourceFile

    let createSourceFileForPath path (psiModule: FSharpScriptPsiModule) =
        NavigateablePsiSourceFileWithLocation
            (projectFileExtensions, projectFileTypeCoordinator, psiModule, path, (fun _ -> psiModule.IsValid),
             (fun _ -> ScriptFileProperties.Instance), documentManager, psiModule.ResolveContext) :> IPsiSourceFile

    let rec createPsiModule path document id sourceFileCtor (changeBuilder: PsiModuleChangeBuilder) =
        let lifetimeDefinition = Lifetimes.Define(lifetime)
        let lifetime = lifetimeDefinition.Lifetime
        let psiModule = FSharpScriptPsiModule(lifetime, path, solution, sourceFileCtor, id, assemblyFactory, this)

        changeBuilder.AddModuleChange(psiModule, PsiModuleChange.ChangeType.Added)
        changeBuilder.AddFileChange(psiModule.SourceFile, PsiModuleChange.ChangeType.Added)

        let scriptOptions = getScriptOptions path document
        let references = getScriptReferences path scriptOptions
        referencedPaths.[path] <- references

        for filePath in references.Files do
            createPsiModuleForPath filePath changeBuilder

        for assemblyPath in references.Assemblies do
            psiModule.AddReference(assemblyPath)

        lifetimeDefinition, psiModule

    and createPsiModuleForPath (path: FileSystemPath) changeBuilder =
        match getPsiModulesForPath path |> Seq.tryHead with
        | None ->
            let fileDocument = documentManager.GetOrCreateDocument(path)
            let moduleId = path.FullPath
            let sourceFileCtor = createSourceFileForPath path
            let lifetimeDefinition, psiModule = createPsiModule path fileDocument moduleId sourceFileCtor changeBuilder

            scriptsFromPaths.[path] <- (lifetimeDefinition, psiModule)
        | _ -> ()

    member x.CreatePsiModuleForProjectFile(projectFile: IProjectFile, changeBuilder: PsiModuleChangeBuilder) =
        locks.AssertWriteAccessAllowed()

        let path = projectFile.Location

        // if previously created a psi module for the path then replace it with a psi module for the project file
        tryGetValue path scriptsFromPaths
        |> Option.iter (fun (lifetimeDefinition, psiModule) ->
            scriptsFromPaths.Remove(path) |> ignore
            lifetimeDefinition.Terminate()
            changeBuilder.AddModuleChange(psiModule, PsiModuleChange.ChangeType.Removed)
            changeBuilder.AddFileChange(psiModule.SourceFile, PsiModuleChange.ChangeType.Removed))

        let document = projectFile.GetDocument()
        let sourceFileCtor = createSourceFileForProjectFile projectFile
        let moduleId = projectFile.GetPersistentID()

        let lifetimeDefinition, psiModule = createPsiModule path document moduleId sourceFileCtor changeBuilder
        scriptsFromProjectFiles.Add(path, (lifetimeDefinition, psiModule))

        psiModule

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

        tryGetValue psiModule.Path referencedPaths
        |> Option.map (fun paths -> paths.Files |> Seq.map (getPsiModulesForPath >> Seq.maxBy sameProjectSorter))
        |> Option.get

    member x.RemoveProjectFilePsiModule(moduleToRemove: FSharpScriptPsiModule, changeBuilder: PsiModuleChangeBuilder) =
        let path = moduleToRemove.Path
        scriptsFromProjectFiles.GetValuesSafe(path)
        |> Seq.tryFind (fun (_, psiModule) -> psiModule = moduleToRemove)
        |> Option.orElseWith (fun _ -> sprintf "psiModule not found: %O" path |> failwith)
        |> Option.iter (fun ((lifetimeDefinition, psiModule) as value) ->
            scriptsFromProjectFiles.RemoveValue(path, value) |> ignore
            lifetimeDefinition.Terminate()
            changeBuilder.AddModuleChange(psiModule, PsiModuleChange.ChangeType.Removed)
            changeBuilder.AddFileChange(psiModule.SourceFile, PsiModuleChange.ChangeType.Removed))

    member x.GetPsiModulesForPath(path) =
        getPsiModulesForPath path 

    member x.TargetFrameworkId =
        targetFrameworkId

    interface IProjectPsiModuleProviderFilter with
        member x.OverrideHandler(lifetime, project, handler) =
            let handler =
                FSharpScriptPsiModuleHandler(lifetime, solution, handler, this, projectFileExtensions, changeManager)
            handler :> _, null

    interface IPsiModuleFactory with
        member x.Modules =
            scriptsFromPaths.Values
            |> Seq.append scriptsFromProjectFiles.Values
            |> Seq.map (fun (_, psiModule) -> psiModule :> IPsiModule)
            |> HybridCollection<IPsiModule>

    interface IChangeProvider with
        member x.Execute(changeMap) =
            let change = changeMap.GetChange<ProjectFileDocumentCopyChange>(documentManager.ChangeProvider)
            if isNull change then null else

            let path = change.ProjectFile.Location
            match tryGetValue path referencedPaths with
            | Some oldReferences ->
                let newOptions = getScriptOptions path change.Document
                let newReferences = getScriptReferences path newOptions

                let getDiff oldPaths newPaths =
                    let notChanged = Enumerable.Intersect(newPaths, oldPaths) |> HashSet
                    let filterChanges = Seq.filter (notChanged.Contains >> not) >> List.ofSeq
                    filterChanges newPaths, filterChanges oldPaths

                match getDiff oldReferences.Assemblies newReferences.Assemblies with
                | [], [] when newReferences.Files.SetEquals(oldReferences.Files) -> null
                | added, removed ->
                    let changeBuilder = PsiModuleChangeBuilder()

                    referencedPaths.[path] <- newReferences
                    for filePath in newReferences.Files do
                        createPsiModuleForPath filePath changeBuilder

                    for psiModule in getPsiModulesForPath path do
                        for path in added do psiModule.AddReference(path)
                        for path in removed do psiModule.RemoveReference(path)
                        changeBuilder.AddModuleChange(psiModule, PsiModuleChange.ChangeType.Invalidated)
                    changeBuilder.Result :> _
            | _ -> null


/// Overriding psi module handler for each project (a real project, misc files project, solution folder, etc). 
type FSharpScriptPsiModuleHandler
        (lifetime, solution, handler, modulesProvider, projectFileExtensions, changeManager) as this =
    inherit DelegatingProjectPsiModuleHandler(handler)

    let locks = solution.Locks
    let psiModules = solution.PsiModules()
    let sourceFiles = Dictionary<FileSystemPath, IPsiSourceFile>()

    let removeSourceFile changeBuilder (sourceFile: IPsiSourceFile) =
        let psiModule = sourceFile.PsiModule :?> FSharpScriptPsiModule
        sourceFiles.Remove(sourceFile.GetLocation()) |> ignore
        modulesProvider.RemoveProjectFilePsiModule(psiModule, changeBuilder)

    do
        changeManager.RegisterChangeProvider(lifetime, this)
        changeManager.AddDependency(lifetime, psiModules, this)
        lifetime.AddAction(fun _ ->
            changeManager.ExecuteAfterChange(fun _ ->
                let changeBuilder = new PsiModuleChangeBuilder()
                sourceFiles.Values |> List.ofSeq |> List.iter (removeSourceFile changeBuilder)
                changeManager.OnProviderChanged(this, changeBuilder.Result, SimpleTaskExecutor.Instance))) |> ignore

    /// Prevents creating default psi source files for scripts and adds new psi modules with source files instead.
    override x.OnProjectFileChanged(projectFile, oldLocation, changeType, changeBuilder) =
        locks.AssertWriteAccessAllowed()
        match changeType with
        | PsiModuleChange.ChangeType.Added when
                projectFile.LanguageType.Is<FSharpScriptProjectFileType>() ->

            let psiModule = modulesProvider.CreatePsiModuleForProjectFile(projectFile, changeBuilder)
            sourceFiles.[projectFile.Location] <- psiModule.SourceFile

        | PsiModuleChange.ChangeType.Removed when sourceFiles.ContainsKey(oldLocation) ->

            modulesProvider.CreatePsiModuleForPath(oldLocation, changeBuilder)
            removeSourceFile changeBuilder sourceFiles.[oldLocation]

        | _ -> handler.OnProjectFileChanged(projectFile, oldLocation, changeType, changeBuilder)

    override x.GetPsiSourceFilesFor(projectFile) =
        handler.GetPsiSourceFilesFor(projectFile)
        |> Seq.append (tryGetValue projectFile.Location sourceFiles |> Option.toList)

    override x.InternalsVisibleTo(moduleTo, moduleFrom) =
        moduleTo :? FSharpScriptPsiModule && moduleFrom :? FSharpScriptPsiModule ||
        handler.InternalsVisibleTo(moduleTo, moduleFrom)

    interface IChangeProvider with
        member x.Execute(_) = null

type FSharpScriptPsiModule
        (lifetime, path, solution, sourceFileCtor, moduleId, assemblyFactory, modulesProvider) as this =
    inherit ConcurrentUserDataHolder()

    let locks = solution.Locks
    let psiServices = solution.GetPsiServices()
    let psiModules = solution.PsiModules()

    let containingModule = FSharpScriptModule(path, solution)
    let targetFrameworkId = modulesProvider.TargetFrameworkId
    let sourceFile = lazy (sourceFileCtor this)
    let resolveContext = lazy (PsiModuleResolveContext(this, targetFrameworkId, null))

    let assemblyCookies = DictionaryEvents<FileSystemPath, IAssemblyCookie>(lifetime, moduleId)
    let containsAssemblyCookieMsg = sprintf "%s contains cookie for %O" moduleId

    do
        assemblyCookies.AddRemove.Advise_Remove(lifetime, fun (AddRemoveArgs (KeyValuePair (_, assemblyCookie))) ->
            assemblyCookie.Dispose())
        lifetime.AddAction(fun _ ->
            use lock = WriteLockCookie.Create()
            assemblyCookies.Clear()) |> ignore

    member x.Path = path
    member x.SourceFile: IPsiSourceFile = sourceFile.Value
    member x.ResolveContext = resolveContext.Value

    member x.IsValid = psiServices.Modules.HasModule(this)

    member x.AddReference(path: FileSystemPath) =
        locks.AssertWriteAccessAllowed()
        Assertion.Assert(assemblyCookies.ContainsKey(path) |> not, containsAssemblyCookieMsg path)
        assemblyCookies.Add(path, assemblyFactory.AddRef(path, moduleId, this.ResolveContext))

    member x.RemoveReference(path: FileSystemPath) =
        locks.AssertWriteAccessAllowed()
        Assertion.Assert(assemblyCookies.ContainsKey(path), containsAssemblyCookieMsg path)
        assemblyCookies.Remove(path) |> ignore

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
            assemblyCookies.Values
            |> Seq.choose (fun assemblyCookie ->
                psiModules.GetPrimaryPsiModule(assemblyCookie.Assembly, targetFrameworkId)
                |> Option.ofObj
                |> Option.map (fun psiModule -> PsiModuleReference(psiModule) :> IPsiModuleReference))
            |> Seq.append (modulesProvider.GetReferencedScriptPsiModules(this)
                |> Seq.map (fun psiModule -> PsiModuleReference(psiModule) :> _))

        member x.PsiLanguage = FSharpLanguage.Instance :> _
        member x.ProjectFileType = UnknownProjectFileType.Instance :> _

        member x.GetAllDefines() = EmptyList.InstanceList :> _
        member x.IsValid() = x.IsValid


type ScriptReferences =
    { Assemblies: ISet<FileSystemPath>
      Files: ISet<FileSystemPath> }


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
