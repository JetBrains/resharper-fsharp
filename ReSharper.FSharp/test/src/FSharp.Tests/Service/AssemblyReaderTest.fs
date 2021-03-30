namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open System
open JetBrains.Application.Components
open JetBrains.Application.Settings
open JetBrains.Application.changes
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.FeaturesTestFramework.Daemon
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader
open JetBrains.ReSharper.Plugins.FSharp.Shim.FileSystem
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.TestFramework
open JetBrains.Util
open NUnit.Framework

[<SolutionComponent>]
type TestAssemblyReaderShim(lifetime: Lifetime, changeManager: ChangeManager, psiModules: IPsiModules,
        cache: FcsModuleReaderCommonCache, assemblyInfoShim: AssemblyInfoShim, settingsStore: ISettingsStore) =
    inherit AssemblyReaderShim(lifetime, changeManager, psiModules, cache, assemblyInfoShim, settingsStore)

    let mutable projectPath = FileSystemPath.Empty
    let mutable projectPsiModule = null
    let mutable reader = Unchecked.defaultof<_>

    member this.PsiModule = projectPsiModule
    member this.Path = projectPath

    member this.CreateProjectCookie(path: FileSystemPath, psiModule: IPsiModule) =
        projectPath <- path
        projectPsiModule <- psiModule
        reader <- new ProjectFcsModuleReader(projectPsiModule, cache)

        { new IDisposable with
            member x.Dispose() =
                projectPath <- FileSystemPath.Empty
                projectPsiModule <- null
                reader <- Unchecked.defaultof<_> }

    override this.Exists(path) =
        path = projectPath || base.Exists(path)

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

[<FSharpTest; TestSetting(typeof<FSharpOptions>, "NonFSharpProjectInMemoryAnalysis", "true")>]
type AssemblyReaderTest() =
    inherit TestWithTwoProjectsBase(FSharpProjectFileType.FsExtension, CSharpProjectFileType.CS_EXTENSION)

    override this.RelativeTestDataPath = "common/assemblyReaderShim"

    [<Test>] member x.``Type def - Class 01``() = x.DoNamedTest() // todo: test InternalsVisibleTo
    [<Test>] member x.``Type def - Enum 01``() = x.DoNamedTest() // todo: wrong Class ctor error 
    [<Test>] member x.``Type def - Namespace 01``() = x.DoNamedTest()

    override this.DoTest(project: IProject, _: IProject) =
        let solution = this.Solution
        let manager = HighlightingSettingsManager.Instance

        this.ShellInstance.GetComponent<FcsCheckerService>().Checker.StopBackgroundCompile()
        solution.GetPsiServices().Files.CommitAllDocuments()

        this.ExecuteWithGold(fun writer ->
            let projectFile = project.GetAllProjectFiles() |> Seq.exactlyOne
            let sourceFile = projectFile.ToSourceFiles().Single()

            let daemon = TestHighlightingDumper(sourceFile, writer, null, fun highlighting sourceFile settingsStore ->
                let severity = manager.GetSeverity(highlighting, sourceFile, solution, settingsStore)
                severity = Severity.WARNING || severity = Severity.ERROR)

            daemon.DoHighlighting(DaemonProcessKind.VISIBLE_DOCUMENT)
            daemon.Dump()) |> ignore

    override this.DoTest(lifetime: Lifetime, project: IProject) =
        let path = this.SecondProject.Location / (this.SecondProjectName + ".dll")
        let psiModule = this.SecondProject.GetPsiModules().SingleItem().NotNull()
        use cookie = this.Solution.GetComponent<TestAssemblyReaderShim>().CreateProjectCookie(path, psiModule)

        base.DoTest(lifetime, project)
