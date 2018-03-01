namespace rec JetBrains.ReSharper.Plugins.FSharp.ProjectModel

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

/// Provides a separate psi module for each script file with referenced assemblies set determined by "#r" directives.
[<SolutionComponent>]
type FSharpScriptPsiModulesProvider(lifetime: Lifetime, solution: ISolution, changeManager: ChangeManager,
                                    documentManager: DocumentManager, checkerService: FSharpCheckerService,
                                    (*projectFileTypeCoordinator: PsiProjectFileTypeCoordinator,*)
                                    platformManager: PlatformManager, assemblyFactory: AssemblyFactory,
                                    projectFileExtensions: ProjectFileExtensions) as this =

    let [<Literal>] id = "FSharpScriptPsiModuleProvider"
    let scriptPsiModules = DictionaryEvents<FileSystemPath, LifetimeDefinition * FSharpScriptPsiModule>(lifetime, id)

    let locks = solution.Locks
    let checker = checkerService.Checker

    let targetFrameworkId =
        let plaformInfo = platformManager.GetAllPlatformInfos() |> Seq.maxBy (fun info -> info.Version)
        plaformInfo.PlatformID.ToTargetFrameworkId()

    do
        changeManager.RegisterChangeProvider(lifetime, this)
        changeManager.AddDependency(lifetime, this, documentManager.ChangeProvider)

        scriptPsiModules.SuppressItemErrors <- true
        scriptPsiModules.AddRemove.Advise_Remove(lifetime, fun args ->
            let moduleLifetime, _ = args.Value.Value
            moduleLifetime.Terminate()) |> ignore
        lifetime.AddAction(fun _ -> scriptPsiModules.Clear()) |> ignore

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

    member x.GetOrCreatePsiModule(projectFile: IProjectFile, changeBuilder: PsiModuleChangeBuilder) =
        locks.AssertWriteAccessAllowed()

        let path = projectFile.Location
        tryGetValue scriptPsiModules path
        |> Option.map snd
        |> Option.defaultWith (fun _ ->
            let lifetimeDefintion = Lifetimes.Define(lifetime)
            let lifetime = lifetimeDefintion.Lifetime
            let psiModule =
                FSharpScriptPsiModule(lifetime, projectFile, targetFrameworkId, assemblyFactory, documentManager)
            scriptPsiModules.[path] <- (lifetimeDefintion, psiModule)
            changeBuilder.AddModuleChange(psiModule, PsiModuleChange.ChangeType.Added)
            changeBuilder.AddFileChange(psiModule.SourceFile, PsiModuleChange.ChangeType.Added)

            for path in projectFile |> getScriptOptions |> getReferencedPaths do
                psiModule.AddReference(path)

            psiModule)

    interface IProjectPsiModuleProviderFilter with
        member x.OverrideHandler(lifetime, _, handler) =
            let handler = FSharpScriptPsiModuleHandler(lifetime, locks, handler, this, projectFileExtensions)
            handler :> _, null

    interface IPsiModuleFactory with
        member x.Modules =
            let modules = scriptPsiModules.Values |> Seq.map (fun (_, psiModule) -> psiModule :> IPsiModule)
            HybridCollection<IPsiModule>(modules)

    interface IChangeProvider with
        member x.Execute(changeMap) =
            let change = changeMap.GetChange<ProjectFileDocumentCopyChange>(documentManager.ChangeProvider)
            if isNull change then null else

            let projectFile = change.ProjectFile

            tryGetValue scriptPsiModules change.ProjectFile.Location
            |> Option.map (fun (_, psiModule) ->
                let oldPaths = psiModule.ReferencedPaths
                let newPaths = getScriptOptions projectFile |> getReferencedPaths
                let added, removed =
                    let notChanged = Enumerable.Intersect(newPaths, oldPaths) |> HashSet
                    let filterChanges = Seq.filter (notChanged.Contains >> not) >> List.ofSeq
                    filterChanges newPaths, filterChanges oldPaths
                if List.isEmpty added && List.isEmpty removed then null else

                for path in added do psiModule.AddReference(path)
                for path in removed do psiModule.RemoveReference(path)

                let changeBuilder = PsiModuleChangeBuilder()
                changeBuilder.AddModuleChange(psiModule, PsiModuleChange.ChangeType.Invalidated)
                changeBuilder.Result)
            |> Option.defaultValue null :> _


