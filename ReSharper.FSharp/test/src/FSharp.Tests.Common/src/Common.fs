namespace JetBrains.ReSharper.Plugins.FSharp.Tests

open System
open System.Collections.Generic
open System.IO
open FSharp.Compiler.CodeAnalysis
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Components
open JetBrains.Application.platforms
open JetBrains.DataFlow
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Properties
open JetBrains.ProjectModel.Properties.Managed
open JetBrains.ReSharper.FeaturesTestFramework.Refactorings
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Caches
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.TestFramework
open JetBrains.TestFramework
open JetBrains.TestFramework.Projects
open JetBrains.Util
open JetBrains.Util.Dotnet.TargetFrameworkIds
open Moq

module FSharpTestUtil =
    let referencedProjectGuid = "C13207C7-045E-485A-BC1A-AFA1472CD8BC"

    let createTestFileSets (test: BaseTestNoShell) (mainExtension: string) (referencedExtension: string) =
        let mainFilePath = test.TestDataPath / (test.TestMethodName + mainExtension)
        let secondFilePath = test.TestDataPath / (test.TestMethodName + referencedExtension)
    
        [| [| mainFilePath.FullPath; referencedProjectGuid |]; [| secondFilePath.FullPath |] |]

    let referenceCSharpProject (test: BaseTestNoShell) =
        createTestFileSets test FSharpProjectFileType.FsExtension CSharpProjectFileType.CS_EXTENSION

module FSharpTestAttribute =
    let extensions =
        [| FSharpProjectFileType.FsExtension
           FSharpSignatureProjectFileType.FsiExtension |]
        |> HashSet

    let targetFrameworkId =
        TargetFrameworkId.Create(FrameworkIdentifier.NetFramework, Version(4, 5, 1), ProfileIdentifier.Default)


[<AutoOpen>]
module PackageReferences =
    let [<Literal>] FSharpCorePackage = "FSharp.Core/4.7.2"
    let [<Literal>] JetBrainsAnnotationsPackage = "JetBrains.Annotations/2022.1.0"
    let [<Literal>] SqlProviderPackage = "SQLProvider/1.1.101"
    let [<Literal>] FsPickler = "FsPickler/5.3.2"

type ITestAssemblyReaderShim =
    abstract CreateReferencedProjectCookie: IProject -> IDisposable
    abstract Dispose: unit -> unit

type FSharpTestAttribute(extension) =
    inherit TestAspectAttribute()

    member val ReferenceFSharpCore = true with get, set

    new () =
        FSharpTestAttribute(FSharpProjectFileType.FsExtension)

    interface ITestPackagesProvider with
        override this.GetPackages _ =
            [| if this.ReferenceFSharpCore then
                TestPackagesAttribute.ParsePackageName(FSharpCorePackage) |] :> _

        member this.Inherits = false

    interface ITestLibraryReferencesProvider with
        member this.GetReferences(_, _, _) = JetBrains.Util.EmptyArray.Instance
        member this.Inherits = false

    interface ITestTargetFrameworkIdProvider with
        member x.GetTargetFrameworkId() = FSharpTestAttribute.targetFrameworkId
        member this.Inherits = false

    interface ITestFileExtensionProvider with
        member x.Extension = extension

    interface ITestProjectPropertiesProvider with
        member x.GetProjectProperties(_, targetFrameworkIds, _) =
            FSharpProjectPropertiesFactory.CreateProjectProperties(targetFrameworkIds)

    interface ITestProjectOutputTypeProvider with
        member x.Priority = -1
        member x.ProjectOutputType = ProjectOutputType.CONSOLE_EXE

    interface ITestProjectFilePropertiesProvider with
        member x.Process(path, properties, projectDescriptor) =
            if FSharpTestAttribute.extensions.Contains(path.ExtensionWithDot) then
                for targetFrameworkId in projectDescriptor.ProjectProperties.ActiveConfigurations.TargetFrameworkIds do
                    properties.SetBuildAction(BuildAction.COMPILE, targetFrameworkId)

    override this.OnBeforeTestExecute(context) =
        let context = context
        let testProject = context.TestProject
        let referencedProjects = testProject.GetReferencedProjects(testProject.GetCurrentTargetFrameworkId())
        for referencedProject in referencedProjects do
            let isCSharpOrVb (projectFile: IProjectFile) =
                let fileType = projectFile.LanguageType
                fileType.Is<CSharpProjectFileType>() || fileType.Is<VBProjectFileType>()

            let files = referencedProject.GetAllProjectFiles()
            if files |> Seq.exists isCSharpOrVb then
                let testAssemblyReaderShim = referencedProject.GetSolution().GetComponent<ITestAssemblyReaderShim>()
                testAssemblyReaderShim.CreateReferencedProjectCookie(referencedProject) |> ignore

    override this.OnAfterTestExecute(context) =
        let solution = context.TestProject.GetSolution()
        let testAssemblyReaderShim = solution.GetComponent<ITestAssemblyReaderShim>()
        testAssemblyReaderShim.Dispose()

