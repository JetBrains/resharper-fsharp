namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.UnitTesting

open System
open System.Collections.Generic
open System.Linq
open System.Text
open JetBrains.Application
open JetBrains.DocumentManagers
open JetBrains.Metadata.Reader.API
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.UnitTestFramework
open JetBrains.ReSharper.UnitTestFramework.Elements
open JetBrains.ReSharper.UnitTestFramework.Persistence
open JetBrains.Util

module private ExpectoNamedTestsCache =
    let private namedTestsOfFileKey =
        Key<CachedPsiValueWithOffsets<ExpectoNamedTest[]>>("ExpectoNamedTestsOfFile")

    let private namedTestsReachableFromRootsKey =
        Key<CachedPsiValueWithOffsets<ExpectoNamedTest[]>>("ExpectoNamedTestsReachableFromRoots")

    let private isInterrupted = Func<bool>(fun _ -> Interruption.Current.Check())

    let private collectNamedTestsOfFile (psiFile: IFile) =
        let walker = ExpectoTestDeclarationWalker(psiFile, isInterrupted, followsReferencesToOtherFiles = false)
        walker.WalkRoots()
        walker.WalkNonRootDeclarations()
        Array.ofSeq walker.NamedTests

    let private collectNamedTestsReachableFromRoots (psiFile: IFile) =
        let walker = ExpectoTestDeclarationWalker(psiFile, isInterrupted, followsReferencesToOtherFiles = true)
        walker.WalkRoots()
        Array.ofSeq walker.NamedTests

    let private getCachedNamedTests (key: Key<CachedPsiValueWithOffsets<ExpectoNamedTest[]>>)
            (collectNamedTests: IFile -> ExpectoNamedTest[]) (projectFile: IProjectFile) =
        let sourceFile = projectFile.ToSourceFile()
        let psiFile = if isNull sourceFile then null else sourceFile.GetPrimaryPsiFile()
        if isNull psiFile then [||] else

        let cachedValue = psiFile.UserData.GetOrCreateDataUnderLock(key, fun _ -> CachedPsiValueWithOffsets())
        cachedValue.GetValue(psiFile, collectNamedTests)

    let getNamedTestsOfFile = getCachedNamedTests namedTestsOfFileKey collectNamedTestsOfFile

    let getNamedTestsReachableFromRoots =
        getCachedNamedTests namedTestsReachableFromRootsKey collectNamedTestsReachableFromRoots


