namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Common.Scripts

open System
open System.IO
open System.Linq
open System.Threading.Tasks
open JetBrains.Application
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Components
open JetBrains.Application.Parts
open JetBrains.Application.platforms
open JetBrains.DataFlow
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.BuildTools
open JetBrains.ProjectModel.MSBuild.BuildTools
open JetBrains.ProjectModel.ProjectsHost.SolutionHost.Impl
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts
open JetBrains.ReSharper.Plugins.FSharp.Tests.Common
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


[<SolutionInstanceComponent(InstantiationEx.LegacyDefault)>]
[<ZoneMarker(typeof<ICommonTestFSharpPluginZone>)>]
type MyTestSolutionToolset(lifetime: Lifetime, dotNetCoreInstallationsDetector: DotNetCoreInstallationsDetector, settings, logger: ILogger, notifier: RuntimeAndToolsetChangeNotifier) =
    inherit DefaultSolutionToolset(lifetime, settings, logger, notifier)

    let changed = new Signal<_>("MySoluAtionToolset::Changed")

    let cli = dotNetCoreInstallationsDetector.DetectSdkInstallations(InteractionContext.SolutionContext).FirstOrDefault().NotNull()
    let dotnetCoreToolset = DotNetCoreToolset(cli, cli.Sdks.FirstOrDefault().NotNull())

    let env = BuildToolEnvironment.Create(dotnetCoreToolset, null)
    let buildTool = DotNetCoreMsBuildProvider().Discover(env).FirstOrDefault().NotNull()

    interface ISolutionToolset with
        member x.GetRuntimeAndToolset() =
          let toolset = base.GetRuntimeAndToolset()
          RuntimeAndToolset(toolset.MonoRuntime, toolset.DotNetCoreToolset, buildTool, toolset.NonNativeX86DotNetCoreToolset, toolset.NonNativeX64DotNetCoreToolset)
          
        member x.GetRuntimeAndToolsetAsync() = ValueTask<RuntimeAndToolset>(x.GetRuntimeAndToolset())
        member x.Changed = changed :> _




[<ShellComponent>]
[<ZoneMarker(typeof<ICommonTestFSharpPluginZone>)>]
type FSharpFileServiceStub() =
    interface IFSharpFileService with
        member x.IsScratchFile _ = false
        member x.IsScriptLike _ = false