type FSharpSignatureTestAttribute() =
    inherit FSharpTestAttribute(FSharpSignatureProjectFileType.FsiExtension)


type FSharpScriptTestAttribute() =
    inherit FSharpTestAttribute(FSharpScriptProjectFileType.FsxExtension)


[<AttributeUsage(AttributeTargets.Method ||| AttributeTargets.Class, Inherited = false)>]
type FSharpLanguageLevelAttribute(languageLevel: FSharpLanguageLevel) =
    inherit TestAspectAttribute()

    override this.OnBeforeTestExecute(context) =
        let project = context.TestProject

        let property = project.GetSolution().GetComponent<FSharpLanguageLevelProjectProperty>()
        property.OverrideLanguageLevel(context.TestLifetime, languageLevel, project)

        PsiFileCachedDataUtil.InvalidateInAllProjectFiles(project, FSharpLanguage.Instance, FSharpLanguageLevel.key)


[<AttributeUsage(AttributeTargets.Method ||| AttributeTargets.Class, Inherited = false)>]
type TestDefinesAttribute(defines: string) =
    inherit TestAspectAttribute()

    override this.OnBeforeTestExecute(context) =
        let projectProperties = context.TestProject.ProjectProperties
        for projectConfiguration in projectProperties.GetActiveConfigurations<IManagedProjectConfiguration>() do
            let oldDefines = projectConfiguration.DefineConstants
            projectConfiguration.DefineConstants <- defines
            context.TestLifetime.OnTermination(fun _ -> projectConfiguration.DefineConstants <- oldDefines) |> ignore


[<AttributeUsage(AttributeTargets.Method ||| AttributeTargets.Class, Inherited = false)>]
type FSharpExperimentalFeatureAttribute(feature: ExperimentalFeature) =
    inherit TestAspectAttribute()

    let mutable cookie: IDisposable = Unchecked.defaultof<_>

    override this.OnBeforeTestExecute _ =
        cookie <- FSharpExperimentalFeatureCookie.Create(feature)

    override this.OnAfterTestExecute _ =
        cookie.Dispose()
        cookie <- null

[<SolutionComponent>]
type TestModifiedFilesCache(psiFilesCache: IPsiFilesCache ) =
    member val ModifiedFileCookies = Dictionary<IPsiSourceFile, IDisposable>()
    member this.PsiFilesCache = psiFilesCache

    interface IPsiCache with
        member this.Invalidate(element: ITreeNode, _) =
            if isNull element then () else

            let sourceFile = element.GetSourceFile()
            if isNull sourceFile then () else

            if not (this.ModifiedFileCookies.ContainsKey(sourceFile)) then
                this.ModifiedFileCookies[sourceFile] <- this.PsiFilesCache.GetTransientCookie(sourceFile)

type AssertCorrectTreeStructureAttribute() =
    inherit TestAspectAttribute()
  
    override this.OnAfterTestExecute(context: TestAspectAttribute.TestAspectContext) =
        let dumpFile (sourceFile: IPsiSourceFile) =
            let file = sourceFile.GetPrimaryPsiFile()
            let writer = new StringWriter()
            DebugUtil.DumpPsi(writer, file, fun n w -> DebugUtil.DumpNode(n, w))
            writer.ToString()
    
        let solution = context.TestProject.GetSolution()
        let modifiedFilesCache = solution.GetComponent<TestModifiedFilesCache>()
        ReadLockCookie.Execute(fun _ -> solution.GetPsiServices().Files.CommitAllDocuments())
    
        try
            for KeyValue(sourceFile, cookie) in modifiedFilesCache.ModifiedFileCookies do
                if (not (sourceFile.IsValid())) then () else

                let afterModification = dumpFile sourceFile

                cookie.Dispose()
                modifiedFilesCache.PsiFilesCache.Drop(sourceFile)

                let afterParse = dumpFile sourceFile
                if afterModification = afterParse then () else

                let file = FileSystemDefinition.CreateTemporaryFile(
                    extensionWithDot = ".gold",
                    handler = fun stream ->
                        let writer = new StreamWriter(stream)
                        writer.Write(afterParse)
                        writer.Flush()
                    )

                context.TestFixture.ExecuteWithSpecifiedGold(file, fun writer -> writer.Write(afterModification))
                |> ignore
        finally
            modifiedFilesCache.ModifiedFileCookies.Clear()


