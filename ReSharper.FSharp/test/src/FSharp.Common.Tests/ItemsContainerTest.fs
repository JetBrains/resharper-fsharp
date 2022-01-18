module rec JetBrains.ReSharper.Plugins.FSharp.Tests.Common.ItemsContainerTest

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Text.RegularExpressions
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.Platform.MsBuildHost.Models
open JetBrains.ProjectModel
open JetBrains.ProjectModel.ProjectsHost
open JetBrains.ProjectModel.ProjectsHost.Impl
open JetBrains.ProjectModel.ProjectsHost.MsBuild
open JetBrains.ProjectModel.ProjectsHost.MsBuild.Internal
open JetBrains.ProjectModel.ProjectsHost.MsBuild.Structure
open JetBrains.ProjectModel.Update
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Host.ProjectItems.ItemsContainer
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.TestFramework
open JetBrains.Util
open JetBrains.Util.Logging
open Moq

type Assert = NUnit.Framework.Assert
type TestAttribute = NUnit.Framework.TestAttribute
type ExplicitAttribute = NUnit.Framework.ExplicitAttribute
type TestFixtureAttribute = NUnit.Framework.TestFixtureAttribute

let projectDirectory = VirtualFileSystemPath.Parse(@"C:\Solution\Project", InteractionContext.SolutionContext)
let solutionMark = SolutionMarkFactory.Create(projectDirectory.Combine("Solution.sln"))
let projectMark = DummyProjectMark(solutionMark, "Project", Guid.Empty, projectDirectory.Combine("Project.fsproj"))

let projectPath (relativePath: string) = projectDirectory / relativePath


let (|NormalizedPath|) (path: VirtualFileSystemPath) =
    path.MakeRelativeTo(projectDirectory).NormalizeSeparators(FileSystemPathEx.SeparatorStyle.Unix)

let (|AbsolutePath|) (path: VirtualFileSystemPath) =
    path.MakeAbsoluteBasedOn(projectDirectory)

let removeIdentities path =
    Regex.Replace(path, @"\[\d+\]", String.Empty)


let createContainer items writer =
    let container = LoggingFSharpItemsContainer(writer, createRefresher writer)
    let rdItems =
        items |> List.map (fun { ItemType = itemType; EvaluatedInclude = evaluatedInclude; Link = link } ->
            let evaluatedInclude = removeIdentities evaluatedInclude
            let metadata =
                match link with
                | null -> []
                | _ -> [RdProjectMetadata("Link", link, false)]
            RdProjectItem(itemType, evaluatedInclude, String.Empty, Nullable(false), RdThisProjectItemOrigin(), metadata.ToList(id)))

    let rdProject = RdProject(List(), rdItems.ToList(id), List(), List(), List(), List(), List())
    let rdProjectDescription =
        RdProjectDescription(projectDirectory.FullPath, projectMark.Location.FullPath, null, List(), List(), List())
    let msBuildProject = MsBuildProject(projectMark, Dictionary(), [rdProject].ToList(id), rdProjectDescription)
    let projectProperties = FSharpProjectPropertiesFactory.CreateProjectProperties(List())
    let projectDescriptor = ProjectDescriptor.CreateByProjectName(Guid.NewGuid(), projectProperties, null, projectMark.Name)

    (container :> IFSharpItemsContainer).OnProjectLoaded(projectMark, msBuildProject, projectDescriptor)
    container


let createRefresher (writer: TextWriter) =
    { new IFSharpItemsContainerRefresher with
        member x.RefreshProject(_, initial) =
            if not initial then writer.WriteLine("Refresh whole project")

        member x.RefreshFolder(_, NormalizedPath path, id) =
            writer.WriteLine(sprintf "Refresh %s[%O]" path id)

        member x.UpdateFile(_, NormalizedPath path) =
            writer.WriteLine(sprintf "Update view %s" path)

        member x.UpdateFolder(_, NormalizedPath path, id) =
            writer.WriteLine(sprintf "Update view %s[%O]" path id)

        member x.ReloadProject(projectMark) =
            writer.WriteLine(sprintf "Reload project %s" projectMark.Name)

        member x.SelectItem(_, _) = () }


let createViewFile path (solutionItems: IDictionary<VirtualFileSystemPath, IProjectItem>) =
    FSharpViewFile(getOrCreateFile path solutionItems)

let createViewFolder path id solutionItems =
    FSharpViewFolder (getOrCreateFolder path solutionItems, { Identity = id })

let getOrCreateFile (AbsolutePath path) solutionItems: IProjectFile =
    solutionItems.GetOrCreateValue(path, fun () ->

    let file = Mock<IProjectFile>()
    file.Setup(fun x -> x.Name).Returns(path.Name) |> ignore
    file.Setup(fun x -> x.Location).Returns(path) |> ignore
    file.Setup(fun x -> x.ParentFolder).Returns(getOrCreateFolder path.Parent solutionItems) |> ignore
    file.Setup(fun x -> x.GetProject()).Returns(fun _ ->
        getOrCreateFolder projectDirectory solutionItems :?> _) |> ignore
    file.Object :> IProjectItem) :?> _


