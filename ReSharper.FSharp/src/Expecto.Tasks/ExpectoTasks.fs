module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.UnitTesting.ExpectoTasks

open System
open System.Xml
open JetBrains.ReSharper.TaskRunnerFramework

let [<Literal>] ExpectoId = "Expecto"
let [<Literal>] AssemblyPathId = "PathId"
let [<Literal>] TestId = "TestId"
let [<Literal>] TestNameId = "TestNameId"
let [<Literal>] TestUniqueId = "TestUniqueId"
let [<Literal>] TestParentUniqueId = "TestParentUniqueId"

[<AllowNullLiteral; Serializable>]
type ExpectoAssemblyTask =
    inherit RemoteTask

    val Path: string

    new (path: string) =
        { inherit RemoteTask(ExpectoId)
          Path = path }

    new (xmlElement: XmlElement) =
        { inherit RemoteTask(xmlElement)
          Path = RemoteTask.GetXmlAttribute(xmlElement, AssemblyPathId) }

    override x.SaveXml(xmlElement) =
        base.SaveXml(xmlElement)
        RemoteTask.SetXmlAttribute(xmlElement, AssemblyPathId, x.Path)

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
        { inherit RemoteTask(ExpectoId)
          TestId = elementId }

    new (xmlElement: XmlElement) =
        { inherit RemoteTask(xmlElement)
          TestId = RemoteTask.GetXmlAttribute(xmlElement, TestId) }

    override x.SaveXml(xmlElement) =
        base.SaveXml(xmlElement)
        RemoteTask.SetXmlAttribute(xmlElement, TestId, x.TestId)

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
        { inherit RemoteTask(ExpectoId)
          Name = name
          UniqueId = id
          ParentUniqueId = parentId }

    new (xmlElement: XmlElement) =
        { inherit RemoteTask(xmlElement)
          Name = RemoteTask.GetXmlAttribute(xmlElement, TestNameId)
          UniqueId = RemoteTask.GetXmlAttribute(xmlElement, TestUniqueId) |> int
          ParentUniqueId = RemoteTask.GetXmlAttribute(xmlElement, TestParentUniqueId) |> int }

    override x.SaveXml(xmlElement) =
        base.SaveXml(xmlElement)
        RemoteTask.SetXmlAttribute(xmlElement, TestNameId, x.Name)
        RemoteTask.SetXmlAttribute(xmlElement, TestUniqueId, x.UniqueId.ToString())
        RemoteTask.SetXmlAttribute(xmlElement, TestParentUniqueId, x.ParentUniqueId.ToString())

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
