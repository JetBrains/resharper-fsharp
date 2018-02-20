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

/// Provides psi module handlers that create separate psi modules for scripts with different set of
/// referenced assemblies and source files determined by "#r" and "#load" directives.
[<SolutionComponent>]
type FSharpScriptsModuleProviderFilter(lifetime: Lifetime, solution: ISolution, changeManager: ChangeManager,
                                       documentManager: DocumentManager, projectFileExtensions: ProjectFileExtensions,
                                       checkerService: FSharpCheckerService, assemblyFactory: AssemblyFactory) =
    let assemblies = ConcurrentDictionary<FileSystemPath, IAssemblyCookie>()
    do
        lifetime.AddAction(fun _ ->
            use writeLock = WriteLockCookie.Create()
            for assembly in assemblies.Values do assembly.Dispose()) |> ignore

    interface IProjectPsiModuleProviderFilter with
        member x.OverrideHandler(lifetime, project, handler) =
            let handler =
                FSharpScriptPsiModuleHandler(lifetime, solution, handler, changeManager, documentManager, assemblies,
                                             projectFileExtensions, checkerService.Checker, assemblyFactory)
            handler :> _, null


type FSharpScriptPsiModuleHandler(lifetime, solution, handler, changeManager, documentManager, assemblies,
                                  projectFileExtensions, checker, assemblyFactory) as this =
    inherit DelegatingProjectPsiModuleHandler(handler)

    let [<Literal>] holderId = "FSharpScriptPsiModuleHandler"

    let scriptModules = Dictionary<IProjectFile, IPsiModule>()
    let locker = JetFastSemiReenterableRWLock()
    let psiModules = solution.PsiModules()
    let miscFiles = solution.SolutionMiscFiles()

    do
        changeManager.RegisterChangeProvider(lifetime, this)
        changeManager.AddDependency(lifetime, this, documentManager.ChangeProvider)

    /// Prevents creating default psi source files for scripts and adds new modules for these files instead.
    override x.OnProjectFileChanged(projectFile, oldLocation, changeType, changeBuilder) =
        match changeType with
        | PsiModuleChange.ChangeType.Added when
                projectFile.LanguageType.Is<FSharpScriptProjectFileType>() ->

            use lock = locker.UsingWriteLock()

            let scriptPath = projectFile.Location
            let source = projectFile.GetDocument().GetText()
            let scriptOptions, _ = checker.GetProjectOptionsFromScript(scriptPath.FullPath, source).RunAsTask()

            let scriptModule = FSharpScriptPsiModule(projectFile, projectFileExtensions, documentManager)
            let project = projectFile.GetProject()
            let resolveContext = PsiModuleResolveContext(scriptModule, TargetFrameworkId.Default, project)
            scriptModule.ResolveContext <- resolveContext

            // todo: add files to caches? add psi changes?
            let sourceFiles =
                scriptOptions.SourceFiles
                |> Array.choose (fun pathString ->
                    let path = FileSystemPath.TryParse(pathString)
                    if path.IsEmpty || path = scriptPath then None else

                    solution.FindProjectItemsByLocation(path).OfType<IProjectFile>()
                    |> Seq.tryHead
                    |> Option.orElseWith (fun _ -> Some (miscFiles.CreateMiscFile(path)))) // todo: make writable by default
                |> Array.append [| projectFile |]
                |> Array.map (fun projectFile ->
                    PsiProjectFile(scriptModule, projectFile,
                        (fun _ _ -> FSharpScriptFileProperties(scriptModule) :> _),
                        (fun _ _ -> projectFile.IsValid()),
                        documentManager, scriptModule.ResolveContext) :> IPsiSourceFile)
            scriptModule.SourceFiles <- sourceFiles

            let assemblyPaths =
                scriptOptions.OtherOptions
                |> Array.choose (fun o ->
                    if o.StartsWith("-r:", StringComparison.OrdinalIgnoreCase) then
                        let path = FileSystemPath.TryParse(o.Substring(3))
                        if path.IsEmpty then None else Some path
                    else None)
                |> HashSet

            let references =
                assemblyPaths.ToArray()
                |> Array.choose (fun path ->
                    let assemblyCookie = assemblies.GetOrCreateValue(path, fun () ->
                        assemblyFactory.AddRef(path, holderId, scriptModule.ResolveContext))
                    match psiModules.GetPrimaryPsiModule(assemblyCookie.Assembly, TargetFrameworkId.Default) with
                    | null -> None
                    | assemblyModule -> Some (PsiModuleReference(assemblyModule) :> IPsiModuleReference))
            scriptModule.References <- references

            changeBuilder.AddModuleChange(scriptModule, PsiModuleChange.ChangeType.Added)
            scriptModules.[projectFile] <- scriptModule

        | PsiModuleChange.ChangeType.Removed when
                projectFileExtensions.GetFileType(oldLocation).Is<FSharpScriptProjectFileType>() ->

            use lock = locker.UsingWriteLock()
            match scriptModules.TryGetValue(projectFile) with
            | null -> ()
            | scriptModule ->
                changeBuilder.AddModuleChange(scriptModule, PsiModuleChange.ChangeType.Removed)
                scriptModules.Remove(projectFile) |> ignore

        | _ -> handler.OnProjectFileChanged(projectFile, oldLocation, changeType, changeBuilder)

    override x.GetAllModules() =
        use lock = locker.UsingReadLock()
        handler.GetAllModules().Concat(scriptModules.Values).ToIList()

    override x.GetPsiSourceFilesFor(projectFile) =
        use lock = locker.UsingReadLock()
        match projectFile with
        | file when file.LanguageType.Is<FSharpScriptProjectFileType>() ->
            match scriptModules.TryGetValue(file) with
            | null -> Seq.empty
            | scriptModule -> scriptModule.SourceFiles
        | _ -> handler.GetPsiSourceFilesFor(projectFile)

    interface IChangeProvider with
        member x.Execute(changeMap) =
            let change = changeMap.GetChange<ProjectFileDocumentCopyChange>(documentManager.ChangeProvider)
            if isNull change then null else

            let projectFile = change.ProjectFile
            if projectFile.LanguageType.Is<FSharpScriptProjectFileType>() |> not then null else // todo: impl/sig files loaded to script

            null
            

