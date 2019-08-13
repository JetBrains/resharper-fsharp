module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.UnitTesting.Expecto

open System
open JetBrains.Application.UI.BindableLinq.Collections
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.ClrLanguages
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.UnitTestFramework
open JetBrains.ReSharper.UnitTestFramework.AttributeChecker
open JetBrains.ReSharper.UnitTestFramework.Elements
open JetBrains.ReSharper.UnitTestFramework.Exploration
open JetBrains.ReSharper.UnitTestFramework.Strategy
open JetBrains.Util.Reflection

let expectoAssemblyName = AssemblyNameInfoFactory.Create("Expecto")

let testsAttribute = clrTypeName "Expecto.TestsAttribute"
let pTestsAttribute = clrTypeName "Expecto.PTestsAttribute"
let fTestsAttribute = clrTypeName "Expecto.FTestsAttribute"

let expectoTestType = clrTypeName "Expecto.Test"

let attributes =
    [| testsAttribute
       pTestsAttribute
       fTestsAttribute |]

let getReturnType (declaredElement: IDeclaredElement) =
    match declaredElement with
    | :? IMethod as method -> method.ReturnType
    | :? IProperty as property -> property.Type
    | :? IField as field -> field.Type
    | _ -> null

let hasTestReturnType (declaredElement: IDeclaredElement) =
    match getReturnType declaredElement with
    | null -> false
    | returnType ->

    match returnType.GetTypeElement() with
    | null -> false
    | typeElement ->

    typeElement.GetClrName() = expectoTestType && typeElement.Module.Name = "Expecto"

let isExpectoTest (declaredElement: IDeclaredElement) =
    match declaredElement.As<IModifiersOwner>() with
    | null -> false
    | modifiersOwner ->

    if not modifiersOwner.IsStatic ||
       modifiersOwner.GetAccessRights() <> AccessRights.PUBLIC then false else

    if not (hasTestReturnType declaredElement) then false else

    let solution = declaredElement.GetSolution()
    let attributeChecker = solution.GetComponent<IUnitTestAttributeChecker>()

    match declaredElement.As<IAttributesOwner>() with
    | null -> false
    | attributesOwner -> attributeChecker.HasDerivedAttribute(attributesOwner, attributes)


[<UnitTestProvider>]
type ExpectoProvider() =
    let elementComparer = UnitTestElementComparer(typeof<ExpectoTestElement>)

    let isSupported project targetFrameworkId =
        use cookie = ReadLockCookie.Create()

        let mutable info = Unchecked.defaultof<_>
        ReferencedAssembliesService.IsProjectReferencingAssemblyByName(project, targetFrameworkId, expectoAssemblyName, &info) ||
        ReferencedProjectsService.IsProjectReferencingAssemblyByName(project, targetFrameworkId, expectoAssemblyName, &info)
    
    interface IUnitTestProvider with
        member x.ID = "Expecto"
        member x.Name = "Expecto"

        member x.IsElementOfKind(element: IUnitTestElement, elementKind: UnitTestElementKind) =
            if elementKind <> UnitTestElementKind.Test then false else
            element :? ExpectoTestElement

        member x.IsElementOfKind(declaredElement: IDeclaredElement, elementKind: UnitTestElementKind) =
            match elementKind with
            | UnitTestElementKind.Test -> isExpectoTest declaredElement
            | UnitTestElementKind.TestContainer ->
                match declaredElement.As<ITypeElement>() with
                | null -> false
                | typeElement -> typeElement.GetMembers() |> Seq.exists isExpectoTest
            | _ -> false

        member x.IsSupported(project, targetFrameworkId) = isSupported project targetFrameworkId
        member x.IsSupported(_, project, targetFrameworkId) = isSupported project targetFrameworkId

        member x.CompareUnitTestElements(a, b) = elementComparer.Compare(a, b)
        member x.SupportsResultEventsForParentOf _ = false


and [<SolutionComponent>]
    ExpectoService =
    { Provider: ExpectoProvider
      ElementManager: IUnitTestElementManager
      IdFactory: IUnitTestElementIdFactory }


