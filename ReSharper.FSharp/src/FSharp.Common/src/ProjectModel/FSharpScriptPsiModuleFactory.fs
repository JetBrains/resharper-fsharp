namespace rec JetBrains.ReSharper.Plugins.FSharp.ProjectModel

open System
open System.Collections.Generic
open System.Linq
open JetBrains.Application.changes
open JetBrains.Metadata.Reader.API
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Common.Checker
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Impl
open JetBrains.ReSharper.Psi.Modules
open JetBrains.Threading
open JetBrains.Util

// Do not create F# script source files in default project psi modules.
// We want these files to be in separate psi modules with different set of referenced assemblies
// and source files determined by "#r" and "#load" directives.

[<SolutionComponent>]
type FSharpScriptsModuleProviderFilter(projectFileExtensions, projectFileTypeCoordinator, documentManager) =
    interface IProjectPsiModuleProviderFilter with
        member x.OverrideHandler(lifetime, project, handler) =
            let handler = FSharpScriptPsiModuleHandler(handler, projectFileExtensions, projectFileTypeCoordinator,
                                                       documentManager)
            handler :> _, null


type FSharpScriptPsiModuleHandler(handler, projectFileExtensions: ProjectFileExtensions, projectFileTypeCoordinator,
                                  documentManager) =
    inherit DelegatingProjectPsiModuleHandler(handler)

    let scriptModules = Dictionary<IProjectFile, IPsiModule>()
    let locker = JetFastSemiReenterableRWLock() // for review: ever needed?

    override x.OnProjectFileChanged(projectFile, oldLocation, changeType, changeBuilder) =
        match changeType with
        | PsiModuleChange.ChangeType.Added when
                projectFile.LanguageType.Is<FSharpScriptProjectFileType>() ->

            use lock = locker.UsingWriteLock()
            let scriptModule =
                FSharpScriptPsiModule(projectFile, projectFileExtensions, projectFileTypeCoordinator, documentManager)

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


type FSharpScriptFileProperties(file: IProjectFile, psiModule: IPsiModule) =
    inherit DefaultPropertiesForFileInProject(file.GetProject(), psiModule)

    override x.ShouldBuildPsi = true
    override x.IsGeneratedFile = false
    override x.ProvidesCodeModel = true
    override x.IsICacheParticipant = true
    override x.IsNonUserFile = false


type FSharpScriptPsiModule(projectFile, projectFileExtensions, projectFileTypeCoordinator, documentManager) as this =
    inherit ConcurrentUserDataHolder()

    let project = projectFile.GetProject()
    let solution = projectFile.GetSolution()
    let psiServices = solution.GetPsiServices()

    let sourceFile =
        lazy
            PsiProjectFile(this, projectFile,
                (fun _ _ -> FSharpScriptFileProperties(projectFile, this) :> _),
                (fun _ _ -> projectFile.IsValid()),
                documentManager, UniversalModuleReferenceContext.Instance) :> IPsiSourceFile

    member x.ScriptProjectFile = projectFile

    interface IPsiModule with
        member x.Name = projectFile.Name + " module"
        member x.DisplayName = projectFile.Name + " module"
        member x.GetPersistentID() = "FSharpScriptModule:" + projectFile.Location.FullPath

        member x.GetSolution() = solution
        member x.GetPsiServices() = psiServices
        member x.TargetFrameworkId = TargetFrameworkId.Default // todo: get highest known

        member x.PsiLanguage = FSharpLanguage.Instance :> _
        member x.ProjectFileType = FSharpScriptProjectFileType.Instance :> _

        // todo: add included source files
        // todo: add file sorter to prefer own source file for fsx over loaded into another script
        member x.SourceFiles = seq { yield sourceFile.Value } 

        member x.GetReferences(resolveContext) = Seq.empty // todo

        member x.ContainingProjectModule = project :> _
        member x.GetAllDefines() = EmptyList.InstanceList :> _
        member x.IsValid() = projectFile.IsValid() && psiServices.Modules.HasModule(this)
