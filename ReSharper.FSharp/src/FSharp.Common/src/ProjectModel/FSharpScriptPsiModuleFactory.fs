namespace rec JetBrains.ReSharper.Plugins.FSharp.ProjectModel

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Linq
open JetBrains.Application.Threading
open JetBrains.Application.changes
open JetBrains.Application.platforms
open JetBrains.DataFlow
open JetBrains.DocumentManagers
open JetBrains.DocumentManagers.impl
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
open Microsoft.FSharp.Compiler.SourceCodeServices

/// Provides psi module handlers that create separate psi modules for scripts with different set of
/// referenced assemblies and source files determined by "#r" and "#load" directives.
[<SolutionComponent>]
type FSharpScriptPsiModuleProviderFilter(lifetime: Lifetime, solution: ISolution, changeManager: ChangeManager,
                                         documentManager: DocumentManager, projectFileExtensions: ProjectFileExtensions,
                                         (*projectFileTypeCoordinator: PsiProjectFileTypeCoordinator,*)
                                         checkerService: FSharpCheckerService, assemblyFactory: AssemblyFactory) =
    interface IProjectPsiModuleProviderFilter with
        member x.OverrideHandler(lifetime, _, handler) =
            let handler =
                FSharpScriptPsiModuleHandler(lifetime, solution, handler, changeManager, documentManager,
                                             projectFileExtensions,
                                             (*projectFileTypeCoordinator,*)
                                             checkerService.Checker, assemblyFactory)
            handler :> _, null


type FSharpScriptPsiModuleHandler(lifetime, solution, handler, changeManager, documentManager,
                                  projectFileExtensions,
                                  (*projectFileTypeCoordinator,*)
                                  checker, assemblyFactory) as this =
    inherit DelegatingProjectPsiModuleHandler(handler)

    let [<Literal>] id = "FSharpScriptPsiModuleHandler"
    let locker = JetFastSemiReenterableRWLock()

    let miscFiles = solution.SolutionMiscFiles()
    let scriptPsiModules = DictionaryEvents<IProjectFile, LifetimeDefinition * IPsiModule>(lifetime, id)

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

    /// Prevents creating default psi source files for scripts and adds new modules for these files instead.
    override x.OnProjectFileChanged(projectFile, oldLocation, changeType, changeBuilder) =
        match changeType with
        | PsiModuleChange.ChangeType.Added when
                projectFile.LanguageType.Is<FSharpScriptProjectFileType>() ->

            use lock = locker.UsingWriteLock()

            let moduleLifetime = Lifetimes.Define(lifetime)
            let psiModule = FSharpScriptPsiModule(moduleLifetime.Lifetime, projectFile, documentManager, assemblyFactory)
            let scriptOptions = getScriptOptions projectFile

            // todo: add files to caches? add psi changes?
            let sourceFiles =
                scriptOptions.SourceFiles
                |> Array.choose (fun pathString ->
                    let path = FileSystemPath.TryParse(pathString)
                    if path.IsEmpty then None else

                    solution.FindProjectItemsByLocation(path).OfType<IProjectFile>()
                    |> Seq.tryHead
                    |> Option.orElseWith (fun _ ->
                        Some (miscFiles.CreateMiscFile(path)))) // todo: make writable by default
                |> Array.map (fun projectFile ->
                    PsiProjectFile(psiModule, projectFile,
                        (fun _ _ -> FSharpScriptFileProperties(psiModule) :> _),
                        (fun _ _ -> projectFile.IsValid()),
                        documentManager, psiModule.ResolveContext) :> IPsiSourceFile)
//                    FSharpScriptPsiProjectFile(projectFile, scriptModule, projectFileExtensions,
//                                               projectFileTypeCoordinator, documentManager) :> IPsiSourceFile)
            psiModule.SourceFiles <- sourceFiles

            for path in getReferencedPaths scriptOptions do
                psiModule.AddReference(path)

            changeBuilder.AddModuleChange(psiModule, PsiModuleChange.ChangeType.Added)
            for file in sourceFiles do
                changeBuilder.AddFileChange(file, PsiModuleChange.ChangeType.Added)

            scriptPsiModules.[projectFile] <- (moduleLifetime, psiModule :> _)

        | PsiModuleChange.ChangeType.Removed when
                projectFileExtensions.GetFileType(oldLocation).Is<FSharpScriptProjectFileType>() ->

            use lock = locker.UsingWriteLock()
            tryGetValue scriptPsiModules projectFile
            |> Option.iter (fun (_, psiModule) ->
                changeBuilder.AddModuleChange(psiModule, PsiModuleChange.ChangeType.Removed)
                scriptPsiModules.Remove(projectFile) |> ignore)

        | _ -> handler.OnProjectFileChanged(projectFile, oldLocation, changeType, changeBuilder)

    override x.GetAllModules() =
        use lock = locker.UsingReadLock()
        handler.GetAllModules().Concat(scriptPsiModules.Values |> Seq.map (fun (_, m) -> m)).ToIList()

    override x.GetPsiSourceFilesFor(projectFile) =
        use lock = locker.UsingReadLock()
        scriptPsiModules.Values
        |> Seq.collect (fun (_, m) -> m.SourceFiles |> Seq.filter (fun f -> f.ToProjectFile() = projectFile))
        |> Seq.append (handler.GetPsiSourceFilesFor(projectFile))

    interface IChangeProvider with
        member x.Execute(changeMap) =
            let change = changeMap.GetChange<ProjectFileDocumentCopyChange>(documentManager.ChangeProvider)
            let psiModules = solution.PsiModules()
            if isNull change then null else

            let affectedscriptPsiModules =
                psiModules.GetPsiSourceFilesFor(change.ProjectFile).AsEnumerable()
                |> Seq.choose (fun sf ->
                    match sf.PsiModule with
                    | :? FSharpScriptPsiModule as scriptModule -> Some scriptModule
                    | _ -> None)
                |> List.ofSeq

            null