[<SolutionComponent>]
[<ZoneMarker(typeof<ITestFSharpPluginZone>)>]
type TestFSharpResolvedSymbolsCache(lifetime, checkerService, psiModules, fcsProjectProvider, scriptModuleProvider, locks) =
    inherit FcsResolvedSymbolsCache(lifetime, checkerService, psiModules, fcsProjectProvider, scriptModuleProvider, locks)

    override x.Invalidate _ =
        x.ProjectSymbolsCaches.Clear()

    interface IHideImplementation<FcsResolvedSymbolsCache>


[<SolutionComponent>]
[<ZoneMarker(typeof<ITestFSharpPluginZone>)>]
type TestFcsProjectBuilder(checkerService, modulePathProvider, logger, psiModules) =
    inherit FcsProjectBuilder(checkerService, Mock<_>().Object, modulePathProvider, logger, psiModules)

    override x.GetProjectItemsPaths(project, targetFrameworkId) =
        project.GetAllProjectFiles()
        |> Seq.filter (fun file -> file.LanguageType.Is<FSharpProjectFileType>())
        |> Seq.map (fun file -> file.Location, file.Properties.GetBuildAction(targetFrameworkId))
        |> Seq.toArray

    interface IHideImplementation<FcsProjectBuilder>


[<SolutionComponent>]
[<ZoneMarker(typeof<ITestFSharpPluginZone>)>]
type TestFcsProjectProvider(lifetime: Lifetime, checkerService: FcsCheckerService,
        fcsProjectBuilder: FcsProjectBuilder, scriptFcsProjectProvider: IScriptFcsProjectProvider) as this =
    do
        checkerService.FcsProjectProvider <- this
        lifetime.OnTermination(fun _ -> checkerService.FcsProjectProvider <- Unchecked.defaultof<_>) |> ignore

    let mutable currentFcsProject = None

    let getNewFcsProject (psiModule: IPsiModule) =
        let projectKey = FcsProjectKey.Create(psiModule)
        fcsProjectBuilder.BuildFcsProject(projectKey)

    // todo: referenced projects
    // todo: unify with FcsProjectProvider check
    let areSameForChecking (newProject: FcsProject) (oldProject: FcsProject) =
        let getReferencedProjectOutputs (options: FSharpProjectOptions) =
            options.ReferencedProjects |> Array.map (fun project -> project.OutputFile)

        let newOptions = newProject.ProjectOptions
        let oldOptions = oldProject.ProjectOptions

        newOptions.ProjectFileName = oldOptions.ProjectFileName &&
        newOptions.SourceFiles = oldOptions.SourceFiles &&
        newOptions.OtherOptions = oldOptions.OtherOptions &&
        getReferencedProjectOutputs newOptions = getReferencedProjectOutputs oldOptions

    let getFcsProject (psiModule: IPsiModule) =
        lock this (fun _ ->
            let newFcsProject = getNewFcsProject psiModule
            match currentFcsProject with
            | Some oldFcsProject when areSameForChecking newFcsProject oldFcsProject ->
                oldFcsProject
            | _ ->
                currentFcsProject <- Some(newFcsProject)
                newFcsProject
        )

    let getProjectOptions (sourceFile: IPsiSourceFile) =
        let fcsProject = getFcsProject sourceFile.PsiModule
        Some fcsProject.ProjectOptions

    interface IHideImplementation<FcsProjectProvider>

    interface IFcsProjectProvider with
        member x.HasPairFile sourceFile =
            let fcsProject = getFcsProject sourceFile.PsiModule
            fcsProject.ImplementationFilesWithSignatures.Contains(sourceFile.GetLocation())

        member x.GetProjectOptions(sourceFile: IPsiSourceFile) =
            if sourceFile.LanguageType.Is<FSharpScriptProjectFileType>() then
                scriptFcsProjectProvider.GetScriptOptions(sourceFile) else

            getProjectOptions sourceFile

        member x.GetParsingOptions(sourceFile) =
            if isNull sourceFile then sandboxParsingOptions else

            let isScript = sourceFile.LanguageType.Is<FSharpScriptProjectFileType>()
            let targetFrameworkId = sourceFile.PsiModule.TargetFrameworkId

            let isExe =
                match sourceFile.GetProject() with
                | null -> false
                | project ->

                match project.ProjectProperties.ActiveConfigurations.TryGetConfiguration(targetFrameworkId) with
                | :? IManagedProjectConfiguration as cfg ->
                    cfg.OutputType = ProjectOutputType.CONSOLE_EXE
                | _ -> false

            let paths =
                if isScript then
                    [| sourceFile.GetLocation().FullPath |]
                else
                    let project = sourceFile.GetProject().NotNull()
                    fcsProjectBuilder.GetProjectItemsPaths(project, targetFrameworkId) |> Array.map (fst >> string)

            let defines =
                if isScript then
                    ImplicitDefines.scriptDefines
                else
                    sourceFile.GetProject().NotNull()
                    |> FcsProjectBuilder.getProjectConfiguration targetFrameworkId
                    |> FcsProjectBuilder.getDefines
                    |> List.append ImplicitDefines.sourceDefines 

            { FSharpParsingOptions.Default with
                SourceFiles = paths
                ConditionalDefines = defines
                IsExe = isExe
                IsInteractive = isScript
                LangVersionText = "preview" } // todo: fix language level attribute is not applied

        member x.GetFileIndex(sourceFile) =
            if sourceFile.LanguageType.Is<FSharpScriptProjectFileType>() then 0 else

            let fcsProject = getFcsProject sourceFile.PsiModule
            match tryGetValue (sourceFile.GetLocation()) fcsProject.FileIndices with
            | Some index -> index
            | _ -> -1

        member x.ProjectRemoved = new Signal<_>("Todo") :> _

        member x.InvalidateReferencesToProject _ = false
        member x.HasFcsProjects = false
        member this.GetAllFcsProjects() = []

        member this.GetProjectOptions(_: IPsiModule): FSharpProjectOptions option = failwith "todo"
        member this.GetFcsProject(psiModule) = Some (getFcsProject psiModule)
        member this.PrepareAssemblyShim _ = ()
        member this.GetReferencedModule _ = None
        member this.GetPsiModule _ = failwith "todo"
        member this.GetAllReferencedModules() = failwith "todo"


