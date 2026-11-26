namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.UnitTesting

open System
open JetBrains.Application
open JetBrains.Application.Parts
open JetBrains.Application.Settings
open JetBrains.Application.Settings.Implementation
open JetBrains.Metadata.Utils
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Resources
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.UnitTestFramework
open JetBrains.ReSharper.UnitTestFramework.Elements
open JetBrains.ReSharper.UnitTestFramework.Execution.Hosting
open JetBrains.ReSharper.UnitTestFramework.Execution.TestRunner
open JetBrains.ReSharper.UnitTestFramework.Exploration.Artifacts
open JetBrains.ReSharper.UnitTesting.VsTest.Provider
open JetBrains.Util
open JetBrains.Util.Dotnet.TargetFrameworkIds
open JetBrains.Util.Reflection

[<UnitTestProvider>]
type ExpectoTestProvider() =
    static let expectoAssemblyName = AssemblyNameInfoFactory.Create2("Expecto", null)

    member this.TryGetExpectoAssemblyName(project: IProject, targetFrameworkId: TargetFrameworkId): AssemblyNameInfo =
        use cookie = ReadLockCookie.Create()

        let mutable referencedAssemblyName = null
        let isReferencingExpecto =
            ReferencedAssembliesService.IsProjectReferencingAssemblyByName(project, targetFrameworkId,
                expectoAssemblyName, &referencedAssemblyName)

        if isReferencingExpecto then referencedAssemblyName else null

    member this.IsReferencingExpectoProject(project: IProject, targetFrameworkId: TargetFrameworkId) =
        use cookie = ReadLockCookie.Create()

        project.GetReferencedProjects(targetFrameworkId, transitive = false)
        |> Seq.exists (fun referencedProject ->
            let referencedTargetFrameworkId =
                targetFrameworkId.SelectTargetFrameworkIdToReference(referencedProject.TargetFrameworkIds)

            if isNull referencedTargetFrameworkId then false else

            let outputAssemblyName = referencedProject.GetOutputAssemblyName(referencedTargetFrameworkId)
            equalsIgnoreCase outputAssemblyName expectoAssemblyName.Name)

    member this.IsSupported(project: IProject, targetFrameworkId: TargetFrameworkId) =
        isNotNull (this.TryGetExpectoAssemblyName(project, targetFrameworkId))

    interface IDotNetArtifactBasedUnitTestProvider with
        member this.ID = "Expecto"
        member this.Name = "Expecto"

        member this.IsElementOfKind(_: IDeclaredElement, _: UnitTestElementKind) = false

        member this.IsElementOfKind(element: IUnitTestElement, elementKind: UnitTestElementKind) =
            match element with
            | :? ExpectoTestElement as element ->
                let kindOfElement =
                    if element.IsTestContainer then UnitTestElementKind.TestContainer else UnitTestElementKind.Test

                elementKind = kindOfElement
            | _ -> false

        member this.IsSupported(_: IHostProvider, project, targetFrameworkId) =
            this.IsSupported(project, targetFrameworkId)

        member this.IsSupported(project, targetFrameworkId) = this.IsSupported(project, targetFrameworkId)
        member this.SupportsResultEventsForParentOf(_) = false
        member this.GetRunStrategy(_, _) = UT.Facade.Get<ExpectoTestRunnerRunStrategy>()


[<SolutionComponent(Instantiation.DemandAnyThreadSafe)>]
type ExpectoTestExplorerFromArtifacts(provider: ExpectoTestProvider, agentManager: ITestRunnerAgentManager,
        adapter: ExpectoTestRunnerAdapter, installedPackageChecker: UnitTestInstalledPackageChecker) =
    inherit UnitTestExplorerFrom.TestRunner.Discoverable(provider, agentManager, adapter, installedPackageChecker,
        TimeSpan.FromMinutes(1.))

    static let minSupportedExpectoVersion = Version(9, 0)

    override this.IsSupported(project, targetFrameworkId) =
        let expectoAssemblyName = provider.TryGetExpectoAssemblyName(project, targetFrameworkId)
        let isSupportedExpecto =
            isNull expectoAssemblyName ||
            expectoAssemblyName.Version >= minSupportedExpectoVersion ||
            provider.IsReferencingExpectoProject(project, targetFrameworkId)

        if not isSupportedExpecto then PertinenceResult.Maybe(Strings.ExpectoVersionNotSupported_Text) else

        base.IsSupported(project, targetFrameworkId)

    override this.IsMicrosoftNetTestSdkRequired(_, _) = false
    override this.IsExecutableRequired(_, _) = true


[<ShellComponent(Instantiation.DemandAnyThreadSafe)>]
type ExcludeYoloDevExpectoAdapterFromVsTest(schema: ISettingsSchema, logger: ILogger) =
    inherit HaveDefaultSettings<VsTestProviderSettings>(schema, logger)

    override this.Name = Strings.ExpectoVsTestAdapterExclusion_Name

    override this.InitDefaultSettings(mountPoint) =
        this.SetIndexedValue(mountPoint, (fun (settings: VsTestProviderSettings) -> settings.ExecutorExcludeList),
            "executor://yolodev/expecto", "expecto.visualstudio.dotnetcore.testadapter.dll")
