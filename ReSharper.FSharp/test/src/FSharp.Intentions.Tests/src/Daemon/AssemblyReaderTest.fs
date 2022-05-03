namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon

open System
open JetBrains.Application.Components
open JetBrains.Application.changes
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Daemon.Impl
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.FeaturesTestFramework.Daemon
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.Daemon.Stages
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader
open JetBrains.ReSharper.Plugins.FSharp.Shim.FileSystem
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Caches
open JetBrains.ReSharper.Psi.Modules
open JetBrains.Util
open NUnit.Framework

[<SolutionComponent>]
type TestAssemblyReaderShim(lifetime: Lifetime, changeManager: ChangeManager,
        psiModules: IPsiModules, cache: FcsModuleReaderCommonCache, assemblyInfoShim: AssemblyInfoShim,
        checkerService: FcsCheckerService, fsOptionsProvider: FSharpOptionsProvider, symbolCache: ISymbolCache) =
    inherit AssemblyReaderShim(lifetime, changeManager, psiModules, cache, assemblyInfoShim, checkerService,
        fsOptionsProvider, symbolCache)

    let mutable projectPath = VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext)
    let mutable projectPsiModule = null
    let mutable reader = Unchecked.defaultof<_>

    member this.PsiModule = projectPsiModule
    member this.Path = projectPath

    override this.DebugReadRealAssemblies = false

    member this.CreateProjectCookie(path: VirtualFileSystemPath, psiModule: IPsiModule) =
        projectPath <- path
        projectPsiModule <- psiModule
        reader <- new ProjectFcsModuleReader(projectPsiModule, cache, this)

        { new IDisposable with
            member x.Dispose() =
                projectPath <- VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext)
                projectPsiModule <- null
                reader <- Unchecked.defaultof<_> }

    override this.ExistsFile(path) =
        path = projectPath || base.ExistsFile(path)

    override this.GetLastWriteTime(path) =
        if path = projectPath then DateTime.MinValue else base.GetLastWriteTime(path)

    override this.GetModuleReader(path, readerOptions) =
        if path = projectPath then reader :> _ else base.GetModuleReader(path, readerOptions)

    interface IHideImplementation<AssemblyReaderShim>

[<SolutionComponent>]
type TestModulePathProvider(shim: TestAssemblyReaderShim) =
    inherit ModulePathProvider()

    override this.GetModulePath(psiModule) =
        if psiModule == shim.PsiModule then
            shim.Path
        else
            base.GetModulePath(psiModule)

    interface IHideImplementation<ModulePathProvider>

[<FSharpTest>]
type AssemblyReaderTest() =
    inherit TestWithTwoProjectsBase(FSharpProjectFileType.FsExtension, CSharpProjectFileType.CS_EXTENSION)

    override this.RelativeTestDataPath = "common/assemblyReaderShim"

     // todo: test InternalsVisibleTo

    [<Test>] member x.``Event 01``() = x.DoNamedTest()

    [<Test>] member x.``Field 01``() = x.DoNamedTest()
    [<Test>] member x.``Field 02 - Inherit``() = x.DoNamedTest()

    [<Test>] member x.``Method - Ctor 01``() = x.DoNamedTest()
    [<Test>] member x.``Method - Ctor 02 - Param array``() = x.DoNamedTest()
    [<Test>] member x.``Method - Ctor 03 - Optional param``() = x.DoNamedTest()

    [<Test>] member x.``Property 01``() = x.DoNamedTest()
    [<Test>] member x.``Property 02 - Accessibility``() = x.DoNamedTest()

    [<Test>] member x.``Type def - Class 01``() = x.DoNamedTest()
    [<Test>] member x.``Type def - Class 02 - Nested``() = x.DoNamedTest()
    [<Test>] member x.``Type def - Class 03 - Abstract``() = x.DoNamedTest()
    [<Test>] member x.``Type def - Class 04 - Interface impl``() = x.DoNamedTest()

    [<Test>] member x.``Type def - Delegate 01``() = x.DoNamedTest()

    [<Test>] member x.``Type def - Enum 01``() = x.DoNamedTest()
    [<Test>] member x.``Type def - Enum 02``() = x.DoNamedTest()

    [<Test>] member x.``Type def - Interface 01``() = x.DoNamedTest()
    [<Test>] member x.``Type def - Interface 02 - Super``() = x.DoNamedTest() // todo: members, type parameters

    [<Test>] member x.``Type def - Namespace 01``() = x.DoNamedTest()

    [<Test>] member x.``Type parameter 01``() = x.DoNamedTest()

    override this.DoTest(project: IProject, _: IProject) =
        let solution = this.Solution
        let manager = HighlightingSettingsManager.Instance

        solution.GetPsiServices().Files.CommitAllDocuments()

        this.ExecuteWithGold(fun writer ->
            let projectFile = project.GetAllProjectFiles() |> Seq.exactlyOne
            let sourceFile = projectFile.ToSourceFiles().Single()

            let stages =
                DaemonStageManager.GetInstance(solution).Stages
                |> Seq.filter (fun stage -> stage :? TypeCheckErrorsStage)
                |> List.ofSeq

            let daemon = TestHighlightingDumper(sourceFile, writer, stages, fun highlighting sourceFile settingsStore ->
                let severity = manager.GetSeverity(highlighting, sourceFile, solution, settingsStore)
                severity = Severity.WARNING || severity = Severity.ERROR)

            daemon.DoHighlighting(DaemonProcessKind.VISIBLE_DOCUMENT)
            daemon.Dump()) |> ignore

    override this.DoTest(lifetime: Lifetime, project: IProject) =
        let path = this.SecondProject.Location / (this.SecondProjectName + ".dll")
        let psiModule = this.SecondProject.GetPsiModules().SingleItem().NotNull()
        use assemblyReaderCookie = FSharpExperimentalFeatureCookie.Create(ExperimentalFeature.AssemblyReaderShim)
        use cookie = this.Solution.GetComponent<TestAssemblyReaderShim>().CreateProjectCookie(path, psiModule)

        base.DoTest(lifetime, project)
