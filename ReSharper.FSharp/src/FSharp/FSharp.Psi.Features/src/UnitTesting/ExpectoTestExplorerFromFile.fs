namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.UnitTesting

open System.Collections.Generic
open System.Linq
open JetBrains.Application.Parts
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.CSharp.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.UnitTestFramework.Elements
open JetBrains.ReSharper.UnitTestFramework.Exploration
open JetBrains.ReSharper.UnitTestFramework.Exploration.Daemon
open JetBrains.ReSharper.UnitTestFramework.Persistence
open JetBrains.Util

type private ExpectoTestCollector(observer: IUnitTestElementObserverOnFile, discoveredElements: ExpectoTestElement[],
        namedTests: IReadOnlyList<ExpectoNamedTest>) =
    let elementFactory = ExpectoTestElementFactory()
    let sourceFilePath = observer.PsiSourceFile.GetLocation().FullPath

    let getMostExactMatchesOfDiscoveredElements () =
        let namedTestsByName = namedTests.ToLookup(fun namedTest -> Array.last namedTest.Path.Names)
        let namedTestsWithComputedName = namedTestsByName[ExpectoTestPath.ComputedName]

        [| for element in discoveredElements do
             let path = element.Path
             let namedTestsWithSameName = namedTestsByName[Array.last path]
             let candidates = Seq.append namedTestsWithSameName namedTestsWithComputedName
             let matches = ExpectoNamedTests.getMostExactMatches path element.IsTestContainer candidates
             element, matches |]

    let reportElementsDisposition (navigationRange: DocumentRange) (containingRange: DocumentRange)
            (elements: IUnitTestElement[]) (isKnownFromSource: bool) =
        let navigationRange = navigationRange.TextRange
        let containingRange = containingRange.TextRange

        match elements with
        | [||] -> ()
        | [| element |] when isKnownFromSource ->
            observer.OnUnitTestElementDisposition(element, navigationRange, containingRange)
        | _ ->

        let fakeElement = { new IFakeUnitTestElement with member _.GetRelatedUnitTestElements() = elements }
        observer.OnUnitTestElementDisposition(fakeElement, navigationRange, containingRange)

    let getRootTypeName (root: ITypeMemberDeclaration) =
        let typeMember = root.DeclaredElement
        let containingType = if isNull typeMember then null else typeMember.GetContainingType()
        if isNull containingType then null else containingType.GetClrName().FullName

    let createAndReportElement (namedTest: ExpectoNamedTest) =
        let path = namedTest.Path
        let typeName = getRootTypeName path.Root
        let element = elementFactory.GetOrCreate(path.Names, typeName, namedTest.IsGroup, sourceFilePath, 0)

        observer.OnUnitTestElement(element)
        element :> IUnitTestElement

    let getRootRange (root: ITypeMemberDeclaration) =
        match root with
        | :? IMultipleDeclarationMember as declarationMember -> declarationMember.MultipleDeclaration.GetDocumentRange()
        | _ -> root.GetDocumentRange()

    let reportRootsAndContainingTypes (elementsByRoot: OneToListMap<ITypeMemberDeclaration, IUnitTestElement>) =
        let elementsByContainingType = OneToListMap<ITypeDeclaration, IUnitTestElement>()

        for KeyValue(root, elements) in elementsByRoot do
            let elements = Array.ofSeq elements
            reportElementsDisposition (root.GetNameDocumentRange()) (getRootRange root) elements true

            let containingTypeDeclarations = root.ContainingNodes<ITypeDeclaration>().ToEnumerable()
            for typeDecl in containingTypeDeclarations do
                elementsByContainingType.AddValueRange(typeDecl, elements)

        for KeyValue(typeDecl, elements) in elementsByContainingType do
            let nameRange = typeDecl.GetNameDocumentRange()
            if not nameRange.IsEmpty then
                reportElementsDisposition nameRange (typeDecl.GetDocumentRange()) (Array.ofSeq elements) true

    member this.HasDiscoveredTestNotFoundInSource =
        let matchesOfDiscoveredElements = getMostExactMatchesOfDiscoveredElements ()
        matchesOfDiscoveredElements |> Array.exists (fun (element, matches) ->
            not element.IsTestContainer && matches.Length = 0)

    member this.ReportNamedTests() =
        let elementsByNameNode = OneToListMap<ITreeNode, IUnitTestElement>()
        let elementsByRoot = OneToListMap<ITypeMemberDeclaration, IUnitTestElement>()
        let elementsKnownFromSource = HashSet<IUnitTestElement>()
        let pathsKnownFromSource = HashSet<string[]>(HashIdentity.Structural)

        let namedTestsKnownFromSource =
            namedTests
            |> Seq.filter _.Path.IsKnownFromSource
            |> Seq.distinctBy (fun namedTest -> namedTest.NameNode, namedTest.Path)
            |> Seq.sortBy _.Path.Names.Length
            |> Array.ofSeq

        for namedTest in namedTestsKnownFromSource do
            let path = namedTest.Path
            let element = createAndReportElement namedTest
            elementsKnownFromSource.Add(element) |> ignore
            pathsKnownFromSource.Add(path.Names) |> ignore
            elementsByNameNode.Add(namedTest.NameNode, element)

            if path.Names.Length = 1 then
                elementsByRoot.Add(path.Root, element)

        let matchesOfDiscoveredElements = getMostExactMatchesOfDiscoveredElements ()
        for element, matches in matchesOfDiscoveredElements do
            if not (pathsKnownFromSource.Contains(element.Path)) then
                for namedTest in matches do
                    elementsByNameNode.Add(namedTest.NameNode, element)

        let testNodesByNameNode = Dictionary<ITreeNode, ITreeNode>()
        for namedTest in namedTests do
            testNodesByNameNode[namedTest.NameNode] <- namedTest.TestNode

        let nameNodes = elementsByNameNode.Keys |> Seq.sortBy _.GetTreeStartOffset().Offset |> Array.ofSeq
        for nameNode in nameNodes do
            let elements = elementsByNameNode[nameNode].Distinct().ToArray()
            let isKnownFromSource = elements |> Array.forall elementsKnownFromSource.Contains
            let testRange = testNodesByNameNode[nameNode].GetDocumentRange()
            reportElementsDisposition (nameNode.GetDocumentRange()) testRange elements isKnownFromSource

        reportRootsAndContainingTypes elementsByRoot


