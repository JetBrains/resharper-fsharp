namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Common.Scripts

open System
open System.IO
open System.Linq
open JetBrains.Application
open JetBrains.Application.Components
open JetBrains.Application.Environment
open JetBrains.Application.platforms
open JetBrains.DataFlow
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.BuildTools
open JetBrains.ProjectModel.MSBuild.BuildTools
open JetBrains.ProjectModel.ProjectsHost.SolutionHost
open JetBrains.ProjectModel.ProjectsHost.SolutionHost.Impl
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Host.ProjectItems.ItemsContainer
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Host.ProjectItems.ProjectStructure
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.TestFramework
open JetBrains.TestFramework.Projects
open NUnit.Framework

type ScriptPsiModulesTest() =
    inherit BaseTest()

    override x.RelativeTestDataPath = "projectModel/scripts"

    [<Test; Explicit>] member x.SolutionItem() = x.DoTestSolution()
    [<Test; Explicit>] member x.FileInProject() = x.DoTestSolution()
    [<Test; Explicit>] member x.MultipleTargetFrameworks() = x.DoTestSolution()

    [<Test; Explicit>] member x.CSharpProject() = x.DoTestSolution()
    [<Test; Explicit>] member x.FileDoNotExist() = x.DoTestSolution()

    member x.DoTestSolution() =
        x.RunGuarded(fun _ -> Lifetime.Using(x.DoTestSolutionImpl))

    member x.DoTestSolutionImpl(lifetime: Lifetime) =
        use persistCacheCookie = x.ShellInstance.GetComponent<TestCachesConfigurationSettings>().PersistCachesCookie()

        let solution = x.OpenSolution(lifetime)
        x.ExecuteWithGold(fun writer ->
            let scriptModulesProvider = solution.GetComponent<FSharpScriptPsiModulesProvider>()
            scriptModulesProvider.Dump(writer)
            x.DumpSourceFilePersistentIds(solution, writer))
        x.SolutionManager.CloseSolution()

    member x.OpenSolution(lifetime: Lifetime): ISolution =
        let tempSolutionPath = x.CopyTestDataDirectoryToTemp2(lifetime, x.TestMethodName).ToVirtualFileSystemPath()
        x.SolutionManager.OpenSolution(tempSolutionPath / x.SolutionFileName)

    member x.DumpSourceFilePersistentIds(solution: ISolution, writer: TextWriter) =
        writer.WriteLine()
        writer.WriteLine("Source files persistent ids:")

        let psiModules = solution.PsiModules()
        for project in solution.GetTopLevelProjects() do
            for projectFile in project.GetAllProjectFiles() do
                if not (projectFile.LanguageType.Is<FSharpScriptProjectFileType>()) then () else

                writer.WriteLine(projectFile.Location.MakeRelativeTo(solution.SolutionDirectory))
                for sourceFile in psiModules.GetPsiSourceFilesFor(projectFile) do
                    writer.WriteLine("  " + sourceFile.GetPersistentID())

    member x.SolutionManager: SolutionHostManager =
        x.ShellInstance.GetComponent<SolutionHostManager>()

    member x.SolutionFileName: string =
        x.TestMethodName + ".sln"

    member x.ExecuteWithGold(action: Action<TextWriter>) =
        base.ExecuteWithGold(action) |> ignore


[<SolutionInstanceComponent>]
type MyTestSolutionToolset(lifetime: Lifetime, logger: ILogger) =
    inherit DefaultSolutionToolset(lifetime, logger)

    let changed = new Signal<_>(lifetime, "MySolutionToolset::Changed")

    let cli = DotNetCoreRuntimesDetector.DetectDotNetCoreRuntimes(InteractionContext.SolutionContext).FirstOrDefault().NotNull()
    let dotnetCoreToolset = DotNetCoreToolset(cli, cli.Sdks.FirstOrDefault().NotNull())

    let env = BuildToolEnvironment.Create(dotnetCoreToolset, null)
    let buildTool = DotNetCoreMsBuildProvider().Discover(env).FirstOrDefault().NotNull()

    interface ISolutionToolset with
        member x.GetBuildTool() = buildTool
        member x.Changed = changed :> _

[<ZoneActivator>]
type SolutionHostZoneActivator() =
    interface IActivate<IHostSolutionZone>


[<SolutionInstanceComponent>]
type FSharpProjectStructurePresenterStub() =
    interface IHideImplementation<FSharpProjectStructurePresenter>


[<SolutionInstanceComponent>]
type FSharpItemsContainerRefresherStub() =
    interface IHideImplementation<FSharpItemsContainerRefresher>

    interface  IFSharpItemsContainerRefresher with
        member x.RefreshProject(_, _) = ()
        member x.RefreshFolder(_, _, _) = ()
        member x.UpdateFile(_, _) = ()
        member x.UpdateFolder(_, _, _) = ()
        member x.ReloadProject _ = ()
        member x.SelectItem(_, _) = ()


[<SolutionFeaturePart>]
type FSharpItemModificationContextProviderStub() =
    interface IHideImplementation<FSharpItemModificationContextProvider>

[<ShellComponent>]
type FSharpFileServiceStub() =
    interface IHideImplementation<FSharpFileService>

    interface IFSharpFileService with
        member x.IsScratchFile _ = false
        member x.IsScriptLike _ = false
