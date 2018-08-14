namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Scripts

open System
open System.IO
open JetBrains.Application.Components
open JetBrains.Application.Environment
open JetBrains.DataFlow
open JetBrains.ProjectModel
open JetBrains.ProjectModel.BuildTools
open JetBrains.ProjectModel.ProjectsHost.SolutionHost
open JetBrains.ProjectModel.ProjectsHost.SolutionHost.Impl
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.ReSharper.Psi.Modules
open JetBrains.TestFramework
open JetBrains.TestFramework.Projects
open NUnit.Framework

type ScriptPsiModulesTest() =
    inherit BaseTest()

    override x.RelativeTestDataPath = "projectModel/scripts"

    [<Test>] member x.SolutionItem() = x.DoTestSolution()
    [<Test>] member x.FileInProject() = x.DoTestSolution()
    [<Test>] member x.MultipleTargetFrameworks() = x.DoTestSolution()

    [<Test>] member x.CSharpProject() = x.DoTestSolution()
    [<Test>] member x.FileDoNotExist() = x.DoTestSolution()

    member x.DoTestSolution() =
        x.RunGuarded(fun _ -> Lifetimes.Using(x.DoTestSolutionImpl))

    member x.DoTestSolutionImpl(lifetime: Lifetime) =
        use persistCacheCookie = x.ShellInstance.GetComponent<TestCachesConfigurationSettings>().PersistCachesCookie()

        let solution = x.OpenSolution(lifetime)
        x.ExecuteWithGold(fun writer ->
            let scriptModulesProvider = solution.GetComponent<FSharpScriptPsiModulesProvider>()
            scriptModulesProvider.Dump(writer)
            x.DumpSourceFilePersistentIds(solution, writer))

    member x.OpenSolution(lifetime: Lifetime): ISolution =
        let tempSolutionPath = x.CopyTestDataDirectoryToTemp2(lifetime, x.TestMethodName)
        let solution = x.SolutionManager.OpenSolution(tempSolutionPath / x.SolutionFileName)
        lifetime.AddAction2(fun _ -> x.SolutionManager.CloseSolution())
        solution

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
type TestSolutionToolset(lifetime: Lifetime, buildToolContainer: BuildToolContainer) =

    let buildTool = buildToolContainer.GetAutoDetected(BuildToolEnvironment.EmptyEnvironment)
    let changed = new SimpleSignal(lifetime, "MySolutionToolset::Changed")

    interface ISolutionToolset with
        member x.CurrentBuildTool = buildTool
        member x.Changed = changed :> _

    interface  IHideImplementation<DefaultSolutionToolsetStub>


[<ZoneActivator>]
type SolutionHostZoneActivator() =
    interface IActivate<IHostSolutionZone> with
        member x.ActivatorEnabled() = true