[<SolutionComponent(Instantiation.DemandAnyThreadSafe)>]
type ExpectoTestExplorerFromFile(provider: ExpectoTestProvider, repository: IUnitTestElementRepository) =
    interface IUnitTestExplorerFromFile with
        member this.Provider = provider

        member this.ProcessFile(psiFile, observer, interrupted) =
            let source = observer.Source
            let elementsOfFile = repository.Query(source.ToCriterion()) |> Seq.cast<ExpectoTestElement>
            let elementsOfProject =
                repository.GetBy(source.Provider, source.Project, source.TargetFrameworkId)
                |> Seq.cast<ExpectoTestElement>

            let elementsWithNoSourceFile =
                elementsOfProject |> Seq.filter (fun element -> isNull element.SourceProjectFile)

            let discoveredElements =
                Seq.append elementsOfFile elementsWithNoSourceFile
                |> Seq.distinct
                |> Seq.filter (fun element -> not (element.Origin.HasFlag(UnitTestElementOrigin.Source)))
                |> Array.ofSeq

            let walker = ExpectoTestDeclarationWalker(psiFile, interrupted, followsReferencesToOtherFiles = false)
            let collector = ExpectoTestCollector(observer, discoveredElements, walker.NamedTests)

            walker.WalkRoots()

            if collector.HasDiscoveredTestNotFoundInSource then
                walker.WalkNonRootDeclarations()

            collector.ReportNamedTests()