let getOrCreateFolder (AbsolutePath path) solutionItems: IProjectFolder =
    solutionItems.GetOrCreateValue(path, fun () ->

    let solution = Mock<ISolution>()
    solution.Setup(fun x -> x.FindProjectItemsByLocation(It.IsAny()))
        .Returns(fun path -> [solutionItems.TryGetValue(path)].AsCollection()) |> ignore

    let folder = Mock<IProjectFolder>()
    folder.Setup(fun x -> x.Name).Returns(path.Name) |> ignore
    folder.Setup(fun x -> x.Location).Returns(path) |> ignore
    folder.Setup(fun x -> x.ParentFolder).Returns(fun _ -> getOrCreateFolder path.Parent solutionItems) |> ignore
    folder.Setup(fun x -> x.GetSolution()).Returns(solution.Object) |> ignore
    folder.Setup(fun x -> x.GetProject()).Returns(fun _ ->
        getOrCreateFolder projectDirectory solutionItems :?> _) |> ignore

    if path.Equals(projectDirectory) then
        let project = folder.As<IProject>()
        project.Setup(fun x -> x.GetData(ProjectsHostExtensions.ProjectMarkKey)).Returns(projectMark) |> ignore

    folder.Object :> IProjectItem) :?> _


let createViewItems solutionItems (item: AnItem) : seq<FSharpViewItem> = seq {
    let components = item.EvaluatedInclude.Split('/')

    let mutable path = projectDirectory
    for itemComponent in Seq.take (components.Length - 1) components do
        let matched = Regex.Match(itemComponent, @"(?<name>\w+)\[(?<identity>\d+)\]")
        Assert.IsTrue(matched.Success)

        let name = matched.Groups.["name"].Value
        let id = Int32.Parse(matched.Groups.["identity"].Value)
        path <- path.Combine(name)

        yield createViewFolder path id solutionItems

    path <- path.Combine(Array.last components |> removeIdentities)
    yield
        match item.ItemType with
        | Folder -> createViewFolder path 1 solutionItems
        | _ -> createViewFile path solutionItems }


let createItem itemType evaluatedInclude =
    { ItemType = itemType; EvaluatedInclude = evaluatedInclude; Link = null }

let link link item =
    { item with Link = link }