[<SolutionComponent>]
[<ZoneMarker(typeof<ITestFSharpPluginZone>)>]
type TestAssemblyReaderShim(lifetime, changeManager, psiModules, cache, assemblyInfoShim,
        fsOptionsProvider, symbolCache, solution, locks, logger) =
    inherit AssemblyReaderShim(lifetime, changeManager, psiModules, cache, assemblyInfoShim,
        fsOptionsProvider, symbolCache, solution, locks, logger)

    let mutable projectPath = VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext)
    let mutable referencedProject = Unchecked.defaultof<_>
    let mutable reader = Unchecked.defaultof<_>

    member this.ReferencedProject = referencedProject
    member this.Path = projectPath

    override this.DebugReadRealAssemblies = false

    member this.Dispose() =
            projectPath <- VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext)
            referencedProject <- Unchecked.defaultof<_>
            reader <- Unchecked.defaultof<_>

    interface ITestAssemblyReaderShim with
        member this.CreateReferencedProjectCookie(project: IProject) =
            Assertion.Assert(isNull reader)

            let path = project.Location / (project.Name + ".dll")
            let psiModule = psiModules.GetPrimaryPsiModule(project, project.TargetFrameworkIds.SingleItem())

            projectPath <- path
            referencedProject <- project
            reader <- new ProjectFcsModuleReader(psiModule, cache, path, this, None)

            { new IDisposable with
                member x.Dispose() =
                    this.Dispose() }

        member this.Dispose() = this.Dispose()

    override this.ExistsFile(path) =
        path = projectPath || base.ExistsFile(path)

    override this.GetLastWriteTime(path) =
        if path = projectPath then DateTime.MinValue else base.GetLastWriteTime(path)

    override this.GetModuleReader(path, readerOptions) =
        if path = projectPath then reader :> _ else base.GetModuleReader(path, readerOptions)

    interface IHideImplementation<AssemblyReaderShim>


module FSharpTestPopup =
    let [<Literal>] OccurrenceName = "OCCURRENCE"

    let setOccurrence occurrenceName assertExists (solution: ISolution) (lifetime: Lifetime) =
        if isNull occurrenceName then () else

        let workflowPopupMenu = solution.GetComponent<TestWorkflowPopupMenu>()
        workflowPopupMenu.SetTestData(lifetime, fun _ occurrences _ _ _ ->
            occurrences
            |> Array.tryFind (fun occurrence -> occurrence.Name.Text = occurrenceName)
            |> Option.defaultWith (fun _ ->
                if assertExists then
                    failwithf $"Could not find %s{occurrenceName} occurrence"
                else
                    occurrences
                    |> Seq.tryLast
                    |> Option.defaultValue null
            )
        )