type FSharpScriptPsiModule(projectFile, projectFileExtensions, documentManager) as this =
    inherit ConcurrentUserDataHolder()

    let project = projectFile.GetProject()
    let solution = projectFile.GetSolution()
    let psiServices = solution.GetPsiServices()

    let sourceFile =
        lazy
            PsiProjectFile(this, projectFile,
                (fun _ _ -> FSharpScriptFileProperties(this) :> _),
                (fun _ _ -> projectFile.IsValid()),
                documentManager, this.ResolveContext) :> IPsiSourceFile

    member val ResolveContext: PsiModuleResolveContext = null with get, set
    member val SourceFiles: IPsiSourceFile[] = null with get, set
    member val References: IPsiModuleReference[] = null with get, set

    interface IPsiModule with
        member x.Name = projectFile.Name + " module"
        member x.DisplayName = projectFile.Name + " module"
        member x.GetPersistentID() = "FSharpScriptModule:" + projectFile.Location.FullPath

        member x.GetSolution() = solution
        member x.GetPsiServices() = psiServices
        member x.TargetFrameworkId = TargetFrameworkId.Default // todo: get highest known

        member x.PsiLanguage = FSharpLanguage.Instance :> _
        member x.ProjectFileType = FSharpScriptProjectFileType.Instance :> _

        // todo: add file sorter to prefer own source file for fsx over loaded into another script
        member x.SourceFiles = x.SourceFiles :> _ 
        member x.GetReferences(resolveContext) = x.References :> _

        member x.ContainingProjectModule = project :> _
        member x.GetAllDefines() = EmptyList.InstanceList :> _
        member x.IsValid() = projectFile.IsValid() && psiServices.Modules.HasModule(this)


type FSharpScriptFileProperties(psiModule: IPsiModule) =
    inherit DefaultPropertiesForFileInProject(psiModule.ContainingProjectModule :?> IProject, psiModule)

    override x.ShouldBuildPsi = true
    override x.IsGeneratedFile = false
    override x.ProvidesCodeModel = true
    override x.IsICacheParticipant = true
    override x.IsNonUserFile = false