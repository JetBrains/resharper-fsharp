[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.UnitTesting.Expecto.Tasks

open System
open System.Xml
open JetBrains.ReSharper.TaskRunnerFramework

let [<Literal>] expectoId = "Expecto"
let [<Literal>] assemblyPathId = "PathId"
let [<Literal>] testId = "TestId"

[<AllowNullLiteral; Serializable>]
type ExpectoAssemblyTask =
    inherit RemoteTask

    val Path: string

    new (path: string) =
        { inherit RemoteTask(expectoId)
          Path = path }

    new (xmlElement: XmlElement) =
        { inherit RemoteTask(xmlElement)
          Path = RemoteTask.GetXmlAttribute(xmlElement, assemblyPathId) }

    override x.SaveXml(xmlElement) =
        base.SaveXml(xmlElement)
        RemoteTask.SetXmlAttribute(xmlElement, assemblyPathId, x.Path)

    override x.IsMeaningfulTask = false

    override x.Equals(other: obj) =
        match other with
        | :? ExpectoAssemblyTask as task -> x.Equals(task)
        | _ -> false

    override x.Equals(other: RemoteTask) =
        match other with
        | :? ExpectoAssemblyTask as task -> task.Path = x.Path
        | _ -> false

    override x.GetHashCode() = x.Path.GetHashCode()


[<AllowNullLiteral; Serializable>]
type ExpectoTestsTask =
    inherit RemoteTask

    val TestId: string

    new (elementId: string) =
        { inherit RemoteTask(expectoId)
          TestId = elementId }

    new (xmlElement: XmlElement) =
        { inherit RemoteTask(xmlElement)
          TestId = RemoteTask.GetXmlAttribute(xmlElement, testId) }

    override x.SaveXml(xmlElement) =
        base.SaveXml(xmlElement)
        RemoteTask.SetXmlAttribute(xmlElement, testId, x.TestId)

    override x.IsMeaningfulTask = true

    override x.Equals(other: obj) =
        match other with
        | :? ExpectoTestsTask as task -> x.Equals(task)
        | _ -> false

    override x.Equals(other: RemoteTask) =
        match other with
        | :? ExpectoTestsTask as task -> task.TestId = x.TestId
        | _ -> false

    override x.GetHashCode() = x.TestId.GetHashCode()