type FSharpScriptPsiModule(lifetime, projectFile, documentManager, assemblyFactory) as this =
    inherit ConcurrentUserDataHolder()

    let locker = JetFastSemiReenterableRWLock()

    let solution = projectFile.GetSolution()
    let psiServices = solution.GetPsiServices()
    let psiModules = solution.PsiModules()

    let scriptModule = FSharpScriptModule(projectFile)
    let resolveContext = lazy (PsiModuleResolveContext(this, TargetFrameworkId.Default, null))
    let references = DictionaryEvents<FileSystemPath, AssemblyReference>(lifetime, projectFile.Location.FullPath)

    do
        references.SuppressItemErrors <- true
        references.AddRemove.Advise_Remove(lifetime, fun args ->
            let reference = args.Value.Value
            reference.Assembly.Dispose()) |> ignore
        lifetime.AddAction(fun _ -> references.Clear()) |> ignore

    member val SourceFiles: IPsiSourceFile[] = null with get, set

    member x.ResolveContext = resolveContext.Value
    member x.ReferencedPaths = references.Keys

    member x.AddReference(path: FileSystemPath) =
        use lock = locker.UsingWriteLock()
        if references.ContainsKey(path) then () else

        let assemblyCookie = assemblyFactory.AddRef(path, projectFile.Location.FullPath, this.ResolveContext)
        match psiModules.GetPrimaryPsiModule(assemblyCookie.Assembly, TargetFrameworkId.Default) with
        | null -> assemblyCookie.Dispose()
        | assemblyModule ->
            let reference = { Assembly = assemblyCookie; Reference = PsiModuleReference(assemblyModule) }
            references.Add(path, reference) |> ignore

    member x.RemoveReference(path: FileSystemPath) =
        use lock = locker.UsingWriteLock()
        references.Remove(path) |> ignore

    interface IPsiModule with
        member x.Name = "F# script module: " + projectFile.Name
        member x.DisplayName = "F# script module: " + projectFile.Name
        member x.GetPersistentID() = "FSharpScriptModule:" + projectFile.Location.FullPath

        member x.GetSolution() = solution
        member x.GetPsiServices() = psiServices

        // todo: references to project psi modules with incompatible id, e.g. script -> coreapp
        member x.TargetFrameworkId = TargetFrameworkId.Default

        member x.PsiLanguage = FSharpLanguage.Instance :> _
        member x.ProjectFileType = UnknownProjectFileType.Instance :> _

        // todo: add source file sorter to prefer own source file for fsx over loaded into another script
        member x.SourceFiles = x.SourceFiles :> _ 

        member x.GetReferences(resolveContext) =
            use lock = locker.UsingReadLock()
            references.Values
            |> Seq.map (fun ref -> ref.Reference)

        member x.ContainingProjectModule = scriptModule :> _
        member x.GetAllDefines() = EmptyList.InstanceList :> _
        member x.IsValid() = projectFile.IsValid() && psiServices.Modules.HasModule(this)


type FSharpScriptModule(projectFile: IProjectFile) =
    inherit UserDataHolder()

    interface IModule with
        member x.Presentation = projectFile.Name
        member x.Name = projectFile.Location.FullPath

        member x.IsValid() = projectFile.IsValid()
        member x.GetSolution() = projectFile.GetSolution()
        member x.Accept(visitor) = visitor.VisitProjectModelElement(x)
        member x.MarshallerType = null

        member x.GetProperty(key) = base.GetData(key)
        member x.SetProperty(key, value) = base.PutData(key, value)


type FSharpScriptPsiProjectFile(projectFile: IProjectFile, scriptModule: FSharpScriptPsiModule, projectFileExtensions,
                                projectFileTypeCoordinator, documentManager) =
    inherit NavigateablePsiSourceFileWithLocation(projectFileExtensions, projectFileTypeCoordinator, scriptModule,
                                                  projectFile.Location,
                                                  (fun _ -> projectFile.IsValid()),
                                                  (fun _ -> FSharpScriptFileProperties(scriptModule) :> _),
                                                  documentManager, scriptModule.ResolveContext)
    interface IPsiProjectFile with
        member x.ProjectFile = projectFile


type FSharpScriptFileProperties(psiModule: IPsiModule) =
    interface IPsiSourceFileProperties with
        member x.ShouldBuildPsi = true
        member x.IsGeneratedFile = false
        member x.ProvidesCodeModel = true
        member x.IsICacheParticipant = true
        member x.IsNonUserFile = false
        member x.GetPreImportedNamespaces() = Seq.empty
        member x.GetDefaultNamespace() = String.Empty
        member x.GetDefines() = EmptyList.Instance :> _


type AssemblyReference =
    { Assembly: IAssemblyCookie
      Reference: IPsiModuleReference }