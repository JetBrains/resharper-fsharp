module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.UnitTesting.ExpectoRunner

open System
open System.IO
open System.Xml
open JetBrains.ReSharper.TaskRunnerFramework

let [<Literal>] expectoId = "Expecto"

[<AllowNullLiteral>]
type TestRunner() =
    inherit MarshalByRefObject()

    member x.Run(server: IRemoteTaskServer, configuration: TaskExecutorConfiguration, logger: SimpleLogger,
                 assemblyTaskNode: TaskExecutionNode,  shadowCopyCookie: IShadowCopyCookie) =
        ()

and [<Serializable>]
    ExpectoAssemblyTask(path: string) =
    inherit RemoteTask(expectoId)

    static let [<Literal>] attributePathId = "Path"
    
    new (xmlElement: XmlElement) =
        let path = RemoteTask.GetXmlAttribute(xmlElement, attributePathId)
        ExpectoAssemblyTask(path)

    member x.Path = path

    override x.SaveXml(xmlElement) =
        base.SaveXml(xmlElement)
        RemoteTask.SetXmlAttribute(xmlElement, attributePathId, path)

    override x.IsMeaningfulTask = false

    override x.Equals (other: obj) =
        match other with
        | :? ExpectoAssemblyTask as task -> x.Equals(task)
        | _ -> false

    override x.Equals(other: RemoteTask) =
        match other with
        | :? ExpectoAssemblyTask as task -> task.Path = path
        | _ -> false

    override x.GetHashCode() = path.GetHashCode()

and [<Serializable>]
    ExpectTestElementTask(elementId: string) =
    inherit RemoteTask(expectoId)

    static let [<Literal>] attributeElementId = "ElementId"
    
    new (xmlElement: XmlElement) =
        let path = RemoteTask.GetXmlAttribute(xmlElement, attributeElementId)
        ExpectTestElementTask(path)

    override x.SaveXml(xmlElement) =
        base.SaveXml(xmlElement)
        RemoteTask.SetXmlAttribute(xmlElement, attributeElementId, elementId)

    member x.TestId = elementId
    
    override x.IsMeaningfulTask = true
    override x.Equals(other: RemoteTask) =
        match other with
        | :? ExpectTestElementTask as task -> task.TestId = elementId
        | _ -> false

    override x.Equals (other: obj) =
        match other with
        | :? ExpectTestElementTask as task -> x.Equals(task)
        | _ -> false

    override x.GetHashCode() = elementId.GetHashCode()

and ExpectoTaskRunner(server) =
    inherit RecursiveRemoteTaskRunner(server)

    let mutable runner = null

    override x.ExecuteRecursive(node: TaskExecutionNode) =
        let assemblyTask = node.RemoteTask :?> ExpectoAssemblyTask
        let assemblyLocation = TaskExecutor.Configuration.GetAssemblyLocation(assemblyTask.Path)
        let assemblyCodeBase = Path.GetDirectoryName(assemblyLocation)
        let assemblyConfig = Path.GetFullPath(assemblyLocation) + ".config"

        use our = new AssemblyLoader()
        our.RegisterAssemblyOf<AssemblyLoader>()
        our.RegisterAssemblyOf<TestRunner>()
        
        use appConfig = AppConfig.Change(assemblyConfig)
        use shadowCopyCookie = ShadowCopy.SetupFor(assemblyCodeBase)
        use domainCookie = AppDomainBuilder.Setup(assemblyLocation, assemblyConfig, shadowCopyCookie)
        use assemblyLoader = domainCookie.CreateAndUnwrapFrom<AssemblyLoader>()
        assemblyLoader.RegisterAssemblyOf<AssemblyLoader>()
        assemblyLoader.RegisterAssemblyOf<TestRunner>()
          
          //ES: There is a difference in how Mono and .NET resolves assemblies.
          //    Whenever one loads an assembly using Assembly.LoadFrom method,
          //    in true .NET its dependencies will also be looked for in the folder
          //    one loaded it from. Mono, however, does not check that folder.
        assemblyLoader.RegisterPathOf<TestRunner>()
          
          //ES: There are yet undetermined cases when test runner classes or
          //    classes from a test assembly may leak into our domain. Registering
          //    test runner root and test assembly root helps us resolving them. 
        our.RegisterPathOf<TestRunner>()
        our.RegisterPath(shadowCopyCookie.TargetLocation)

        runner <- domainCookie.CreateAndUnwrapFrom<TestRunner>()
        runner.Run(x.Server, TaskExecutor.Configuration, TaskExecutor.Logger, node, shadowCopyCookie)

        ()

    static member val RunnerInfo =
        let location = typeof<RemoteTask>.Assembly.Location
        let dir = Path.GetDirectoryName(location)
        RemoteTaskRunnerInfo(expectoId, typeof<ExpectoTaskRunner>, [| dir |])