and [<AllowNullLiteral>]
    ExpectoTestElement(id, name, declaredElement, service: ExpectoService) =
    let mutable parent: IUnitTestElement = Unchecked.defaultof<_>
    let children = new BindableSetCollectionWithoutIndexTracking<_>(UT.Locks.ReadLock, UnitTestElement.EqualityComparer)

    member x.RemoveChild(child) = children.Remove(child) |> ignore
    member x.AppendChild(child) = children.Add(child)

    interface IUnitTestElement with
        member x.Id = id
        member x.Kind = "Expecto tests"

        member x.ShortName = name
        member x.GetPresentation(_, _) = name

        member x.GetDeclaredElement() = declaredElement

        member x.GetProjectFiles() =
            declaredElement.GetSourceFiles()
            |> Seq.map (fun sourceFile -> sourceFile.ToProjectFile())

        member x.GetNamespace() =
            let cl = declaredElement.As<IClrDeclaredElement>()
            UnitTestElementNamespaceFactory.Create(cl.GetContainingType().GetClrName().NamespaceNames)

        member x.GetDisposition() =
            if not (isValid declaredElement) then UnitTestElementDisposition.InvalidDisposition else

            let locations =
                declaredElement.GetDeclarations()
                |> Seq.map (fun decl ->
                    let file = decl.GetContainingFile()
                    if isNull file then null else

                    let projectFile = file.GetSourceFile().ToProjectFile()
                    let nameRange = decl.GetNameDocumentRange().TextRange
                    UnitTestElementLocation(projectFile, nameRange, decl.GetDocumentRange().TextRange))

            UnitTestElementDisposition(locations, x)

        member x.Parent
            with get () = parent
            and set (value) =
                if value == parent then () else
                begin
                    use lock = UT.WriteLock()
                    if isNotNull parent then
                        (parent :?> ExpectoTestElement).RemoveChild(x)

                    if isNotNull value then
                        parent <- value
                        (parent :?> ExpectoTestElement).AppendChild(x)
                end
                service.ElementManager.FireElementChanged(x)

        member x.Children = children :> _

        member x.GetRunStrategy _ = DoNothingRunStrategy() :> _
        member x.GetTaskSequence(explicitElements, run) = null // todo

        member val OwnCategories = Unchecked.defaultof<_> with get, set
        member val Origin = Unchecked.defaultof<_> with get, set

        member x.Explicit = false
        member x.ExplicitReason = ""



type ExpectoElementFactory(expectoService: ExpectoService) =
//    let elements = new WeakToWeakDictionary<UnitTestElementId, IUnitTestElement>()

    member x.CreateTestElement(el: IDeclaredElement, project: IProject, targetFrameworkId) =
        let id = expectoService.IdFactory.Create(expectoService.Provider, project, targetFrameworkId, el.ShortName)
        match expectoService.ElementManager.GetElementById(id) with
        | null -> ExpectoTestElement(id, el.ShortName, el, expectoService) :> IUnitTestElement
        | el -> el


type Processor
        (interrupted: Func<bool>, attributeChecker: IUnitTestAttributeChecker, factory: ExpectoElementFactory,
         observer: IUnitTestElementsObserver, projectFile: IProjectFile) =

    let project = projectFile.GetProject()

    let checkForInterrupt () =
        if interrupted.Invoke() then
            raise (OperationCanceledException())

    interface IRecursiveElementProcessor with
        member x.ProcessingIsFinished = false
        member x.ProcessAfterInterior _ = ()

        member x.InteriorShouldBeProcessed _ =
            true // todo

        member x.ProcessBeforeInterior(element) =
            checkForInterrupt ()

            let declaration = element.As<IDeclaration>()
            if isNull declaration || declaration :? IBinding then () else

            let declaredElement = declaration.DeclaredElement
            if not (isExpectoTest declaredElement) then () else

            let testElement = factory.CreateTestElement(declaredElement, project, observer.TargetFrameworkId)

            let navigationRange = declaration.GetNameDocumentRange().TextRange
            let documentRange = declaration.GetDocumentRange().TextRange

            if navigationRange.IsValid && documentRange.IsValid then
                let disposition = UnitTestElementDisposition(testElement, projectFile, navigationRange, documentRange)
                observer.OnUnitTestElementDisposition(disposition)


[<SolutionComponent>]
type ExpectoTestFileExplorer
        (service: ExpectoService, clrLanguages: ClrLanguagesKnown, attributeChecker: IUnitTestAttributeChecker) =
    interface IUnitTestExplorerFromFile with
        member x.Provider = service.Provider :> _

        member x.ProcessFile(file, observer, interrupted) =
            if not (Seq.contains file.Language clrLanguages.AllLanguages) then () else

            let projectFile = file.GetSourceFile().ToProjectFile()
            if isNull projectFile then () else

            let factory = ExpectoElementFactory(service)
            let processor = Processor(interrupted, attributeChecker, factory, observer, projectFile)
            file.ProcessDescendants(processor)
