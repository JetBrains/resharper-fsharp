[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.UnitTesting.Expecto.Tasks

open System
open System.Xml
open JetBrains.ReSharper.TaskRunnerFramework

let [<Literal>] expectoId = "Expecto"
let [<Literal>] assemblyPathId = "PathId"
let [<Literal>] testId = "TestId"
let [<Literal>] testNameId = "TestNameId"
let [<Literal>] testUniqueId = "TestUniqueId"
let [<Literal>] testParentUniqueId = "TestParentUniqueId"

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


[<AbstractClass; AllowNullLiteral; Serializable>]
type ExpectoTestTaskBase =
    inherit RemoteTask

    val Name: string
    val UniqueId: int
    val ParentUniqueId: int

    new (name: string, id: int, parentId: int) =
        { inherit RemoteTask(expectoId)
          Name = name
          UniqueId = id
          ParentUniqueId = parentId }

    new (xmlElement: XmlElement) =
        { inherit RemoteTask(xmlElement)
          Name = RemoteTask.GetXmlAttribute(xmlElement, testNameId)
          UniqueId = RemoteTask.GetXmlAttribute(xmlElement, testUniqueId) |> int
          ParentUniqueId = RemoteTask.GetXmlAttribute(xmlElement, testParentUniqueId) |> int }

    override x.SaveXml(xmlElement) =
        base.SaveXml(xmlElement)
        RemoteTask.SetXmlAttribute(xmlElement, testNameId, x.Name)
        RemoteTask.SetXmlAttribute(xmlElement, testUniqueId, x.UniqueId.ToString())
        RemoteTask.SetXmlAttribute(xmlElement, testParentUniqueId, x.ParentUniqueId.ToString())

    override x.IsMeaningfulTask = true

    override x.Equals(other: obj) =
        match other with
        | :? ExpectoTestTaskBase as task -> x.Equals(task)
        | _ -> false

    override x.Equals(other: RemoteTask) =
        match other with
        | :? ExpectoTestTaskBase as task -> task.UniqueId = x.UniqueId
        | _ -> false

    override x.GetHashCode() = x.UniqueId


type ExpectoTestCaseTask =
    inherit ExpectoTestTaskBase

    new (name: string, id, parentId: int) =
        { inherit ExpectoTestTaskBase(name, id, parentId) }

    new (xmlElement: XmlElement) =
        { inherit ExpectoTestTaskBase(xmlElement) }


type ExpectoTestListTask =
    inherit ExpectoTestTaskBase

    new (name: string, id, parentId) =
        { inherit ExpectoTestTaskBase(name, id, parentId) }

    new (xmlElement: XmlElement) =
        { inherit ExpectoTestTaskBase(xmlElement) }