[<TestFixture>]
type FSharpItemsContainerTest() =
    inherit BaseTestNoShell()

    let eq a b = a = b

    override x.RelativeTestDataPath = "common/itemsContainer"

    [<Test>]
    member x.``Initialization 01``() =
        x.DoContainerInitializationTest(
            [ createItem "Compile" "File1"
              createItem "Compile" "Folder[1]/SubFolder[1]/File1"
              createItem "Compile" "Folder[1]/SubFolder[1]/File2"
              createItem "Compile" "Folder[1]/OtherSubFolder[1]/Data[1]/File3"
              createItem "Compile" "Folder[1]/File4"
              createItem "Compile" "File5"
              createItem "Compile" "Folder[2]/SubFolder[2]/File6"
              createItem "Compile" "Folder[2]/SubFolder[2]/File7"
              createItem "Compile" "File8" ])

    [<Test>]
    member x.``Initialization 02 - CompileBefore``() =
        x.DoContainerInitializationTest(
            [ createItem "Compile"       "File2"
              createItem "CompileBefore" "File1" ])

    [<Test>]
    member x.``Initialization 03 - CompileBefore folder``() =
        x.DoContainerInitializationTest(
            [ createItem "Compile"       "Folder[1]/File2"
              createItem "Compile"       "File3"
              createItem "CompileBefore" "Folder[1]/File1"
              createItem "Compile"       "File4" ])

    [<Test>]
    member x.``Initialization 04 - CompileBefore, CompileAfter``() =
        x.DoContainerInitializationTest(
            [ createItem "Compile"       "File3"
              createItem "CompileAfter"  "File4"
              createItem "CompileBefore" "File1"
              createItem "CompileAfter"  "File5"
              createItem "CompileBefore" "File2" ])

    [<Test>]
    member x.``Initialization 05 - CompileBefore, CompileAfter, folders``() =
        x.DoContainerInitializationTest(
            [ createItem "Compile"       "Folder[1]/File3"
              createItem "CompileAfter"  "Folder[1]/File4"
              createItem "CompileBefore" "File1"
              createItem "CompileAfter"  "File5"
              createItem "CompileBefore" "Folder[1]/File2" ])

    [<Test>]
    member x.``Initialization 06 - CompileBefore, folders``() =
        x.DoContainerInitializationTest(
            [ createItem "Compile"       "File3"
              createItem "Compile"       "Folder[2]/File4"
              createItem "Compile"       "File5"
              createItem "CompileBefore" "File1"
              createItem "CompileBefore" "Folder[1]/File2"
              createItem "CompileAfter"  "Folder[3]/File6"
              createItem "CompileAfter"  "File7" ])

    [<Test>]
    member x.``Initialization 07 - Linked files``() =
        x.DoContainerInitializationTest(
            [ createItem "Compile"      "File1"
              createItem "Compile"      "..\\ExternalFolder\\File2"
              createItem "Compile"      "..\\ExternalFolder\\File3" |> link "File3"
              createItem "CompileAfter" "..\\ExternalFolder\\File4" |> link "LinkFolder\\File4"
              createItem "Compile"      "..\\ExternalFolder\\File5" |> link "LinkFolder\\File5" ])

    [<Test>]
    member x.``Initialization 08 - Empty folders``() =
        x.DoContainerInitializationTest(
            [ createItem "Compile" "File1"
              createItem "Folder"  "Empty1[1]"
              createItem "Compile" "File2"
              createItem "Compile" "Folder[1]/File3"
              createItem "Folder"  "Folder[1]/Empty2[1]"
              createItem "Compile" "Folder[1]/File4"
              createItem "Compile" "File5"
              createItem "Folder"  "Folder[2]/Empty3[1]" ])

    [<Test>]
    member x.``Add file 01 - Empty project``() =
        x.DoContainerModificationTest(([]: string list),
            fun container _ ->
                container.OnAddFile("Compile", "File1", null, None))

    [<Test>]
    member x.``Add file 02 - No relative``() =
        x.DoContainerModificationTest(([]: string list),
            fun container _ ->
                container.OnAddFile("Compile", "Folder/Subfolder/File1", null, None))

    [<Test>]
    member x.``Add file 03 - Split folders top level``() =
        x.DoAddFileRelativeToTests(
            [ "Folder[1]/File1"
              "Folder[1]/File2" ],
            "File3",
            "Folder/File2",
            "Folder/File1")

    [<Test>]
    member x.``Add file 04 - Split nested folders``() =
        x.DoAddFileRelativeToTests(
            [ "Folder[1]/SubFolder[1]/File1"
              "Folder[1]/SubFolder[1]/File2" ],
            "Folder/File3",
            "Folder/SubFolder/File2",
            "Folder/SubFolder/File1")

    [<Test>]
    member x.``Add file 05 - Split nested folders, add folders``() =
        x.DoAddFileRelativeToTests(
            [ "Folder[1]/SubFolder[1]/File1"
              "Folder[1]/SubFolder[1]/File2"
              "Folder[1]/Another[1]/SubFolder[1]/File3"
              "Folder[1]/File4" ],
            "Folder/Another/SubFolder/File5",
            "Folder/SubFolder/File2",
            "Folder/SubFolder/File1")

    [<Test>]
    member x.``Add file 06 - Add relative folders``() =
        x.DoAddFileRelativeToTests(
            [ "Folder[1]/SubFolder[1]/File1"],
            "Folder/Another/File2",
            null,
            "Folder/SubFolder/File1")

    [<Test>]
    member x.``Add file 07 - Top level``() =
        x.DoAddFileRelativeToTests(
            [ "File1"
              "File2"
              "File3"
              "File4"
              "File5" ],
            "File6",
            "File4",
            "File3")

    [<Test>]
    member x.``Add file 08 - Top level, add folders``() =
        x.DoAddFileRelativeToTests(
            [ "File1"
              "File2"
              "File3"
              "File4"
              "File5" ],
            "Folder/File6",
            "File4",
            "File3")

    [<Test>]
    member x.``Add file 09 - No relative, refresh``() =
        x.DoContainerModificationTest(
            [ "File1"
              "Folder[1]/File2"],
            fun container _ ->
                container.OnAddFile("Compile", "Folder/Subfolder/File1", null, None))

    [<Test>]
    member x.``Add file 10 - No relative, refresh nested``() =
        x.DoContainerModificationTest(
            [ "File1"
              "Folder[1]/SubFolder[1]/File2" ],
            fun container _ ->
                container.OnAddFile("Compile", "Folder/Subfolder/Another/File1", null, None))

    [<Test>]
    member x.``Add file 11 - Before first file in folder``() =
        x.DoAddFileRelativeBeforeTest(
            [ "File1"
              "File2"
              "Folder[1]/File3"
              "File4" ],
            "File5",
            "Folder/File3")

    [<Test>]
    member x.``Add file 12 - Before first file in nested folder``() =
        x.DoAddFileRelativeBeforeTest(
            [ "File1"
              "File2"
              "Folder[1]/Subfolder[1]/File3"
              "File4" ],
            "File5",
            "Folder/Subfolder/File3")

    [<Test>]
    member x.``Add file 13 - Before first file in nested folders``() =
        x.DoAddFileRelativeBeforeTest(
            [ "File1"
              "File2"
              "Folder[1]/File3"
              "Folder[1]/Subfolder[1]/File4"
              "Folder[1]/File5"
              "File6" ],
            "Folder/File7",
            "Folder/Subfolder/File4")

    [<Test>]
    member x.``Add file 14 - Before first file in nested folders, different parent``() =
        x.DoAddFileRelativeBeforeTest(
            [ "File1"
              "File2"
              "Folder[1]/File3"
              "Folder[1]/Subfolder[1]/File4"
              "Folder[1]/File5"
              "File6" ],
            "Folder/Another/File7",
            "Folder/Subfolder/File4")

    [<Test>]
    member x.``Add file 15 - Before first file in nested folder, different parent``() =
        x.DoAddFileRelativeBeforeTest(
            [ "File1"
              "File2"
              "Folder[1]/File3"
              "Folder[1]/Subfolder[1]/File4"
              "Folder[1]/File5"
              "File6" ],
            "Another/Subfolder/File7",
            "Folder/Subfolder/File4")

    [<Test>]
    member x.``Add file 16 - Split nested folders``() =
        x.DoAddFileRelativeToTests(
            [ "Folder[1]/SubFolder[1]/File1"
              "Folder[1]/SubFolder[1]/File2" ],
            "File3",
            "Folder/SubFolder/File2",
            "Folder/SubFolder/File1")

    [<Test>]
    member x.``Add file 17 - Split nested folders``() =
        x.DoAddFileRelativeToTests(
            [ "Folder[1]/File1"
              "Folder[1]/File2"
              "Folder[1]/SubFolder[1]/File3"
              "Folder[1]/File4" ],
            "File5",
            "Folder/SubFolder/File3",
            "Folder/File2")

    [<Test>]
    member x.``Add file 18 - Split nested folders``() =
        x.DoAddFileRelativeToTests(
            [ "File1"
              "Folder[1]/File2"
              "Folder[1]/File3"
              "Folder[1]/SubFolder[1]/File4"
              "Folder[1]/File5"
              "File6" ],
            "File7",
            "Folder/SubFolder/File4",
            "Folder/File3")

    [<Test>]
    member x.``Add file 19 - After last file in folder``() =
        x.DoAddFileRelativeAfterTest(
            [ "File1"
              "File2"
              "Folder[1]/File3"
              "File4" ],
            "File5",
            "Folder/File3")

    [<Test>]
    member x.``Add file 20 - After last file in folder``() =
        x.DoAddFileRelativeAfterTest(
            [ "File1"
              "File2"
              "Folder[1]/Subfolder[1]/File3"
              "File4" ],
            "File5",
            "Folder/Subfolder/File3")

    [<Test>]
    member x.``Add file 21 - After last file in nested folder``() =
        x.DoAddFileRelativeAfterTest(
            [ "File1"
              "File2"
              "Folder[1]/File3"
              "Folder[1]/Subfolder[1]/File4"
              "Folder[1]/File5"
              "File6" ],
            "Folder/File7",
            "Folder/Subfolder/File4")

    [<Test>]
    member x.``Add file 22 - After last file in nested folder, different parent``() =
        x.DoAddFileRelativeAfterTest(
            [ "File1"
              "File2"
              "Folder[1]/File3"
              "Folder[1]/Subfolder[1]/File4"
              "Folder[1]/File5"
              "File6" ],
            "Folder/Another/File7",
            "Folder/Subfolder/File4")

    [<Test>]
    member x.``Add file 23 - After last file in nested folder, different parent``() =
        x.DoAddFileRelativeAfterTest(
            [ "File1"
              "File2"
              "Folder[1]/File3"
              "Folder[1]/Subfolder[1]/File4"
              "Folder[1]/File5"
              "File6" ],
            "Another/Subfolder/File7",
            "Folder/Subfolder/File4")

    [<Test>]
    member x.``Add file 24 - After last file before parent folder``() =
        x.DoAddFileRelativeAfterTest(
            [ "File1"
              "Folder[1]/File2" ],
            "Folder/File3",
            "File1")

    [<Test>]
    member x.``Add file 25 - After last file before nested parent folders``() =
        x.DoAddFileRelativeAfterTest(
            [ "File1"
              "Folder[1]/Sub[1]/File2" ],
            "Folder/File3",
            "File1")

    [<Test>]
    member x.``Add file 26 - Before first file after parent folder``() =
        x.DoAddFileRelativeBeforeTest(
            [ "Folder[1]/File1"
              "File2" ],
            "Folder/File3",
            "File2")

    [<Test>]
    member x.``Add file 27 - Before first file after nested parent folders``() =
        x.DoAddFileRelativeBeforeTest(
            [ "Folder[1]/Sub[1]/File1"
              "File2" ],
            "Folder/File3",
            "File2")

    [<Test>]
    member x.``Add file 28 - Before first file after nested parent folders``() =
        x.DoAddFileRelativeBeforeTest(
            [ "Folder[1]/Sub[1]/File1"
              "File2" ],
            "Folder/Sub/File3",
            "File2")

    [<Test>]
    member x.``Add file 29 - After last file before parent folder``() =
        x.DoAddFileRelativeAfterTest(
            [ "File1"
              "Folder[1]/Sub[1]/File2" ],
            "Folder/Sub/File3",
            "File1")

    [<Test>]
    member x.``Add file 30 - After last file before nested parent folder, different parent``() =
        x.DoAddFileRelativeAfterTest(
            [ "File1"
              "Folder[1]/Sub[1]/File2" ],
            "Folder/Another/File3",
            "File1")

    [<Test>]
    member x.``Add file 31 - Before first file after nested parent folder, different parent``() =
        x.DoAddFileRelativeBeforeTest(
            [ "Folder[1]/Sub[1]/File1"
              "File2" ],
            "Folder/Another/File3",
            "File2")

    [<Test>]
    member x.``Remove file 01 - Top level``() =
        x.DoContainerModificationTest(
            [ "File1"
              "Folder[1]/SubFolder[1]/Subfolder[1]/File1"
              "Folder[1]/File4" ],
            fun container _ ->
                container.OnRemoveFile("Compile", "File1"))

    [<Test>]
    member x.``Remove file 02 - Remove file in subfolder``() =
        x.DoContainerModificationTest(
            [ "File1"
              "Folder[1]/SubFolder[1]/File1"
              "Folder[1]/File4" ],
            fun container _ ->
                container.OnRemoveFile("Compile", "Folder/Subfolder/File1"))

    [<Test>]
    member x.``Remove file 03 - Remove file in nested subfolder``() =
        x.DoContainerModificationTest(
            [ "File1"
              "Folder[1]/SubFolder[1]/Subfolder[1]/File1"
              "Folder[1]/File4" ],
            fun container _ ->
                container.OnRemoveFile("Compile", "Folder/Subfolder/Subfolder/File1"))

    [<Test>]
    member x.``Remove file 04 - Remove empty splitted folder``() =
        x.DoContainerModificationTest(
            [ "File1"
              "Folder[1]/File2"
              "File3"
              "Folder[2]/File4" ],
            fun container _ ->
                container.OnRemoveFile("Compile", "Folder/File2"))

    [<Test>]
    member x.``Remove file 05 - Remove empty splitted folder``() =
        x.DoContainerModificationTest(
            [ "File1"
              "Folder[1]/File2"
              "File3"
              "Folder[2]/File4" ],
            fun container _ ->
                container.OnRemoveFile("Compile", "Folder/File4"))

    [<Test>]
    member x.``Remove file 06 - Join splitted folders``() =
        x.DoContainerModificationTest(
            [ "File1"
              "Folder[1]/Subfolder[1]/File1"
              "Folder[1]/File2"
              "Folder[1]/Subfolder[2]/File3"
              "Folder[1]/File4" ],
            fun container _ ->
                container.OnRemoveFile("Compile", "Folder/File2"))

    [<Test>]
    member x.``Remove file 07 - Remove nested empty splitted folder``() =
        x.DoContainerModificationTest(
            [ "File1"
              "Folder[1]/SubFolder[1]/File2"
              "File3"
              "Folder[2]/SubFolder[2]/File4" ],
            fun container _ ->
                container.OnRemoveFile("Compile", "Folder/SubFolder/File4"))

    [<Test>]
    member x.``Remove file 08 - Remove empty splitted folder``() =
        x.DoContainerModificationTest(
            [ "File1"
              "Folder[1]/File2"
              "File3"
              "Folder[2]/File4" ],
            fun container _ ->
                container.OnRemoveFile("Compile", "Folder/File2"))

    [<Test>]
    member x.``Remove file 09 - Remove nested empty splitted folder``() =
        x.DoContainerModificationTest(
            [ "File1"
              "Folder[1]/SubFolder[1]/File2"
              "File3"
              "Folder[2]/SubFolder[2]/File4" ],
            fun container _ ->
                container.OnRemoveFile("Compile", "Folder/SubFolder/File2"))

    [<Test>]
    member x.``Remove file 10 - Remove splitted folder and join relative splitted``() =
        x.DoContainerModificationTest(
            [ "File1"
              "Folder[1]/SubFolder[1]/File2"
              "Another[1]/File3"
              "Folder[2]/SubFolder[2]/File4"
              "Another[2]/File5" ],
            fun container _ ->
                container.OnRemoveFile("Compile", "Another/File3"))

    [<Test>]
    member x.``Modification 01 - Move file``() =
        x.DoContainerModificationTest(
            [ "File1"
              "Folder[1]/SubFolder[1]/File1"
              "Folder[1]/File4" ],
            fun container writer ->
                writer.WriteLine("Move 'Folder/File4' after 'File1':")
                container.OnRemoveFile("Compile", "Folder/File4")
                container.OnAddFile("Compile", "File4", "File1", Some RelativeToType.After))

    [<Test>]
    member x.``Create modification context 01 - No modification``() =
        x.DoCreateModificationContextTest(
            [ createItem "Compile" "File1"
              createItem "Compile" "File2"
              createItem "Compile" "File3" ])

    [<Test>]
    member x.``Create modification context 02 - Single file folder``() =
        x.DoCreateModificationContextTest(
            [ createItem "Compile" "File1"
              createItem "Compile" "Folder[1]/File2"
              createItem "Compile" "File3" ])

    [<Test>]
    member x.``Create modification context 03 - Multiple files folder``() =
        x.DoCreateModificationContextTest(
            [ createItem "Compile" "File1"
              createItem "Compile" "Folder[1]/File2"
              createItem "Compile" "Folder[1]/File3"
              createItem "Compile" "Folder[1]/File4"
              createItem "Compile" "File3" ])

    [<Test>]
    member x.``Create modification context 04 - Nested folders``() =
        x.DoCreateModificationContextTest(
            [ createItem "Compile" "File1"
              createItem "Compile" "Folder[1]/SubFolder[1]/File2"
              createItem "Compile" "Folder[1]/SubFolder[1]/File3"
              createItem "Compile" "Folder[1]/SubFolder[1]/File4"
              createItem "Compile" "File3" ])

    [<Test>]
    member x.``Create modification context 05 - Multiple nested folders``() =
        x.DoCreateModificationContextTest(
            [ createItem "Compile" "File1"
              createItem "Compile" "Folder[1]/SubFolder[1]/File2"
              createItem "Compile" "Folder[1]/SubFolder[1]/File3"
              createItem "Compile" "Folder[1]/SubFolder[1]/File4"
              createItem "Compile" "Folder[1]/File5"
              createItem "Compile" "Folder[1]/SubFolder[2]/File6"
              createItem "Compile" "File7" ])

    [<Test; Explicit("Not implemented")>]
    member x.``Create modification context 06 - CompileBefore``() =
        x.DoCreateModificationContextTest(
            [ createItem "Compile"       "Folder[1]/File3"
              createItem "CompileAfter"  "File5"
              createItem "CompileAfter"  "File6"
              createItem "CompileBefore" "File1"
              createItem "Compile"       "Folder[1]/File4"
              createItem "CompileBefore" "File2" ])

    [<Test>]
    member x.``Update 01 - Rename files``() =
        x.DoContainerModificationTest(
            [ "Folder[1]/File1"
              "File2"
              "File3"
              "Folder[1]/File4" ],
            fun container _ ->
                container.OnUpdateFile("Compile", "File2", "Compile", "NewName1")
                container.OnUpdateFile("Compile", "File3", "Compile", "NewName2"))

    [<Test>]
    member x.``Update 02 - Rename files in folder``() =
        x.DoContainerModificationTest( // todo: change to separate tests
            [ "File1"
              "Folder[1]/File2"
              "Folder[1]/File3"
              "Folder[1]/File4"
              "File5" ],
            fun container _ ->
                container.OnUpdateFile("Compile", "Folder/File2", "Compile", "Folder/NewName1")
                container.OnUpdateFile("Compile", "Folder/File3", "Compile", "Folder/NewName2")
                container.OnUpdateFile("Compile", "Folder/File4", "Compile", "Folder/NewName3"))

    [<Test>]
    member x.``Update 03 - Rename folder``() =
        x.DoContainerModificationTest(
            [ "File1"
              "Folder[1]/SubFolder[1]/File2"
              "Folder[1]/File3"
              "Folder[1]/SubFolder[2]/File4"
              "File5" ],
            fun container _ ->
                container.OnUpdateFolder("Folder", "NewName"))

    [<Test>]
    member x.``Update 04 - Rename splitted folder``() =
        x.DoContainerModificationTest(
            [ "File1"
              "Folder[1]/File2"
              "Folder[1]/SubFolder[1]/File3"
              "Folder[1]/File4"
              "File5"
              "Folder[2]/File6"
              "Folder[2]/SubFolder/File7"
              "Folder[2]/File8" ],
            fun container _ ->
                container.OnUpdateFolder("Folder", "NewName"))

    [<Test>]
    member x.``Update 05 - Rename nested splitted folder``() =
        x.DoContainerModificationTest(
            [ "Folder[1]/SubFolder[1]/File1"
              "File2"
              "Folder[2]/SubFolder[2]/File3" ],
            fun container _ ->
                container.OnUpdateFolder("Folder/SubFolder", "Folder/NewName"))

    [<Test>]
    member x.``Update 06 - Rename nested splitted folder and splitted folder``() =
        x.DoContainerModificationTests(
            [ "Folder[1]/SubFolder[1]/File1"
              "Folder[1]/File2"
              "File3"
              "Folder[2]/SubFolder[2]/File4" ],
            [ fun (container: LoggingFSharpItemsContainer) (writer: TextWriter) ->
                  writer.WriteLine("Rename 'Folder' to 'NewName'")
                  container.OnUpdateFolder("Folder", "NewName")
              fun container writer ->
                  writer.WriteLine("Rename 'NewName/SubFolder' to 'NewName/SubFolderNewName'")
                  container.OnUpdateFolder("NewName/SubFolder", "NewName/SubFolderNewName") ])

    [<Test>]
    member x.``Update 07 - Rename splitted folder and nested splitted folder``() =
        x.DoContainerModificationTests(
            [ "Folder[1]/SubFolder[1]/File1"
              "Folder[1]/File2"
              "File3"
              "Folder[2]/SubFolder[2]/File4" ],
            [ fun (container: LoggingFSharpItemsContainer) (writer: TextWriter) ->
                  writer.WriteLine("Rename 'Folder/SubFolder' to 'Folder/SubFolderNewName'")
                  container.OnUpdateFolder("Folder/SubFolder", "Folder/SubFolderNewName")
              fun container writer ->
                  writer.WriteLine("Rename 'Folder' to 'NewName'")
                  container.OnUpdateFolder("Folder", "NewName") ])

    [<Test>]
    member x.``Update 08 - Change item types, reload project``() =
        x.DoContainerModificationTest(
            [ createItem "CompileAfter"  "File1"
              createItem "Compile"       "File2"
              createItem "CompileBefore" "File3"
              createItem "CompileAfter"  "File4" ],
            (fun container writer ->
                 container.OnUpdateFile("Compile", "File2", "CompileBefore", "File2")
                 writer.WriteLine()
                 container.OnUpdateFile("CompileAfter", "File1", "Resource", "File1")
                 writer.WriteLine()
                 container.OnUpdateFile("CompileBefore", "File3", "CompileAfter", "File3")
                 writer.WriteLine()
                 container.OnUpdateFile("CompileAfter", "File4", "CompileBefore", "File4")),
            false)

    [<Test>]
    member x.``Update 09 - Change item types, no project reload``() =
        x.DoContainerModificationTest(
            [ createItem "Compile" "File1"
              createItem "Resource" "File2" ],
            fun container writer ->
                 container.OnUpdateFile("Compile", "File1", "Resource", "File1")
                 writer.WriteLine()
                 container.OnUpdateFile("Resource", "File2", "Compile", "File2"))

    [<Test>]
    member x.``Update 10 - Change linked item type``() =
        x.DoContainerModificationTest(
            [ createItem "Compile" "..\\ExternalFolder\\File1" |> link "File1" ],
            fun container _ ->
                let (UnixSeparators path) = VirtualFileSystemPath.Parse("..\\ExternalFolder\\File1", InteractionContext.SolutionContext)
                container.OnUpdateFile("Compile", path, "Resource", path))


    member x.DoCreateModificationContextTest(items: AnItem list) =
        let relativeToTypes = [ RelativeToType.Before; RelativeToType.After ]
        x.ExecuteWithGold(fun writer ->
            let container = createContainer items writer
            let provider = FSharpItemModificationContextProvider(container)
            container.Dump(writer)
            writer.WriteLine()

            let itemTypes =
                items |> List.map (fun item ->
                    // todo: linked
                    let path = VirtualFileSystemPath.Parse(removeIdentities item.EvaluatedInclude, InteractionContext.SolutionContext)
                    path.MakeAbsoluteBasedOn(projectDirectory), item.ItemType)
                |> dict

            let viewItems = HashSet(Seq.collect (Dictionary() |> createViewItems) items)
            let viewFiles = viewItems |> Seq.filter (function | FSharpViewFile _ -> true | _ -> false) |> List.ofSeq
            for viewFile in viewFiles do
                for relativeViewItem in viewItems do
                    for relativeToType in relativeToTypes do
                        let projectItem = viewFile.ProjectItem
                        let relative = relativeViewItem.ProjectItem
                        let itemType = itemTypes.TryGetValue(projectItem.Location)
                        let relativeItemType = itemTypes.TryGetValue(relative.Location)

                        let context = provider.CreateModificationContext(Some viewFile, relativeViewItem, relativeToType)
                        match relativeViewItem, context with
                        | FSharpViewFile _, Some context when
                                projectItem <> relative && isNotNull itemType &&
                                equalsIgnoreCase itemType relativeItemType ->

                            let contextRelativeTo = context.RelativeTo.NotNull()
                            Assertion.Assert(eq relative.Location contextRelativeTo.ReferenceItem.Location,
                                sprintf "%O <> %O" relative contextRelativeTo.ReferenceItem.Location)

                            Assertion.Assert(eq relativeToType contextRelativeTo.Type,
                                sprintf "%O <> %O, %O, %O " relativeToType contextRelativeTo.Type
                                        relative.Location contextRelativeTo.ReferenceItem.Location)
                        | _ ->
                            let (NormalizedPath path) = viewFile.Location
                            writer.Write(path)
                            writer.Write(sprintf " %O %O -> " relativeToType relativeViewItem)
                            writer.WriteLine(
                                match context with
                                | Some context ->
                                    let (NormalizedPath path) = context.RelativeTo.NotNull().ReferenceItem.Location
                                    sprintf "%O %s" context.RelativeTo.Type path
                                | _ -> "null")
                writer.WriteLine()) |> ignore


    member x.DoAddFileRelativeBeforeTest(items: string list, filePath, relativeBefore) =
        x.DoAddFileRelativeToTests(items, filePath, relativeBefore, null)

    member x.DoAddFileRelativeAfterTest(items: string list, filePath, relativeAfter) =
        x.DoAddFileRelativeToTests(items, filePath, null, relativeAfter)

    member x.DoAddFileRelativeToTests(items: string list, filePath, relativeBefore, relativeAfter) =
        x.ExecuteWithGold(fun writer ->
            let mutable addBeforeDump: string = null
            let mutable addAfterDump: string = null

            if isNotNull relativeBefore then
              addBeforeDump <- x.DoAddFileImpl(items, filePath, relativeBefore, RelativeToType.Before, writer, true)

            if isNotNull relativeAfter then
              addAfterDump <- x.DoAddFileImpl(items, filePath, relativeAfter, RelativeToType.After, writer, isNull relativeBefore )

            if (isNotNull addBeforeDump && isNotNull addAfterDump) then
              writer.WriteLine(sprintf "Dumps are equal: %O" (addBeforeDump.Equals(addAfterDump, StringComparison.Ordinal))))
        |> ignore

    member x.DoAddFileImpl(items, filePath, relativeTo, relativeToType, writer: TextWriter, shouldDumpInitial) =
        let container = createContainer (items |> List.map (createItem "Compile")) writer
        if shouldDumpInitial then
            container.Dump(writer)
            writer.WriteLine()

        writer.WriteLine("=======")
        container.OnAddFile("Compile", filePath, relativeTo, Some relativeToType)

        let stringWriter = new StringWriter()
        container.Dump(stringWriter)
        writer.WriteLine()
        writer.WriteLine(stringWriter.ToString())

        stringWriter.ToString()

    member x.DoContainerModificationTest(items: string list, action: LoggingFSharpItemsContainer -> TextWriter -> unit, ?dump) =
        x.DoContainerModificationTest(items |> List.map (createItem "Compile"), action, ?dump = dump)

    member x.DoContainerModificationTests(items: string list, actions: (LoggingFSharpItemsContainer -> TextWriter -> unit) list, ?dump) =
        x.DoContainerModificationTests(items |> List.map (createItem "Compile"), actions, ?dump = dump)

    member x.DoContainerModificationTest(items: AnItem list, action: LoggingFSharpItemsContainer -> TextWriter -> unit, ?dump) =
        x.DoContainerModificationTests(items, [action], ?dump = dump)

    member x.DoContainerModificationTests(items: AnItem list, actions: (LoggingFSharpItemsContainer -> TextWriter -> unit) list, ?dump) =
        let dump = defaultArg dump true
        x.ExecuteWithGold(fun writer ->
            let container = createContainer items writer
            container.Dump(writer)

            writer.WriteLine()
            writer.WriteLine("=======")

            for action in actions do
                action container writer
                writer.WriteLine()
                if dump then
                    container.Dump(writer)
                    writer.WriteLine()) |> ignore

    member x.DoContainerInitializationTest(items: AnItem list) =
        x.ExecuteWithGold(fun writer ->
            let container = createContainer items writer
            let solutionItems = Dictionary()

            writer.WriteLine()
            writer.WriteLine("=== Container Dump ===")
            container.Dump(writer)

            let dumpStructure items solutionItems =
                writer.WriteLine()
                writer.WriteLine("=== Structure API ===")

                for item in items do
                    let mutable identString = ""
                    let ident () =
                        identString <- identString + "  "

                    for viewItem in createViewItems solutionItems item do
                        let name = viewItem.ProjectItem.Name
                        let sortKey = container.TryGetSortKey(viewItem)
                        writer.Write(sprintf "%s%s SortKey=%O" identString name (Option.get sortKey))
                        match viewItem with
                        | FSharpViewFolder _ ->
                            writer.WriteLine()
                            ident ()
                        | FSharpViewFile _ ->
                            container.TryGetParentFolderIdentity(viewItem)
                            |> sprintf " ParentFolderIdentity=%O"
                            |> writer.WriteLine

            let dumpParentFolders items solutionItems =
                writer.WriteLine()
                writer.WriteLine("=== Parent Folders API ===")

                let emptyFolders =
                    items |> List.choose (fun item ->
                        match item.ItemType with
                        | Folder -> Some (VirtualFileSystemPath.Parse(removeIdentities item.EvaluatedInclude, InteractionContext.SolutionContext))
                        | _ -> None)

                let folders =
                    items |> Seq.collect (fun item ->
                        VirtualFileSystemPath.Parse(removeIdentities item.EvaluatedInclude, InteractionContext.SolutionContext).GetParentDirectories())
                    |> Seq.append emptyFolders
                    |> HashSet

                for path in folders.OrderBy(fun x -> x.FullPath) do
                    writer.WriteLine(path)
                    for folder, parent in container.CreateFoldersWithParents(getOrCreateFolder path solutionItems) do
                        writer.WriteLine(sprintf "  %O -> %O" folder parent)

            dumpStructure items solutionItems
            dumpParentFolders items solutionItems) |> ignore


