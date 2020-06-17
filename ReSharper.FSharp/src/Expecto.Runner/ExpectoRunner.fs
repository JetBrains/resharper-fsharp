module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.UnitTesting.ExpectoRunner

open System
open System.Collections.Generic
open System.IO
open System.Reflection
open Expecto
open Expecto.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.UnitTesting.ExpectoTasks
open JetBrains.ReSharper.TaskRunnerFramework

[<AllowNullLiteral>]
type ExpectoTestRunner() =
    inherit MarshalByRefObject()

    let testsByFullName = Dictionary<string, ExpectoTestCaseTask>()

    let processTestTree (server: IRemoteTaskServer) test =
        // let flatTests = Expecto.Test.toTestCodeList test
        // let _ = flatTests

        let getId =
            let mutable nextId = 0
            fun _ ->
                let result = nextId
                nextId <- nextId + 1
                result

        // todo: support null named case

        // todo: support name duplicates:
        //   * remember parent so it's possible to replace the child
        //   * add/change parent label

        let rec loop name (fullPath: string) parentId test =
            match test with
            | Test.TestLabel(name, test, _) ->
                let fullPath =
                    if fullPath.Length = 0 then name else
                    fullPath + "/" + name
                loop name fullPath parentId test

            | Test.TestCase _ ->
                let id = getId ()
                let task = ExpectoTestCaseTask(name, id, parentId)
                server.CreateDynamicElement(task)
                testsByFullName.[fullPath] <- task

            | Test.TestList(tests, _) ->
                let id = getId ()
                server.CreateDynamicElement(ExpectoTestListTask(name, id, parentId))
                for test in tests do
                    loop null fullPath id test

            | Test.Sequenced(_, test) ->
                loop name fullPath parentId test

        loop null "" (getId ()) test

    let listToTestListOption test =
        match test with
        | [] -> None
        | [test] -> Some(test)
        | tests -> Some(TestList(tests, Normal))

    member x.Run(server: IRemoteTaskServer, assemblyTaskNode: TaskExecutionNode, task: ExpectoAssemblyTask) =
        // todo: pass proper nodes tree
        let testTask = assemblyTaskNode.Children.[0].RemoteTask :?> ExpectoTestsTask
        let testsName = testTask.TestId

        let test =
            let assemblyPath = task.Path
            let assembly = Assembly.LoadFrom(assemblyPath)

            let bindingFlags = BindingFlags.Public ||| BindingFlags.Static

            let testMembers = List<MemberInfo>()
            for exportedType in  assembly.GetExportedTypes() do
                let typeInfo = exportedType.GetTypeInfo()

                let typeMembers: MemberInfo seq =
                    seq {
                        yield! typeInfo.GetMethods(bindingFlags) |> Seq.cast
                        yield! typeInfo.GetFields(bindingFlags) |> Seq.cast
                        yield! typeInfo.GetProperties(bindingFlags) |> Seq.cast
                    }

                for m in typeMembers do
                    // todo: check method overloads
                    if m.Name = testsName then
                        testMembers.Add(m)

            testMembers
            |> Seq.choose testFromMember
            |> List.ofSeq
            |> listToTestListOption

        match test with
        | None -> ()
        | Some test ->

        processTestTree server test

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

        let beforeEach testFullName =
            let t = testsByFullName.[testFullName]
            server.TaskStarting(t)
            async.Zero()

        let info _ = async.Zero()

        let passed testFullName timeSpan =
            let t = testsByFullName.[testFullName]
            server.TaskDuration(t, timeSpan)
            server.TaskFinished(t, "", TaskResult.Success)
            async.Zero()

        let ignored testFullName message =
            let t = testsByFullName.[testFullName]
            server.TaskFinished(t, message, TaskResult.Skipped)
            async.Zero()

        let failed testFullName message timeSpan =
            let t = testsByFullName.[testFullName]
            server.TaskDuration(t, timeSpan)
            server.TaskFinished(t, message, TaskResult.Error)
            async.Zero()

        let exn testFullName (exn: Exception) timeSpan =
            let t = testsByFullName.[testFullName]
            server.TaskDuration(t, timeSpan)
            server.TaskFinished(t, exn.Message, TaskResult.Exception)
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

        let config =
            // This path requires Expecto 8.9.0+.
            let expectoAssembly = typeof<Expecto.TestsAttribute>.Assembly
            let testsModule = expectoAssembly.GetType("Expecto.Tests")
            let bindingFlags = BindingFlags.Static ||| BindingFlags.NonPublic
            let foldMethod = testsModule.GetMethod("foldCLIArgumentToConfig", bindingFlags)

            let foldArg config arg = (foldMethod.Invoke(null, [| arg |]) :?> _) config
            Seq.fold foldArg ExpectoConfig.defaultConfig cliArgs

        let returnCode = Expecto.Tests.runTests config test
        if returnCode <> 0 && not testsSuccessfullyDiscovered then
            // todo: use proper task
            server.TaskFinished(assemblyTaskNode.Children.[0].RemoteTask, "", TaskResult.Exception)


type ExpectoTaskRunner(server) =
    inherit RecursiveRemoteTaskRunner(server)

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

        let runner = domainCookie.CreateAndUnwrapFrom<ExpectoTestRunner>()
        runner.Run(x.Server, node, assemblyTask)

    static member val RunnerInfo =
        let location = typeof<RemoteTask>.Assembly.Location
        let dir = Path.GetDirectoryName(location)

        RemoteTaskRunnerInfo(ExpectoId, typeof<ExpectoTaskRunner>, [| dir |])
