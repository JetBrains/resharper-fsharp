module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.UnitTesting.Expecto

open System
open System.Collections.Generic
open System.IO
open System.Linq
open JetBrains.Application.UI.BindableLinq.Collections
open JetBrains.Metadata.Reader.API
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.ClrLanguages
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.UnitTesting.ExpectoTasks
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.TaskRunnerFramework
open JetBrains.ReSharper.UnitTestFramework
open JetBrains.ReSharper.UnitTestFramework.AttributeChecker
open JetBrains.ReSharper.UnitTestFramework.Elements
open JetBrains.ReSharper.UnitTestFramework.Exploration
open JetBrains.ReSharper.UnitTestFramework.Strategy
open JetBrains.Util
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

let isSupported project tfId =
    use cookie = ReadLockCookie.Create()

    let mutable info = Unchecked.defaultof<_>
    ReferencedAssembliesService.IsProjectReferencingAssemblyByName(project, tfId, expectoAssemblyName, &info) ||
    ReferencedProjectsService.IsProjectReferencingAssemblyByName(project, tfId, expectoAssemblyName, &info)

let inline ensureValid (declaredElement: #IDeclaredElement) =
    if not (isValid declaredElement) then null else
    declaredElement :> IDeclaredElement

let runnerInfo =
    let location = typeof<RemoteTask>.Assembly.Location
    let dir = Path.GetDirectoryName(location)
    
    let expectoTaskType = typeof<ExpectoAssemblyTask>
    let location2 = expectoTaskType.Assembly.Location
    let dir2 = Path.GetDirectoryName(location2)
    let path = Path.Combine(dir2, "JetBrains.ReSharper.Plugins.FSharp.Expecto.Runner.dll")

    let typeName = "JetBrains.ReSharper.Plugins.FSharp.Psi.Features.UnitTesting.ExpectoRunner+ExpectoTaskRunner"
    
    RemoteTaskRunnerInfo(ExpectoId, path, typeName, [| dir |])

[<UnitTestProvider>]
type ExpectoProvider() =
    let elementComparer = UnitTestElementComparer(typeof<ExpectoTestsElement>)

    // todo: remove
    let taskIds = Dictionary<int, string>()

    interface IUnitTestProvider with
        member x.ID = ExpectoId
        member x.Name = ExpectoId

        member x.IsElementOfKind(element: IUnitTestElement, elementKind: UnitTestElementKind) =
            match elementKind with
            | UnitTestElementKind.Test
            | UnitTestElementKind.TestContainer -> element :? ExpectoTestsElement
            | UnitTestElementKind.TestStuff -> element :? ExpectoTestElementBase
            | _ -> false

        member x.IsElementOfKind(declaredElement: IDeclaredElement, elementKind: UnitTestElementKind) =
            // todo: remove duplication
            match elementKind with
            | UnitTestElementKind.Test ->
                isExpectoTest declaredElement

            | UnitTestElementKind.TestContainer ->
                match declaredElement.As<ITypeElement>() with
                | null -> false
                | typeElement -> typeElement.GetMembers() |> Seq.exists isExpectoTest

            | UnitTestElementKind.TestStuff ->
                isExpectoTest declaredElement ||
                match declaredElement.As<ITypeElement>() with
                | null -> false
                | typeElement -> typeElement.GetMembers() |> Seq.exists isExpectoTest

            | _ -> false

        member x.IsSupported(project, targetFrameworkId) = isSupported project targetFrameworkId
        member x.IsSupported(_, project, targetFrameworkId) = isSupported project targetFrameworkId

        member x.CompareUnitTestElements(a, b) = elementComparer.Compare(a, b)
        member x.SupportsResultEventsForParentOf _ = false

    interface IDynamicUnitTestProvider with
        member x.GetDynamicElement(remoteTask, taskIdsToElements) =
            match remoteTask.As<ExpectoTestTaskBase>() with
            | null -> failwith "unknown task"
            | task ->

            taskIds.[task.UniqueId] <- task.Id

            let testElement =
                if task.ParentUniqueId = 0 then taskIdsToElements.Values.FirstOrDefault().As<ExpectoTestElementBase>()
                else taskIdsToElements.[taskIds.[task.ParentUniqueId]] :?> _

            let declaredElement = testElement.DeclaredElement :?> IClrDeclaredElement

            let solution = declaredElement.GetSolution()
            let idFactory = solution.GetComponent<IUnitTestElementIdFactory>()
            let service = solution.GetComponent<ExpectoService>()

            let psiModule = declaredElement.Module
            let project = psiModule.ContainingProjectModule :?> IProject
            let id = idFactory.Create(x, project, psiModule.TargetFrameworkId, task.Name)
            ExpectoTestCaseElement(task.Name, id, service, Parent = testElement) :> _


and [<SolutionComponent>]
    ExpectoService =
    { Provider: ExpectoProvider
      Strategy: ExpectoTestRunStrategy
      IdFactory: IUnitTestElementIdFactory
      ElementManager: IUnitTestElementManager
      CachingService: UnitTestingCachingService }


and [<AllowNullLiteral>]
    ExpectoTestCaseElement(name, id: UnitTestElementId, service: ExpectoService) =
    inherit ExpectoTestElementBase(name, id, service)

and [<AbstractClass; AllowNullLiteral>]
    ExpectoTestElementBase(name: string, id: UnitTestElementId, service: ExpectoService) =
    let mutable parent: IUnitTestElement = Unchecked.defaultof<_>
    let children = new BindableSetCollectionWithoutIndexTracking<_>(UT.Locks.ReadLock, UnitTestElement.EqualityComparer)

    member x.RemoveChild(child) = children.Remove(child) |> ignore
    member x.AppendChild(child) = children.Add(child)

    member x.Parent
        with get () = parent
        and set (value: IUnitTestElement) =
            if value == parent then () else
            begin
                use lock = UT.WriteLock()
                if isNotNull parent then
                    (parent :?> ExpectoTestElementBase).RemoveChild(x)

                if isNotNull value then
                    parent <- value
                    (parent :?> ExpectoTestElementBase).AppendChild(x)
            end
            service.ElementManager.FireElementChanged(x)

    abstract DeclaredElement: IDeclaredElement
    default x.DeclaredElement = x.Parent.GetDeclaredElement()

    interface IUnitTestElement with
        member x.Id = id
        member x.Kind = "Expecto tests"

        member x.ShortName = name
        member x.GetPresentation(_, _) = name

        member x.GetDeclaredElement() = x.DeclaredElement

        member x.GetProjectFiles() =
            let declaredElement = x.DeclaredElement
            if isNull declaredElement then null else

            declaredElement.GetSourceFiles().Select(fun sourceFile -> sourceFile.ToProjectFile())

        member x.GetNamespace() =
            let declaredElement = x.DeclaredElement
            if isNull declaredElement then UnitTestElementNamespace.Empty else

            let clrDeclaredElement = declaredElement.As<IClrDeclaredElement>()
            UnitTestElementNamespaceFactory.Create(clrDeclaredElement.GetContainingType().GetClrName().NamespaceNames)

        member x.GetDisposition() =
            let declaredElement = x.DeclaredElement
            if isNull declaredElement then UnitTestElementDisposition.InvalidDisposition else

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
            with get() = x.Parent
            and set(value) = x.Parent <- value

        member x.Children = children :> _

        member x.GetRunStrategy _ = service.Strategy :> _

        member x.GetTaskSequence(_, run) =
            let assemblyTask = ExpectoAssemblyTask(id.Project.GetOutputFilePath(id.TargetFrameworkId).FullPath)

            let elementTask =
                match run.GetRemoteTaskForElement(x) with
                | null -> ExpectoTestsTask(id.Id)
                | task -> task

            [| UnitTestTask(null, assemblyTask)
               UnitTestTask(x, elementTask) |] :> _

        member val OwnCategories = Unchecked.defaultof<_> with get, set
        member val Origin = Unchecked.defaultof<_> with get, set

        member x.Explicit = false
        member x.ExplicitReason = ""


and [<AllowNullLiteral>]
    ExpectoTestsElement(declaredElement, name, id, service) =

    inherit ExpectoTestElementBase(name, id, service)

    let declaredElement = declaredElement.As<IXmlDocIdOwner>()
    let containingType = declaredElement.GetContainingType()

    let typeName = containingType.GetClrName().GetPersistent()
    let xmlDocId = declaredElement.XMLDocId

    let getContainingType () =
        service.CachingService.GetTypeElement(id.Project, id.TargetFrameworkId, typeName, true, true)

    let getDeclaredElement () =
        use cookie = ReadLockCookie.Create()

        match getContainingType () with
        | null -> null
        | containingType ->

        use cookie = CompilationContextCookie.GetOrCreate(containingType.Module.GetContextFromModule())
        let members = containingType.EnumerateMembers(name, true).AsList()
        match members.Count with
        | 0 -> null
        | 1 -> ensureValid members.[0]
        | _ ->

        members
        |> Seq.cast<IXmlDocIdOwner>
        |> Seq.tryFind (fun m -> m.XMLDocId = xmlDocId)
        |> Option.toObj
        |> ensureValid

    override x.DeclaredElement = getDeclaredElement ()

and [<SolutionComponent>]
    ExpectoTestRunStrategy(agentManager, resultManager) =
    inherit TaskRunnerOutOfProcessUnitTestRunStrategy(agentManager, resultManager, runnerInfo)


type ExpectoElementFactory(expectoService: ExpectoService) =
    let elements = new WeakToWeakDictionary<UnitTestElementId, IUnitTestElement>()

    member x.CreateTestElement(el: IDeclaredElement, project: IProject, targetFrameworkId) =
        let id = expectoService.IdFactory.Create(expectoService.Provider, project, targetFrameworkId, el.ShortName)

        let mutable element = Unchecked.defaultof<_>
        if elements.TryGetValue(id, &element) then element else

        match expectoService.ElementManager.GetElementById(id) with
        | null ->
            let element = ExpectoTestsElement(el, el.ShortName, id, expectoService) :> IUnitTestElement
            elements.[id] <- element
            element

        | element -> element


type Processor
        (interrupted: Func<bool>, factory: ExpectoElementFactory, observer: IUnitTestElementsObserver,
         projectFile: IProjectFile) =

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
type ExpectoTestFileExplorer(service: ExpectoService, clrLanguages: ClrLanguagesKnown) =
    interface IUnitTestExplorerFromFile with
        member x.Provider = service.Provider :> _

        member x.ProcessFile(file, observer, interrupted) =
            if not (Seq.contains file.Language clrLanguages.AllLanguages) then () else

            let projectFile = file.GetSourceFile().ToProjectFile()
            if isNull projectFile then () else

            let factory = ExpectoElementFactory(service)
            let processor = Processor(interrupted, factory, observer, projectFile)
            file.ProcessDescendants(processor)


let inline isPublic m = (^M: (member IsPublic: bool) m)
let inline isStatic m = (^M: (member IsStatic: bool) m)
let inline returnType m = (^M: (member ReturnValue: IMetadataReturnValue) m).Type
let inline fieldType m = (^M: (member Type: IMetadataType) m)

let isExpectoType (t: IMetadataType) = t.FullName = "Expecto.Test"

let isExpectoValue m = isPublic m && isStatic m && isExpectoType (fieldType m)
let isExpectoFunction m = isPublic m && isStatic m && isExpectoType (returnType m)


[<SolutionComponent>]
type ExpectoTestMetadataExplorer(provider: ExpectoProvider, resolveManager, resolveContextManager, logger) =
    inherit UnitTestExplorerFrom.DotNetArtifacts(provider, resolveManager, resolveContextManager, logger)

    override x.ProcessProject(project, assemblyPath, loader, observer, cancellationToken) =
        MetadataElementsSource.ExploreProject(project, assemblyPath, loader, observer, logger, cancellationToken,
                fun assembly ->

            let members = List<IMetadataTypeMember>()

            for t in assembly.GetTypes() do
                for field in t.GetFields() do
                    if isExpectoValue field then members.Add(field)

                for property in t.GetProperties() do
                    let getter = property.Getter
                    if isExpectoFunction getter then members.Add(property)
 
                for method in t.GetMethods() do
                    if isExpectoFunction method then members.Add(method)

            ())