[<Struct>]
type AnItem =
    { ItemType: string
      EvaluatedInclude: string
      Link: string }

    static member Create(itemType, evaluatedInclude, ?link) =
        { ItemType = itemType; EvaluatedInclude = evaluatedInclude; Link = defaultArg link null }


let itemFilterProvider =
    let defaultBuildActions =
        [| yield! MsBuildCommonBuildActionsProvider().DefaultBuildActions
           yield BuildActions.compileBefore
           yield BuildActions.compileAfter |]
        |> HashSet

    { new IItemTypeFilterProvider with
        member x.CreateItemFilter(_, _) = MsBuildItemTypeFilter(defaultBuildActions) }


type LoggingFSharpItemsContainer(writer, refresher) as this =
    inherit FSharpItemsContainer(Lifetime.Eternal, DummyLogger.Instance, DummyFSharpItemsContainerLoader.Instance,
        refresher, itemFilterProvider)

    let container = this :> IFSharpItemsContainer

    member x.OnAddFile(itemType, location, relativeTo, relativeToType: RelativeToType option) =
        let output, relativeToPath =
            match relativeTo with
            | null -> "", null
            | relativeTo -> sprintf " %O '%O'" (relativeToType.NotNull().Value) relativeTo, projectPath relativeTo
        writer.Write(sprintf "Add '%O'" location)
        writer.WriteLine(output)
        let path = projectPath location
        container.OnAddFile(projectMark, itemType, path, path, relativeToPath, Option.toNullable relativeToType)

    member x.OnRemoveFile(itemType, location) =
        writer.WriteLine(sprintf "Remove '%O'" location)
        container.OnRemoveFile(projectMark, itemType, projectPath location)

    member x.OnUpdateFile(oldItemType, oldLocation, newItemType, newLocation) =
        writer.WriteLine(sprintf "Update file: '%O' (%O) -> '%O' (%O)" oldLocation oldItemType newLocation newItemType)
        container.OnUpdateFile(projectMark, oldItemType, projectPath oldLocation, newItemType, projectPath newLocation)

    member x.OnUpdateFolder(oldLocation, newLocation) =
        writer.WriteLine(sprintf "Update folder: '%O' -> '%O'" oldLocation newLocation)
        container.OnUpdateFolder(projectMark, projectPath oldLocation, projectPath newLocation)

    member x.Dump(writer) = container.Dump(writer)
    member x.TryGetSortKey(viewItem) = container.TryGetSortKey(viewItem)
    member x.TryGetParentFolderIdentity(viewItem) = container.TryGetParentFolderIdentity(viewItem)
    member x.CreateFoldersWithParents(folder) = container.CreateFoldersWithParents(folder)


type DummyFSharpItemsContainerLoader() =
    inherit FSharpItemsContainerLoader(Lifetime.Eternal, null, null)

    override x.GetMap() = Dictionary<IProjectMark, ProjectMapping>() :> _
    static member val Instance = DummyFSharpItemsContainerLoader()
