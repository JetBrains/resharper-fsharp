module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.UnitTesting.ExpectoRunner

open System
open System.IO
open System.Reflection
open global.Expecto
open global.Expecto.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.UnitTesting.Expecto.Tasks
open JetBrains.ReSharper.TaskRunnerFramework

[<AllowNullLiteral>]
type ExpectoTestRunner() =
    inherit MarshalByRefObject()

    member x.Run(server: IRemoteTaskServer, configuration: TaskExecutorConfiguration, logger: SimpleLogger,
                 assemblyTaskNode: TaskExecutionNode,  shadowCopyCookie: IShadowCopyCookie, task: ExpectoAssemblyTask) =
        TaskExecutor.Configuration <- configuration
        TaskExecutor.Logger <- logger

        let assemblyPath = task.Path
        let assembly = Assembly.LoadFrom(assemblyPath)
        match testFromAssembly assembly with
        | None -> ()
        | Some test ->

        // Tests may fail to be discovered, e.g. due to duplicate test names.
        // We set this value in `beforeRun` and checking at test session run end. 
        let mutable testsSuccessfullyDiscovered = false

        let testSummary _ testSummary =
            for executionNode in assemblyTaskNode.Children do
                server.TaskDuration(executionNode.RemoteTask, testSummary.duration)
                server.TaskFinished(executionNode.RemoteTask, "", TaskResult.Success)

            server.TaskDuration(assemblyTaskNode.RemoteTask, testSummary.duration)
            server.TaskFinished(assemblyTaskNode.RemoteTask, "", TaskResult.Success)
            async.Zero()

        let beforeRun _ =
            testsSuccessfullyDiscovered <- true
            async.Zero()

        let beforeEach _ =
            async.Zero()

        let info _ =
            async.Zero()

        let passed _ _ =
            async.Zero()

        let ignored _ _ =
            async.Zero()

        let failed _ _ _ =
            async.Zero()

        let exn _ _ _ =
            async.Zero()

        let testPrinters =
            { beforeRun = beforeRun
              beforeEach = beforeEach
              info = info 
              summary = testSummary
              passed = passed
              ignored = ignored
              failed = failed
              exn = exn }

        let cliArgs = [ CLIArguments.Printer testPrinters ]
        let args = [| |]

        let returnCode = Expecto.Tests.runTestsWithCLIArgs cliArgs args test
        if returnCode <> 0 && not testsSuccessfullyDiscovered then
            // todo: use proper task
            server.TaskFinished(assemblyTaskNode.Children.[0].RemoteTask, "", TaskResult.Exception)


and ExpectoTaskRunner(server) =
    inherit RecursiveRemoteTaskRunner(server)

    let mutable runner = null

    override x.ExecuteRecursive(node: TaskExecutionNode) =
        let assemblyTask = node.RemoteTask :?> ExpectoAssemblyTask
        let assemblyLocation = TaskExecutor.Configuration.GetAssemblyLocation(assemblyTask.Path)
        let assemblyCodeBase = Path.GetDirectoryName(assemblyLocation)
        let assemblyConfig = Path.GetFullPath(assemblyLocation) + ".config"

        use ourAssemblyLoader = new AssemblyLoader()
        ourAssemblyLoader.RegisterAssemblyOf<AssemblyLoader>()
        ourAssemblyLoader.RegisterAssemblyOf<ExpectoTestRunner>()

        let assemblyDir = Path.GetDirectoryName(assemblyLocation)

        use appConfig = AppConfig.Change(assemblyConfig)
        use shadowCopyCookie = ShadowCopy.SetupFor(assemblyCodeBase)
        use domainCookie = AppDomainBuilder.Setup(assemblyLocation, assemblyConfig, shadowCopyCookie)
        use domainAssemblyLoader = domainCookie.CreateAndUnwrapFrom<AssemblyLoader>()

        domainAssemblyLoader.RegisterAssemblyOf<AssemblyLoader>()
        domainAssemblyLoader.RegisterAssemblyOf<ExpectoTestRunner>()
        domainAssemblyLoader.RegisterPathOf<ExpectoTestRunner>()

        ourAssemblyLoader.RegisterPathOf<ExpectoTestRunner>()
        ourAssemblyLoader.RegisterPath(shadowCopyCookie.TargetLocation)
        ourAssemblyLoader.RegisterPath(assemblyDir)
        domainAssemblyLoader.RegisterPath(assemblyDir)

        // todo: remove
        ourAssemblyLoader.RegisterPathOf<AssemblyLoader>()

        runner <- domainCookie.CreateAndUnwrapFrom<ExpectoTestRunner>()
        runner.Run(x.Server, TaskExecutor.Configuration, TaskExecutor.Logger, node, shadowCopyCookie, assemblyTask)

    static member val RunnerInfo =
        let location = typeof<RemoteTask>.Assembly.Location
        let dir = Path.GetDirectoryName(location)
        
        RemoteTaskRunnerInfo(expectoId, typeof<ExpectoTaskRunner>, [| dir |])