/// Overriding psi module handler for each project (a real project, misc files project, solution folder, etc). 
type FSharpScriptPsiModuleHandler(lifetime, locks, handler, scriptPsiModulesProvider: FSharpScriptPsiModulesProvider,
                                  projectFileExtensions: ProjectFileExtensions) =
    inherit DelegatingProjectPsiModuleHandler(handler)

    let sourceFiles = Dictionary<FileSystemPath, IPsiSourceFile>()

    /// Prevents creating default psi source files for scripts and adds new modules for these files instead.
    override x.OnProjectFileChanged(projectFile, oldLocation, changeType, changeBuilder) =
        locks.AssertWriteAccessAllowed()
        match changeType with
        | PsiModuleChange.ChangeType.Added when
                projectFile.LanguageType.Is<FSharpScriptProjectFileType>() ->

            let psiModule = scriptPsiModulesProvider.GetOrCreatePsiModule(projectFile, changeBuilder)
            sourceFiles.[projectFile.Location] <- psiModule.SourceFile


        | PsiModuleChange.ChangeType.Removed when
                projectFileExtensions.GetFileType(oldLocation).Is<FSharpScriptProjectFileType>() ->

            () // todo: remove project file from list for path

        | _ -> handler.OnProjectFileChanged(projectFile, oldLocation, changeType, changeBuilder)

    override x.GetPsiSourceFilesFor(projectFile) = seq {
        locks.AssertReadAccessAllowed()
        match tryGetValue sourceFiles projectFile.Location with
        | Some sourceFile -> yield sourceFile
        | _ -> ()
        yield! handler.GetPsiSourceFilesFor(projectFile) }


 // todo: remove project file
type FSharpScriptPsiModule(lifetime, projectFile, targetFrameworkId, assemblyFactory, documentManager) as this =
    inherit ConcurrentUserDataHolder()

    let locker = JetFastSemiReenterableRWLock()

    let solution = projectFile.GetSolution()
    let psiServices = solution.GetPsiServices()
    let psiModules = solution.PsiModules()

    let scriptModule = FSharpScriptModule(projectFile.Location, solution)
    let resolveContext = lazy (PsiModuleResolveContext(this, targetFrameworkId, null))
    let assemblyCookies = DictionaryEvents<FileSystemPath, IAssemblyCookie>(lifetime, projectFile.Location.FullPath)

    let sourceFile =
        lazy (PsiProjectFile(this, projectFile,
                             (fun _ _ -> FSharpScriptFileProperties() :> _),
                             (fun _ _ -> projectFile.IsValid()),
                             documentManager, resolveContext.Value) :> IPsiSourceFile)

    do
        lifetime.AddAction(fun _ -> assemblyCookies.Clear()) |> ignore

    member x.SourceFile = sourceFile.Value
    member x.ResolveContext = resolveContext.Value

    member x.ReferencedPaths =
        use lock = locker.UsingReadLock()
        assemblyCookies.Keys

    member x.AddReference(path: FileSystemPath) =
        use lock = locker.UsingWriteLock()
        if assemblyCookies.ContainsKey(path) then () else

        let assemblyCookie = assemblyFactory.AddRef(path, projectFile.Location.FullPath, this.ResolveContext)
        lifetime.AddAction(fun _ ->
            use lock = WriteLockCookie.Create()
            assemblyCookie.Dispose()) |> ignore

        assemblyCookies.Add(path, assemblyCookie)

    member x.RemoveReference(path: FileSystemPath) =
        use lock = locker.UsingWriteLock()
        assemblyCookies.Remove(path) |> ignore

    interface IPsiModule with
        member x.Name = "F# script: " + projectFile.Location.Name
        member x.DisplayName = "F# script: " + projectFile.Location.Name
        member x.GetPersistentID() = "FSharpScriptModule:" + projectFile.Location.FullPath

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

        member x.GetAllDefines() = EmptyList.InstanceList :> _
        member x.IsValid() = psiServices.Modules.HasModule(this)


/// Holder for psi module resolve context acting similar to IProject.
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


type FSharpScriptPsiProjectFile(projectFile: IProjectFile, scriptModule: FSharpScriptPsiModule, projectFileExtensions,
                                projectFileTypeCoordinator, documentManager) =
    inherit NavigateablePsiSourceFileWithLocation(projectFileExtensions, projectFileTypeCoordinator, scriptModule,
                                                  projectFile.Location,
                                                  (fun _ -> projectFile.IsValid()),
                                                  (fun _ -> FSharpScriptFileProperties() :> _),
                                                  documentManager, scriptModule.ResolveContext)
    interface IPsiProjectFile with
        member x.ProjectFile = projectFile


type FSharpScriptFileProperties() =
    interface IPsiSourceFileProperties with
        member x.ShouldBuildPsi = true
        member x.IsGeneratedFile = false
        member x.ProvidesCodeModel = true
        member x.IsICacheParticipant = true
        member x.IsNonUserFile = false
        member x.GetPreImportedNamespaces() = Seq.empty
        member x.GetDefaultNamespace() = String.Empty
        member x.GetDefines() = EmptyList.Instance :> _
