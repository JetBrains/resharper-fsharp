namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.UnitTesting

open JetBrains.Application.Parts
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Expecto.Tasks
open JetBrains.ReSharper.TestRunner.Abstractions
open JetBrains.ReSharper.TestRunner.Abstractions.Objects
open JetBrains.ReSharper.UnitTestFramework.Execution.TestRunner
open JetBrains.ReSharper.UnitTestFramework.Execution.TestRunner.DataCollection
open JetBrains.ReSharper.UnitTestFramework.Execution.TestRunner.Extensions
open JetBrains.ReSharper.UnitTestFramework.Exploration.Artifacts
open JetBrains.Util

[<SolutionComponent(Instantiation.DemandAnyThreadSafe)>]
type ExpectoTestRunnerAdapter() =
    static let tasksAssembly = typeof<ExpectoTestTask>.Assembly

    interface ITestRunnerAdapter with
        member this.InProcessAdapterAssembly = tasksAssembly
        member this.Priority = 10

        member this.GetTestAdapterLoader(_) =
            let adapterPath = tasksAssembly.GetDirectory() / "JetBrains.ReSharper.Plugins.FSharp.Expecto.Adapter.dll"
            let runnerTypeName = "JetBrains.ReSharper.Plugins.FSharp.Expecto.Adapter.ExpectoRunner"
            let runner = TypeInfo(runnerTypeName, adapterPath.FullPath)
            TestAdapterInfo(runner, runner, AdditionalAssemblies = [| tasksAssembly.GetPath().FullPath |])

        member this.GetTestContainer(ctx) =
            TestContainer(ctx.GetOutputPath().FullPath, ctx.Settings.TestRunner.ToShadowCopy())

        member this.GetMessageHandlers(_) = Seq.empty<IMessageHandlerMarker>


[<SolutionComponent(Instantiation.DemandAnyThreadSafe)>]
type ExpectoTestRunnerRunStrategy(dataCollectorFactory: IDataCollectorFactory, agentManager: ITestRunnerAgentManager,
        adapter: ExpectoTestRunnerAdapter, artifactResolver: IUnitTestProjectArtifactResolver) =
    inherit TestRunnerRunStrategy(dataCollectorFactory, agentManager, adapter, artifactResolver)


[<SolutionComponent(Instantiation.DemandAnyThreadUnsafe)>]
type ExpectoElementMapping() =
    static let elementFactoryKey = Key<ExpectoTestElementFactory>("ExpectoTestElementFactory")

    interface IUnitTestElementToRemoteTaskMapping<ExpectoTestElement> with
        member this.GetRemoteTask(element, _) =
            ExpectoTestTask.ToClient(element.Path, element.IsTestContainer)

    interface IRemoteTaskToUnitTestElementMapping<ExpectoTestTask> with
        member this.GetElement(task, ctx, _) =
            let elementFactory = ctx.GetOrCreateDataUnderLock(elementFactoryKey, fun () -> ExpectoTestElementFactory())
            elementFactory.GetOrCreate(task.Path, task.TypeName, false, task.SourceFilePath, task.Line)