[<AllowNullLiteral>]
type ExpectoTestElement =
    inherit UnitTestElement

    val mutable private name: string
    val mutable private typeName: string
    val mutable private isGroup: bool
    val mutable private sourceFilePath: string
    val mutable private line: int

    new() =
        { inherit UnitTestElement()
          name = null
          typeName = null
          isGroup = false
          sourceFilePath = null
          line = 0 }

    new(path: string[], parent: ExpectoTestElement, typeName, isGroup, sourceFilePath, line) as this =
        { inherit UnitTestElement(ExpectoTestElement.GetId(path))
          name = Array.last path
          typeName = typeName
          isGroup = isGroup
          sourceFilePath = sourceFilePath
          line = line }
        then this.Parent <- parent

    static member GetId(path: string[]) =
        path |> Seq.map (fun name -> name.Replace(@"\", @"\\").Replace(".", @"\.")) |> String.concat "."

    static member GetPath(id: string) =
        let names = List<string>()
        let name = StringBuilder()

        let mutable index = 0
        while index < id.Length do
            match id[index] with
            | '\\' ->
                index <- index + 1
                name.Append(id[index]) |> ignore
            | '.' ->
                names.Add(name.ToString())
                name.Clear() |> ignore
            | c ->
                name.Append(c) |> ignore

            index <- index + 1

        names.Add(name.ToString())
        names.ToArray()

    [<Persist>]
    member this.Name with get () = this.name and set value = this.name <- value

    [<Persist; CoalesceProperty(typeof<ExpectoTestElementCoalescer>)>]
    member this.TypeName with get () = this.typeName and set value = this.typeName <- value

    [<Persist>]
    member this.IsGroup with get () = this.isGroup and set value = this.isGroup <- value

    [<Persist>]
    member this.SourceFilePath with get () = this.sourceFilePath and set value = this.sourceFilePath <- value

    [<Persist>]
    member this.Line with get () = this.line and set value = this.line <- value

    member this.IsTestContainer = this.isGroup || this.Children.Count > 0

    member this.Path: string[] =
        ExpectoTestElement.GetPath(this.NaturalId.TestId)

    member this.SourceProjectFile: IProjectFile =
        if isNull this.sourceFilePath then null else

        let location = VirtualFileSystemPath.TryParse(this.sourceFilePath, InteractionContext.SolutionContext)
        this.Project.FindProjectItemsByLocation(location).OfType<IProjectFile>().FirstOrDefault()

    member private this.ClrTypeName: IClrTypeName =
        if isNull this.typeName then null else clrTypeName this.typeName

    member private this.TypeElement: ITypeElement =
        let typeName = this.ClrTypeName
        if isNull typeName then null else

        UT.Facade.TypeCache.GetTypeElement(this.Project, this.TargetFrameworkId, typeName, false, true)

    member private this.ProjectFilesOfRootType: IProjectFile[] =
        let typeElement = this.TypeElement
        if isNull typeElement then [||] else

        [| for sourceFile in typeElement.GetSourceFiles() do
             let projectFile = sourceFile.ToProjectFile()
             if isNotNull projectFile then
                 projectFile |]

    override this.ShortName = this.name
    override this.Kind = "Expecto Test"
    override this.GetDeclaredElement() = null
    override this.GetRelatedUnitTestElements() = EmptyArray<IUnitTestElement>.Instance

    override this.GetNamespace() =
        let typeName = this.ClrTypeName
        if isNull typeName then UnitTestElementNamespace.Global else

        UnitTestElementNamespace.Create(typeName.GetNamespaceName())

    override this.GetProjectFiles() =
        let sourceProjectFile = this.SourceProjectFile
        if isNotNull sourceProjectFile then [| sourceProjectFile |] else this.ProjectFilesOfRootType

    member private this.FindNamedTestsInSource(sourceProjectFile: IProjectFile): ExpectoNamedTest[] =
        let path = this.Path
        let isTestContainer = this.IsTestContainer
        let getDistinctMostExactMatches (namedTests: seq<ExpectoNamedTest>) =
            ExpectoNamedTests.getMostExactMatches path isTestContainer namedTests |> Array.distinctBy _.NameNode

        let projectFilesOfRootType = this.ProjectFilesOfRootType
        let projectFiles = if isNull sourceProjectFile then projectFilesOfRootType else [| sourceProjectFile |]
        let namedTestsOfFiles = projectFiles |> Seq.collect ExpectoNamedTestsCache.getNamedTestsOfFile
        let matchesInFiles = getDistinctMostExactMatches namedTestsOfFiles

        let isOnlyMatchInSourceFile = isNotNull sourceProjectFile && matchesInFiles.Length = 1
        let hasAllLiteralNames = matchesInFiles |> Array.exists _.Path.HasAllLiteralNames
        if isOnlyMatchInSourceFile || hasAllLiteralNames then matchesInFiles else

        let namedTestsReachableFromRoots =
            projectFilesOfRootType |> Seq.collect ExpectoNamedTestsCache.getNamedTestsReachableFromRoots

        let matchesReachableFromRoots = getDistinctMostExactMatches namedTestsReachableFromRoots
        if matchesReachableFromRoots.Length > 0 then matchesReachableFromRoots else matchesInFiles

    static member private CreateLocation(namedTest: ExpectoNamedTest) =
        let projectFile = namedTest.NameNode.GetSourceFile().ToProjectFile()
        let navigationRange = namedTest.NameNode.GetDocumentRange().TextRange
        let containingRange = namedTest.TestNode.GetDocumentRange().TextRange
        UnitTestElementLocation(projectFile, navigationRange, containingRange)

    override this.GetLocations() =
        use cookie = ReadLockCookie.Create()

        let projectFile = this.SourceProjectFile
        let namedTests = this.FindNamedTestsInSource(projectFile)
        if namedTests.Length > 0 then Array.map ExpectoTestElement.CreateLocation namedTests :> seq<_> else

        if isNull projectFile || this.line = 0 then Seq.empty else

        let document = projectFile.GetDocument()
        let line = docLine (this.line - 1)
        if line >= document.GetLineCount() then Seq.empty else

        let lineRange = TextRange(document.GetLineStartOffset(line), document.GetLineEndOffsetNoLineBreak(line))
        let text = document.GetText(lineRange)
        let indent = text.Length - text.TrimStart().Length
        let navigationRange = TextRange(lineRange.StartOffset + indent, lineRange.EndOffset)

        Seq.singleton (UnitTestElementLocation(projectFile, navigationRange, lineRange))


and ExpectoTestElementCoalescer() =
    interface IUnitTestElementPropertyCoalescer with
        member this.Merge(target, original, source) =
            let target = target :?> ExpectoTestElement
            let original = original :?> ExpectoTestElement

            if isNull target.TypeName then
                target.TypeName <- original.TypeName

            let keepsSourceLocation =
                original.Origin.HasFlag(UnitTestElementOrigin.Source) && source.Origin <> UnitTestElementOrigin.Source

            if keepsSourceLocation || isNull target.SourceFilePath then
                target.SourceFilePath <- original.SourceFilePath
                target.Line <- original.Line

            let isSame =
                target.TypeName = original.TypeName && target.IsGroup = original.IsGroup &&
                target.SourceFilePath = original.SourceFilePath && target.Line = original.Line

            if isSame then UnitTestElementChanges.None else UnitTestElementChanges.Internal


type ExpectoTestElementFactory() =
    let elementsById = Dictionary<string, ExpectoTestElement>()

    member this.GetOrCreate(path: string[], typeName, isGroup, sourceFilePath, line): ExpectoTestElement =
        let id = ExpectoTestElement.GetId(path)

        let mutable element = null
        if elementsById.TryGetValue(id, &element) then element else

        let parentPath = Array.take (path.Length - 1) path
        let parent = if path.Length = 1 then null else this.GetOrCreate(parentPath, typeName, true, null, 0)
        let element = ExpectoTestElement(path, parent, typeName, isGroup, sourceFilePath, line)
        elementsById[id] <- element
        element
