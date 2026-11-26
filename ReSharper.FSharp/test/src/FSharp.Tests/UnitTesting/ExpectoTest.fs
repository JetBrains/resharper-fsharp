namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.UnitTesting

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Reflection
open System.Text.RegularExpressions
open System.Threading
open System.Threading.Tasks
open JetBrains.Application.Components
open JetBrains.Application.Progress
open JetBrains.Application.Settings
open JetBrains.DocumentManagers
open JetBrains.DocumentManagers.Transactions
open JetBrains.HabitatDetector
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.BuildTools
open JetBrains.ProjectModel.DataContext
open JetBrains.ProjectModel.Properties.Managed
open JetBrains.ReSharper.FeaturesTestFramework.Daemon
open JetBrains.ReSharper.FeaturesTestFramework.UnitTesting
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.UnitTesting
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.TestFramework
open JetBrains.ReSharper.UnitTestFramework
open JetBrains.ReSharper.UnitTestFramework.Criteria
open JetBrains.ReSharper.UnitTestFramework.Elements
open JetBrains.ReSharper.UnitTestFramework.Execution
open JetBrains.ReSharper.UnitTestFramework.Execution.Hosting
open JetBrains.ReSharper.UnitTestFramework.Execution.Launch
open JetBrains.ReSharper.UnitTestFramework.Exploration
open JetBrains.ReSharper.UnitTestFramework.Exploration.Artifacts
open JetBrains.ReSharper.UnitTestFramework.Exploration.Daemon
open JetBrains.ReSharper.UnitTestFramework.Exploration.Daemon.Highlighting
open JetBrains.ReSharper.UnitTestFramework.Persistence
open JetBrains.ReSharper.UnitTestFramework.Session
open JetBrains.ReSharper.UnitTestFramework.Settings
open JetBrains.ReSharper.UnitTesting.VsTest.Provider
open JetBrains.TestFramework
open JetBrains.TestFramework.TestCompiler
open JetBrains.Threading
open JetBrains.Util
open NUnit.Framework

type RecordingObserverOnFile(observer: IUnitTestElementObserver, sourceFile: IPsiSourceFile) =
    member val Dispositions = List<IUnitTestLikeElement * TextRange * TextRange>()

    interface IUnitTestElementObserverOnFile with
        member this.Source = observer.Source
        member this.PsiSourceFile = sourceFile
        member this.GetElementById<'T when 'T: not struct>(testId, salt) = observer.GetElementById<'T>(testId, salt)
        member this.OnUnitTestElement(element) = observer.OnUnitTestElement(element)

        member this.OnUnitTestElementDisposition(element, navigationRange, containingRange) =
            match element with
            | :? IUnitTestElement as element -> observer.OnUnitTestElement(element)
            | _ -> ()

            this.Dispositions.Add((element, navigationRange, containingRange))


type RunnerLocation =
    | NotLocated
    | RootFileLine of line: int
    | FileLine of fileName: string * line: int


type DiscoveredTest =
    { Id: string
      Location: RunnerLocation }


[<AutoOpen>]
module DiscoveredTest =
    let notLocated (testIds: string list) =
        [ for testId in testIds -> { Id = testId; Location = NotLocated } ]

    let atLine (line: int) (testIds: string list) =
        [ for testId in testIds -> { Id = testId; Location = RootFileLine line } ]

    let inFile (fileName: string) (line: int) (testIds: string list) =
        [ for testId in testIds -> { Id = testId; Location = FileLine(fileName, line) } ]

    let inMissingFile (testIds: string list) =
        inFile "Missing" 1 testIds


module ExpectoTestUtil =
    let [<Literal>] ExpectoPackage = "Expecto/11.1.1"
    let [<Literal>] ExpectoFsCheckPackage = "Expecto.FsCheck/11.1.1"
    let [<Literal>] ExpectoHopacPackage = "Expecto.Hopac/11.1.1"

    let timeout = TimeSpan.FromMinutes(2.)

    let wait (task: Task) =
        let isCompleted = JetDispatcher.Run(task, timeout, true)
        Assert.IsTrue(isCompleted, "The task did not complete in time")

    let waitUntil (isMet: unit -> bool) (failureMessage: string) =
        let isMetInTime = JetDispatcher.Run(Func<bool>(isMet >> not), timeout, false)
        Assert.IsTrue(isMetInTime, failureMessage)

    let getElements (solution: ISolution) =
        let repository = solution.GetComponent<IUnitTestElementRepository>()
        let elements = repository.Query(SolutionCriterion.Instance).Cast<ExpectoTestElement>()
        elements.OrderBy((fun element -> element.NaturalId.TestId), StringComparer.Ordinal)

    let dumpElements (withLocations: bool) (writer: TextWriter) (solution: ISolution) =
        use cookie = UT.ReadLock()

        for element in getElements solution do
            let isGroup = element.Provider.IsElementOfKind(element, UnitTestElementKind.TestContainer)
            let kind = if isGroup then "group" else "test"
            let path = element.Path
            Assert.AreEqual(element.NaturalId.TestId, ExpectoTestElement.GetId(path))

            let parentIdInPath = ExpectoTestElement.GetId(Array.take (path.Length - 1) path)
            let parentId = if isNull element.Parent then "" else element.Parent.NaturalId.TestId
            let parent = if parentId = parentIdInPath then "" else $", parent: {parentId}"

            let ns = element.GetNamespace()
            let nsText = if ns = UnitTestElementNamespace.Global then "" else $" in {ns}"
            let sourceFileName = if isNull element.SourceFilePath then "" else Path.GetFileName(element.SourceFilePath)
            let line = if element.Line = 0 then "" else $":{element.Line}"
            let locations = if withLocations then element.GetLocations() else Seq.empty
            let navigationLines =
                locations
                |> Seq.map (fun location ->
                    let projectFile = location.ProjectFile
                    let hasOtherFiles = projectFile.GetProject().GetAllProjectFiles().Count() > 1
                    let fileName = if hasOtherFiles then projectFile.Name + " " else ""

                    let document = projectFile.GetDocument()
                    let line = int (document.GetCoordsByOffset(location.NavigationRange.StartOffset).Line) + 1
                    $", {fileName}{line}: {document.GetText(location.NavigationRange).Trim()}")
                |> String.concat ""

            writer.WriteLine($"{element.NaturalId.TestId}: {kind}{parent}, {element.Origin}, {element.TypeName}{nsText}, {sourceFileName}{line}{navigationLines}")

    let replaceText (projectFile: IProjectFile) (oldText: string) (newText: string) =
        let document = projectFile.GetDocument()
        let offset = document.GetText().IndexOf(oldText, StringComparison.Ordinal)
        Assert.That(offset, Is.Not.Negative, $"{projectFile.Name} does not contain: {oldText}")

        do
            use cookie = WriteLockCookie.Create()
            document.ReplaceText(TextRange(offset, offset + oldText.Length), newText)

        projectFile.GetSolution().GetPsiServices().Files.CommitAllDocuments()
        document

    let private bindSolutionSettings (solution: ISolution) =
        let settingsStore = solution.GetComponent<ISettingsStore>()
        settingsStore.BindToContextTransient(ContextRange.ManuallyRestrictWritesToOneContext(solution.ToDataContext2()))

    let setRunSettingsFile (solution: ISolution) (runSettingsPath: string) =
        let solutionSettings = bindSolutionSettings solution
        solutionSettings.SetValue<UnitTestRunnerSettings, string>((fun settings -> settings.RunConfigurationFilename), runSettingsPath)

    let setDotNetCliExePath (solution: ISolution) (dotNetCliExePath: FileSystemPath) =
        let solutionSettings = bindSolutionSettings solution
        solutionSettings.SetValue<BuildToolSettings, string>((fun settings -> settings.DotNetCliExePath), dotNetCliExePath.FullPath)

    let findElement (solution: ISolution) (testId: string) =
        use cookie = UT.ReadLock()
        (getElements solution).Single(fun element -> element.NaturalId.TestId = testId)

    let createLaunch (project: IProject) (testId: string option) (disableParallelization: bool) =
        let solution = project.GetSolution()

        let criterion: IUnitTestElementCriterion =
            match testId with
            | Some testId -> TestAncestorCriterion(findElement solution testId)
            | None -> ProjectCriterion(project)

        let session = solution.GetComponent<IUnitTestSessionRepository>().CreateSession(criterion)
        let launchManager = solution.GetComponent<IUnitTestLaunchManager>()
        let launch = launchManager.CreateLaunch(session, UnitTestElements(criterion), solution.GetComponent<RunHostProvider>())
        launch.Settings.TestRunner.DisableParallelization.Value <- Nullable disableParallelization
        session.Launch.Value <- launch
        session, launch

    let dumpResults (writer: TextWriter) (solution: ISolution) (session: IUnitTestSession) =
        use cookie = UT.ReadLock()

        writer.WriteLine()
        writer.WriteLine("Results:")

        let resultManager = solution.GetComponent<IUnitTestResultManager>()
        let elementsWithResult =
            getElements solution
            |> Seq.filter (fun element -> resultManager.GetResult(element, session).Status <> UnitTestStatus.Unknown)

        for element in elementsWithResult do
            let result = resultManager.GetResult(element, session)
            let message = result.ShortMessage.Split('\n').FirstOrDefault().Trim()
            writer.WriteLine($"{element.NaturalId.TestId}: {result.Status} {message}")

            let resultData = resultManager.GetResultData(element, session)
            let output = String.concat "" [ for i in 0 .. resultData.OutputChunksCount - 1 -> resultData.GetOutputChunk(i) ]
            for line in output.Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries) do
                writer.WriteLine($"  Output: {line.TrimEnd()}")

            for i in 0 .. resultData.ExceptionCount - 1 do
                let exceptionInfo = resultData.GetExceptionInfo(i)
                if i > 0 then
                    writer.WriteLine($"  Inner exception: {exceptionInfo.Message}")

                let sourceLine = Regex.Match(string exceptionInfo.StackTrace, @"Tests\.fs:line \d+")
                if sourceLine.Success then
                    writer.WriteLine($"  Stack trace: {sourceLine.Value}")


type ExpectoExploration(project: IProject, rootFileName: string) =
    let solution = project.GetSolution()
    let provider = Shell.Instance.GetComponent<ExpectoTestProvider>()
    let discoveryManager = solution.GetComponent<IUnitTestDiscoveryManager>()
    let target = ExplorationTarget(project, project.GetCurrentTargetFrameworkId(), provider)
    let projectFiles = project.GetAllProjectFiles() |> Seq.sortBy _.Name |> List.ofSeq

    member x.ProjectFiles = projectFiles
    member x.RootFile = projectFiles |> List.find (fun projectFile -> projectFile.Name = rootFileName)

    member x.ShortestTypeNameInRootFile =
        use cookie = ReadLockCookie.Create()

        let sourceFile = x.RootFile.ToSourceFile()
        sourceFile.GetPsiServices().Symbols.GetTypesAndNamespacesInFile(sourceFile)
        |> Seq.choose (function :? ITypeElement as typeElement -> Some(typeElement.GetClrName().FullName) | _ -> None)
        |> Seq.minBy String.length

    member x.AddDiscoveredTests(tests: DiscoveredTest list) =
        use transaction = discoveryManager.BeginTransaction(UnitTestElementSource(UnitTestElementOrigin.Artifact, target))
        let elementFactory = ExpectoTestElementFactory()
        let typeName = x.ShortestTypeNameInRootFile
        let rootFileLocation = x.RootFile.Location

        for test in tests do
            let sourceFilePath, line =
                match test.Location with
                | NotLocated -> null, 0
                | RootFileLine line -> rootFileLocation.FullPath, line
                | FileLine(fileName, line) -> rootFileLocation.Parent.Combine(fileName).FullPath, line

            let path = ExpectoTestElement.GetPath(test.Id)
            let mutable element = elementFactory.GetOrCreate(path, typeName, false, sourceFilePath, line)
            while isNotNull element do
                transaction.Observer.OnUnitTestElement(element)
                element <- element.Parent :?> ExpectoTestElement

        ExpectoTestUtil.wait (transaction.CommitAsync(CancellationToken.None))

    member x.ExploreFile(projectFile: IProjectFile, ?interrupted: Func<bool>) =
        let interrupted = defaultArg interrupted (Func<bool>(fun () -> false))
        let sourceFile = projectFile.ToSourceFiles().Single()
        let psiFile = sourceFile.GetPrimaryPsiFile()

        let source = UnitTestElementSourceOnFile(UnitTestElementOrigin.Source, target, projectFile)
        use transaction = discoveryManager.BeginTransaction(source)
        let observer = RecordingObserverOnFile(transaction.Observer, sourceFile)
        let explorer = solution.GetComponent<ExpectoTestExplorerFromFile>() :> IUnitTestExplorerFromFile

        do
            use cookie = ReadLockCookie.Create()
            use compilationContextCookie = CompilationContextCookie.GetOrCreate(psiFile.GetResolveContext())
            explorer.ProcessFile(psiFile, observer, interrupted)

        ExpectoTestUtil.wait (transaction.CommitAsync(CancellationToken.None))
        observer.Dispositions


[<AbstractClass>]
type ExpectoTestBase() =
    inherit BaseTestWithSingleProject()

    let mutable testAction: IProject -> unit = ignore

    member x.WithProject(fileNames: string[], action: IProject -> unit) =
        testAction <- action
        x.DoTestSolution(fileNames)

    member x.WithProjects(fileSets: string[][], action: IProject -> unit) =
        testAction <- action
        x.DoTestSolution(fileSets)

    override x.DoTest(_: Lifetime, project: IProject) =
        testAction project

    member x.WriteGold(write: TextWriter -> unit) =
        x.ExecuteWithGold(fun writer -> write writer) |> ignore


[<AbstractClass>]
type ExpectoExplorationTestBase() =
    inherit ExpectoTestBase()

    override x.RelativeTestDataPath = "features/unitTesting/expecto"

    member x.WithDiscoveredTests(fileNames: string[], tests: DiscoveredTest list, action: ExpectoExploration -> unit) =
        x.WithProject(fileNames, fun project ->
            let exploration = ExpectoExploration(project, Array.last fileNames)
            exploration.AddDiscoveredTests(tests)
            action exploration)

    member x.WithDiscoveredTests(tests: DiscoveredTest list, action: ExpectoExploration -> unit) =
        let fileName = x.TestName
        x.WithDiscoveredTests([| fileName |], tests, action)


[<AbstractClass>]
type ExpectoFileExplorationTestBase() =
    inherit ExpectoExplorationTestBase()

    member private x.ExploreFilesAndWriteGold(exploration: ExpectoExploration) =
        let fileDispositions =
            [ for projectFile in exploration.ProjectFiles -> projectFile, exploration.ExploreFile(projectFile) ]

        x.WriteGold(fun writer ->
            for projectFile, dispositions in fileDispositions do
                let document = projectFile.GetDocument()
                let getLineNumber offset = int (document.GetCoordsByOffset(offset).Line) + 1

                let fileName = if fileDispositions.Length = 1 then "" else $" in {projectFile.Name}"
                writer.WriteLine($"Dispositions{fileName}:")

                for element, navigationRange, containingRange in dispositions do
                    let elements =
                        match element with
                        | :? IUnitTestElement as element -> Seq.singleton element
                        | _ -> element.GetRelatedUnitTestElements()

                    let testIds = elements |> Seq.map (fun element -> element.NaturalId.TestId) |> String.concat ", "
                    let lines = $"{getLineNumber containingRange.StartOffset}-{getLineNumber containingRange.EndOffset}"
                    writer.WriteLine($"{document.GetText(navigationRange)} {lines}: {testIds}")

                writer.WriteLine()

            writer.WriteLine("Elements:")
            ExpectoTestUtil.dumpElements true writer (exploration.RootFile.GetSolution()))

    member x.DoNamedTest(discoveredTests: DiscoveredTest list) =
        x.WithDiscoveredTests(discoveredTests, x.ExploreFilesAndWriteGold)

    member x.DoTestFiles(discoveredTests: DiscoveredTest list, [<ParamArray>] fileNames: string[]) =
        x.WithDiscoveredTests(fileNames, discoveredTests, x.ExploreFilesAndWriteGold)

    member x.DoPlacesAfterEditTest(discoveredTests: DiscoveredTest list, editedFileName: string, oldText: string,
            newText: string, [<ParamArray>] fileNames: string[]) =
        x.WithDiscoveredTests(fileNames, discoveredTests, fun exploration ->
            let solution = exploration.RootFile.GetSolution()
            let editedFile = exploration.ProjectFiles |> List.find (fun projectFile -> projectFile.Name = editedFileName)

            x.WriteGold(fun writer ->
                ExpectoTestUtil.dumpElements true writer solution
                let unsavedDocument = ExpectoTestUtil.replaceText editedFile oldText newText

                writer.WriteLine()
                writer.WriteLine("After the edit:")
                ExpectoTestUtil.dumpElements true writer solution
                GC.KeepAlive(unsavedDocument)))

    member x.DoNamedTestWithReplacedText(discoveredTests: DiscoveredTest list, oldText: string, newText: string) =
        x.WithDiscoveredTests(discoveredTests, fun exploration ->
            exploration.ExploreFile(exploration.RootFile) |> ignore
            let unsavedDocument = ExpectoTestUtil.replaceText exploration.RootFile oldText newText
            x.ExploreFilesAndWriteGold(exploration)
            GC.KeepAlive(unsavedDocument))


[<FSharpTest; EnsureUnitTestRepositoryIsEmpty>]
[<TestPackages(ExpectoTestUtil.ExpectoPackage, ExpectoTestUtil.ExpectoFsCheckPackage, ExpectoTestUtil.ExpectoHopacPackage)>]
type ExpectoFileExplorationTest() =
    inherit ExpectoFileExplorationTestBase()

    [<Test>] member x.``Tests attribute 01``() = x.DoNamedTest([])
    [<Test>] member x.``Tests attribute 02 - Operators``() = x.DoNamedTest([])
    [<Test>] member x.``Tests attribute 03 - Computed tests``() = x.DoNamedTest([])
    [<Test>] member x.``Tests attribute 04 - Not Expecto``() = x.DoNamedTest([])
    [<Test>] member x.``Tests attribute 05 - Computed group discovered``() = x.DoNamedTest(notLocated ["1.test"])

    [<Test>] member x.``Tests attribute 06 - Static member``() = x.DoNamedTest([])
    [<Test>] member x.``Tests attribute 07 - Conditional``() = x.DoNamedTest(notLocated ["All.conditional"; "All.many"])

    [<Test>]
    member x.``Tests attribute 08 - Computed group prefix``() =
        x.DoNamedTest(notLocated ["First.Computed.test"; "Second.Named.test"; "Second.1.test"])

    [<Test>] member x.``Tests attribute 09 - Escaped names``() = x.DoNamedTest([])
    [<Test>] member x.``Tests attribute 10 - Literal name``() = x.DoNamedTest([])
    [<Test>] member x.``Tests attribute 11 - Not public``() = x.DoNamedTest([])
    [<Test>] member x.``Tests attribute 12 - Not test type``() = x.DoNamedTest([])
    [<Test>] member x.``Tests attribute 13 - Dot in name``() = x.DoNamedTest([])
    [<Test>] member x.``Tests attribute 14 - Namespace``() = x.DoNamedTest([])

    [<Test>]
    member x.``Tests attribute 15 - Removed reference``() =
        x.DoNamedTestWithReplacedText([], "        parserTests", "")

    [<Test>]
    member x.``Tests attribute 16 - Group name not literal after build``() =
        x.DoNamedTestWithReplacedText(atLine 9 ["All.Comp.x"], "testList \"Comp\"", "testList groupName")

    [<Test>]
    member x.``Tests attribute 17 - Renamed group with computed tests``() =
        x.DoNamedTestWithReplacedText(atLine 10 ["All.G.1"; "All.G.2"], "\"G\"", "\"H\"")

    [<Test>] member x.``Tests attribute 18 - Incomplete code``() = x.DoNamedTest([])

    [<Test>]
    member x.``Tests attribute 19 - Own functions discovered``() =
        x.DoTestFiles(notLocated ["All.test"], "Tests attribute 04 - Not Expecto.fs")

    [<Test>] member x.``Tests attribute 20 - Yield and type annotation``() = x.DoNamedTest([])
    [<Test>] member x.``Tests attribute 21 - No module declaration``() = x.DoNamedTest([])

    [<Test>]
    member x.``Tests attribute 22 - Theory``() =
        x.DoNamedTest(atLine 8 ["All.theory.1"; "All.theory.2"])

    [<Test>] member x.``Tests attribute 23 - Hopac``() = x.DoNamedTest([])
    [<Test>] member x.``Tests attribute 24 - Call shapes``() = x.DoNamedTest([])

    [<Test; Explicit("Not implemented")>]
    member x.``Tests attribute 25 - Own functions``() =
        x.DoTestFiles([], "Tests attribute 04 - Not Expecto.fs")

    [<Test>]
    member x.``Tests attribute 26 - Local binding``() =
        x.DoNamedTest(notLocated ["All.local"; "All.mapped"])

    [<Test>] member x.``Tests attribute 27 - Concatenated names``() = x.DoNamedTest([])

    [<Test>] member x.``Property 01``() = x.DoNamedTest([])
    [<Test>] member x.``Property 02 - Std gen``() = x.DoNamedTest([])

    [<Test>] member x.``No attribute 01``() = x.DoNamedTest([])

    [<Test>]
    member x.``No attribute 02 - Discovered``() =
        x.DoNamedTest(notLocated ["All.Parser.numbers"; "All.Parser.strings"; "All.Parser.removed"])

    [<Test>]
    member x.``No attribute 03 - Discovered twice``() =
        x.DoNamedTest(notLocated ["First.Shared.test"; "Second.Shared.test"])

    [<Test>] member x.``No attribute 04 - Async builder``() = x.DoNamedTest(notLocated ["All.Parser.async"])
    [<Test>] member x.``No attribute 05 - Same name at two depths``() = x.DoNamedTest(notLocated ["All.Nested.same"])

    [<Test>] member x.``Reference 01``() = x.DoNamedTest([])
    [<Test>] member x.``Reference 02 - Recursive``() = x.DoNamedTest([])
    [<Test>] member x.``Reference 03 - Parameters``() = x.DoNamedTest(notLocated ["All.Computed.computed child"])
    [<Test>] member x.``Reference 04 - Repeated``() = x.DoNamedTest([])
    [<Test>] member x.``Reference 05 - Static member``() = x.DoNamedTest([])
    [<Test>] member x.``Reference 06 - Used in two modules``() = x.DoNamedTest([])
    [<Test>] member x.``Reference 07 - Qualifier``() = x.DoNamedTest(notLocated ["All.second"])

    [<Test>]
    member x.``Reference 08 - Source root and discovered root``() =
        x.DoNamedTest(notLocated ["Second.Shared.test"])

    [<Test>]
    member x.``Reference 09 - Test as function argument``() =
        let wrappedTests = ["All.Wrapper.direct"; "All.Wrapper.piped"; "All.in option"; "All.Lambda.to lambda"]
        x.DoNamedTest(notLocated wrappedTests)

    [<Test>]
    member x.``Reference 10 - More arguments than parameters``() =
        x.DoNamedTest(notLocated ["All.Db.query"; "All.Piped.piped"])

    [<Test>]
    member x.``Reference 11 - Computed name and same literal name``() =
        x.DoNamedTest(atLine 5 ["First.a"])

    [<Test>]
    member x.``Located by runner 01 - Loop``() =
        x.DoNamedTest(atLine 10 ["Generated.case 1"; "Generated.case 2"; "Generated.case 3"])

    [<Test>]
    member x.``Located by runner 02 - No attribute``() =
        let discoveredTests =
            atLine 8 ["All.Parser.1"; "All.Parser.2"] @ notLocated ["All.Parser.static"] @
            atLine 30 ["All.Other.unplaced"] @ inFile "Other.fs" 8 ["All.Other.elsewhere"]

        x.DoNamedTest(discoveredTests)

    [<Test>] member x.``Located by runner 03 - Function value``() = x.DoNamedTest(atLine 8 ["Mapped.a"; "Mapped.b"])

    [<Test>]
    member x.``Located by runner 04 - Lambda after edit``() =
        let discoveredTests = atLine 10 ["All.first"] @ atLine 11 ["All.second"]
        let oldText = "testList \"All\" ["
        x.DoNamedTestWithReplacedText(discoveredTests, oldText, oldText + "\n        testCase \"added\" <| fun _ -> ()")

    [<Test>]
    member x.``Located by runner 05 - No call in source``() =
        x.DoNamedTest(atLine 5 ["All.external"])

    [<Test>]
    member x.``Interrupted exploration``() =
        x.WithDiscoveredTests([| "Tests attribute 01.fs" |], [], fun exploration ->
            let exploreInterrupted () = exploration.ExploreFile(exploration.RootFile, Func<bool>(fun () -> true)) |> ignore
            Assert.Throws<OperationCanceledException>(exploreInterrupted) |> ignore)

    [<Test>]
    member x.``Two files 01``() =
        let discoveredTests = inFile "Two files 01 - Lists.fs" 7 ["All.Parser.numbers"] @ atLine 9 ["All.direct"]
        x.DoTestFiles(discoveredTests, "Two files 01 - Lists.fs", "Two files 01 - Main.fs")

    [<Test>]
    member x.``Two files 02 - No runner location``() =
        let discoveredTests = notLocated ["All.Parser.numbers"; "All.direct"]
        x.DoTestFiles(discoveredTests, "Two files 01 - Lists.fs", "Two files 01 - Main.fs")

    [<Test>]
    member x.``Two files 03 - Same names in two lists``() =
        let listsFileName = "Two files 03 - Lists.fs"
        let discoveredTests =
            inFile listsFileName 7 ["All.Parser.numbers"] @ inFile listsFileName 12 ["Other.Parser.numbers"]

        x.DoTestFiles(discoveredTests, listsFileName, "Two files 03 - Main.fs")

    [<Test>]
    member x.``Two files 04 - Places after a blank line``() =
        let listsFileName = "Two files 01 - Lists.fs"
        let discoveredTests = inFile listsFileName 7 ["All.Parser.numbers"] @ atLine 9 ["All.direct"]
        x.DoPlacesAfterEditTest(discoveredTests, listsFileName, "let parserTests", "\nlet parserTests", listsFileName,
            "Two files 01 - Main.fs")

    [<Test>]
    member x.``Two files 05 - Runner file not in project``() =
        let discoveredTests = inMissingFile ["All.Parser.numbers"] @ atLine 9 ["All.direct"]
        x.DoTestFiles(discoveredTests, "Two files 01 - Lists.fs", "Two files 01 - Main.fs")

    [<Test>]
    member x.``Two files 06 - Same declaration name in two files``() =
        x.DoTestFiles(notLocated ["All.Parser.numbers"], "Two files 06 - Lists.fs", "Two files 06 - Main.fs")

    [<Test>]
    member x.``Two files 07 - Three files``() =
        let fileNames = [| "Two files 07 - Inner.fs"; "Two files 07 - Middle.fs"; "Two files 07 - Main.fs" |]
        x.DoTestFiles(notLocated ["All.Middle.Inner.test"], fileNames)

    [<Test>]
    member x.``Two files 08 - Signature file``() =
        let fileNames = [| "Two files 08 - Lists.fsi"; "Two files 08 - Lists.fs"; "Two files 08 - Main.fs" |]
        x.DoTestFiles(notLocated ["All.Parser.numbers"], fileNames)

    [<Test>]
    member x.``Two files 09 - Computed name beside a list in another file``() =
        let discoveredTests = notLocated ["All.Db.query"] @ atLine 9 ["All.case 1"; "All.case 2"]
        x.DoTestFiles(discoveredTests, "Two files 09 - Db.fs", "Two files 09 - Main.fs")


[<TestNet80(ExpectoTestUtil.ExpectoPackage); EnsureUnitTestRepositoryIsEmpty>]
type ExpectoCSharpFileExplorationTest() =
    inherit ExpectoFileExplorationTestBase()

    [<Test>]
    member x.``CSharp 01 - Explorer``() =
        x.DoNamedTest(notLocated ["Method.in method"; "All.No attribute.discovered"])

    [<Test>] member x.``CSharp 02 - Reference``() = x.DoNamedTest([])
    [<Test>] member x.``CSharp 03 - Names``() = x.DoNamedTest([])
    [<Test>] member x.``CSharp 04 - Conditional``() = x.DoNamedTest(notLocated ["All.conditional"])
    [<Test>] member x.``CSharp 05 - Lambda``() = x.DoNamedTest(notLocated ["All.case"; "All.standard"])
    [<Test>] member x.``CSharp 06 - Local function``() = x.DoNamedTest(notLocated ["All.local"])
    [<Test>] member x.``CSharp 07 - Qualifier``() = x.DoNamedTest(notLocated ["All.second"])
    [<Test>] member x.``CSharp 08 - Collections``() = x.DoNamedTest([])
    [<Test>] member x.``CSharp 09 - Incomplete code``() = x.DoNamedTest([])
    [<Test>] member x.``CSharp 10 - Method call``() = x.DoNamedTest([])

    [<Test>]
    member x.``CSharp 11 - Two files``() =
        x.DoTestFiles(notLocated ["All.Parser.numbers"], "CSharp 11 - Lists.cs", "CSharp 11 - Main.cs")

    [<Test>]
    member x.``CSharp 12 - Partial class``() =
        x.DoTestFiles(notLocated ["All.Shared.x"], "CSharp 12 - Part 2.cs", "CSharp 12 - Part 1.cs")

    [<Test>]
    member x.``CSharp 13 - Places after an edit in a method``() =
        let fileName = x.TestName
        x.DoPlacesAfterEditTest(atLine 10 ["All.first"], fileName, "return Runner", "var x = 1;\n        return Runner",
            fileName)


[<FSharpTest; TestPackages(ExpectoTestUtil.ExpectoPackage); EnsureUnitTestRepositoryIsEmpty>]
type ExpectoGutterMarkTest() =
    inherit HighlightingTestBase()

    override x.RelativeTestDataPath = "features/unitTesting/expecto"
    override x.CompilerIdsLanguage = FSharpLanguage.Instance :> _
    override x.HighlightingPredicate(highlighting, _, _) = highlighting :? IUnitTestElementHighlighting

    [<Test>] member x.``Tests attribute 01``() = x.DoNamedTest()


[<FSharpTest; TestPackages(ExpectoTestUtil.ExpectoPackage); EnsureUnitTestRepositoryIsEmpty>]
type ExpectoLocationUpdateTest() =
    inherit ExpectoExplorationTestBase()

    member x.DoTestFile(fileName: string, discoveredTests: DiscoveredTest list, rediscoveredTests: DiscoveredTest list) =
        x.WithDiscoveredTests([| fileName |], discoveredTests, fun exploration ->
            exploration.ExploreFile(exploration.RootFile) |> ignore
            exploration.AddDiscoveredTests(rediscoveredTests)
            let solution = exploration.RootFile.GetSolution()
            x.WriteGold(fun writer -> ExpectoTestUtil.dumpElements false writer solution))

    member x.DoNamedTest(discoveredTests: DiscoveredTest list, rediscoveredTests: DiscoveredTest list) =
        x.DoTestFile(x.TestName, discoveredTests, rediscoveredTests)

    [<Test>]
    member x.``Location update 01``() =
        let testIds = ["Generated.case 1"; "Generated.case 2"]
        x.DoNamedTest(atLine 8 testIds, atLine 12 testIds)

    [<Test>]
    member x.``Location update 02 - Source location``() =
        x.DoNamedTest([], inFile "Helpers.fs" 3 ["All.helper"])

    [<Test>]
    member x.``Location update 03 - Location kept``() =
        let testIds = ["Generated.case 1"; "Generated.case 2"]
        x.DoTestFile("Location update 01.fs", atLine 8 testIds, notLocated testIds)

    [<Test>]
    member x.``Location update 04 - Group becomes test``() =
        x.DoNamedTest([], notLocated ["All.x"])


[<AbstractClass>]
type ExpectoTestRunnerTestBase(targetFramework: TestTargetFramework, outputExtension: string) =
    inherit ExpectoTestBase()

    let mutable compiler: DotNetTestCodeCompiler = null

    override x.RelativeTestDataPath = "features/unitTesting/expecto/ExpectoTestProject"

    member private x.WithExploredProject(compileProperty: string option, outputType: ProjectOutputType,
            action: IProject -> string -> unit) =
        if isNull compiler then
            compiler <- DotNetTestCodeCompiler(x.TestFixtureLifetime, ContinuousIntegration.External)

        let projectPath = x.TestDataPath / "ExpectoTestProject.fsproj"
        let compileResult =
            compiler.Compile(projectPath, targetFramework, HabitatInfo.OSArchitecture,
                compileMode = DotNetTestCodeCompiler.CompileMode.Build, customProperty = Option.toObj compileProperty).Result

        let dotNetSdk = compiler.GetDotNetSdk(DotNetSdkSource.DeduceStable(targetFramework)).Result

        let outputDir = compileResult.OutputDir
        let expectoPath = outputDir / "Expecto.dll"
        let expectoVersion = AssemblyName.GetAssemblyName(expectoPath.FullPath).Version

        x.WithProject([| "Tests.fs" |], fun project ->
            let solution = project.GetSolution()
            let configuration = project.ProjectProperties.ActiveConfigurations.Configurations.Single() :?> IManagedProjectConfiguration
            configuration.RelativeOutputDirectory <- outputDir.FullPath
            configuration.OutputType <- outputType
            configuration.OutputAssemblyName <- "ExpectoTestProject"
            configuration.OutputAssemblyExtension <- outputExtension

            do
                use writeLockCookie = WriteLockCookie.Create()
                use cookie = solution.CreateTransactionCookie(DefaultAction.Commit, "Add reference", NullProgressIndicator.Create())
                let targetFrameworkId = project.GetCurrentTargetFrameworkId()
                cookie.AddAssemblyReference(project, expectoPath.ToVirtualFileSystemPath(), targetFrameworkId) |> ignore

            ExpectoTestUtil.setDotNetCliExePath solution dotNetSdk.DotNetExecutable
            ExpectoTestUtil.wait (solution.GetComponent<IUnitTestArtifactExplorationProcess>().ExploreProject(project))

            let explorer = solution.GetComponent<ExpectoTestExplorerFromArtifacts>()
            let support = explorer.IsSupported(project, project.GetCurrentTargetFrameworkId())
            let reason =
                match support.Details with
                | :? ReasonDetails as reasonDetails -> $", {reasonDetails.Message}"
                | _ -> ""

            action project $"Expecto: {expectoVersion}, supported: {support.Resolution}{reason}")

    member private x.WriteDiscoveryGold(project: IProject, header: string, writeDetails: TextWriter -> unit) =
        x.WriteGold(fun writer ->
            writer.WriteLine(header)
            ExpectoTestUtil.dumpElements false writer (project.GetSolution())
            writeDetails writer)

    member private x.WriteRunGold(header: string, writeResults: TextWriter -> unit) =
        x.WriteGold(fun writer ->
            writer.WriteLine(header)
            writeResults writer)

    member x.DoDiscoveryTest(?compileProperty: string, ?outputType: ProjectOutputType) =
        let outputType = defaultArg outputType ProjectOutputType.CONSOLE_EXE
        x.WithExploredProject(compileProperty, outputType, fun project header ->
            x.WriteDiscoveryGold(project, header, ignore))

    member x.DoDiscoveryAndFileExplorationTest() =
        x.WithExploredProject(None, ProjectOutputType.CONSOLE_EXE, fun project header ->
            let exploration = ExpectoExploration(project, "Tests.fs")
            exploration.ExploreFile(exploration.RootFile) |> ignore

            x.WriteDiscoveryGold(project, header, ignore))

    member x.DoFaultedDiscoveryTest(compileProperty: string) =
        x.WithExploredProject(Some compileProperty, ProjectOutputType.CONSOLE_EXE, fun project header ->
            let loggedError = Assert.Catch(fun () -> x.ThrowLoggedExceptions(false))
            let missingAssembly = Regex.Match(loggedError.Message, "Could not load file or assembly '[^,']+")

            x.WriteDiscoveryGold(project, header, fun writer ->
                writer.WriteLine($"Logged error: {missingAssembly.Value}")))

    member x.DoRunTest(?testId: string, ?compileProperty: string, ?runSettingsFile: string,
            ?disableParallelization: bool, ?textReplacedInSource: string * string) =
        let runSettingsPath =
            match runSettingsFile with
            | Some fileName -> (x.TestDataPath / fileName).FullPath
            | None -> ""

        x.WithExploredProject(compileProperty, ProjectOutputType.CONSOLE_EXE, fun project header ->
            let solution = project.GetSolution()

            let unsavedDocument =
                match textReplacedInSource with
                | None -> null
                | Some(oldText, newText) ->
                    let exploration = ExpectoExploration(project, "Tests.fs")
                    exploration.ExploreFile(exploration.RootFile) |> ignore
                    let unsavedDocument = ExpectoTestUtil.replaceText exploration.RootFile oldText newText
                    exploration.ExploreFile(exploration.RootFile) |> ignore
                    unsavedDocument

            ExpectoTestUtil.setRunSettingsFile solution runSettingsPath
            let session, launch = ExpectoTestUtil.createLaunch project testId (defaultArg disableParallelization false)
            ExpectoTestUtil.wait (launch.Run())

            x.WriteRunGold(header, fun writer -> ExpectoTestUtil.dumpResults writer solution session)
            GC.KeepAlive(unsavedDocument))

    member x.DoRunAfterAbortedRunTest(abortedTestId: string, testIdToAbortAt: string, testIdAfterAbort: string,
            testId: string) =
        x.WithExploredProject(None, ProjectOutputType.CONSOLE_EXE, fun project header ->
            let solution = project.GetSolution()
            ExpectoTestUtil.setRunSettingsFile solution ""

            let abortedSession, abortedLaunch = ExpectoTestUtil.createLaunch project (Some abortedTestId) true
            let abortedRun = abortedLaunch.Run()

            let resultManager = solution.GetComponent<IUnitTestResultManager>()
            let getStatus (testId: string) =
                let element = ExpectoTestUtil.findElement solution testId
                resultManager.GetResult(element, abortedSession).Status

            let isRunning () = (getStatus testIdToAbortAt).Has(UnitTestStatus.Running)
            ExpectoTestUtil.waitUntil isRunning $"{testIdToAbortAt} did not start"

            let abort = Task.Run(fun () -> abortedLaunch.Abort())
            ExpectoTestUtil.wait (Task.WhenAll(abort, abortedRun))
            let hasPassedAfterAbort = (getStatus testIdAfterAbort).Has(UnitTestStatus.Success)

            let session, launch = ExpectoTestUtil.createLaunch project (Some testId) false
            ExpectoTestUtil.wait (launch.Run())

            x.WriteRunGold(header, fun writer ->
                writer.WriteLine()
                writer.WriteLine($"Aborted run: {testIdAfterAbort} passed: {hasPassedAfterAbort}")
                ExpectoTestUtil.dumpResults writer solution session))


[<FSharpDotNetCoreTest; EnsureUnitTestRepositoryIsEmpty>]
type ExpectoTestRunnerTest() =
    inherit ExpectoTestRunnerTestBase(TestTargetFramework.Net80, ".dll")

    [<Test>] member x.Discovery() = x.DoDiscoveryTest()
    [<Test>] member x.``Discovery - Library``() = x.DoDiscoveryTest(outputType = ProjectOutputType.LIBRARY)
    [<Test>] member x.``Discovery - Expecto 9``() = x.DoDiscoveryTest(compileProperty = "Expecto9")
    [<Test>] member x.``Discovery - Expecto 10``() = x.DoDiscoveryTest(compileProperty = "Expecto10")
    [<Test>] member x.``Discovery - No symbols``() = x.DoDiscoveryTest(compileProperty = "NoSymbols")
    [<Test>] member x.``Discovery - Unreadable symbols``() = x.DoDiscoveryTest(compileProperty = "UnreadableSymbols")
    [<Test>] member x.``Discovery - Missing dependency``() = x.DoFaultedDiscoveryTest("MissingDependency")
    [<Test>] member x.``Discovery - File exploration``() = x.DoDiscoveryAndFileExplorationTest()
    [<Test>] member x.``Run project``() = x.DoRunTest()
    [<Test>] member x.``Run group``() = x.DoRunTest("All.Parser")
    [<Test>] member x.``Run group - Dot in name``() = x.DoRunTest(@"Dots.Api\.V1")
    [<Test>] member x.``Run group - Test name equals group name``() = x.DoRunTest("Dots")

    [<Test>]
    member x.``Run group - Test and group with same path``() =
        x.DoRunTest("Same path", compileProperty = "SamePath")

    [<Test>] member x.``Run project - Expecto 8``() = x.DoRunTest(compileProperty = "Expecto8")
    [<Test>] member x.``Run project - Expecto 9``() = x.DoRunTest(compileProperty = "Expecto9")
    [<Test>] member x.``Run project - Expecto 10``() = x.DoRunTest(compileProperty = "Expecto10")
    [<Test>] member x.``Run project - Throwing root``() = x.DoRunTest(compileProperty = "ThrowingRoot")
    [<Test>] member x.``Run project - Focused``() = x.DoRunTest(compileProperty = "Focused")
    [<Test>] member x.``Run test - Focused project``() = x.DoRunTest("Focused.other", compileProperty = "Focused")

    member x.DoRunTestAddedInSource(?compileProperty: string) =
        let oldText = "        ptestCase \"pending\""
        let newText = "        testCase \"added\" <| fun _ -> ()\n\n" + oldText
        x.DoRunTest("All.added", ?compileProperty = compileProperty, textReplacedInSource = (oldText, newText))

    [<Test>] member x.``Run test - Added in source``() = x.DoRunTestAddedInSource()
    [<Test>] member x.``Run test - Added in source - Expecto 9``() = x.DoRunTestAddedInSource("Expecto9")
    [<Test>] member x.``Run test - Added in source - Expecto 10``() = x.DoRunTestAddedInSource("Expecto10")

    [<Test>]
    member x.``Run sequenced - Disabled parallelization``() =
        x.DoRunTest("Parallel", disableParallelization = true)

    [<Test>]
    member x.``Run sequenced - Run settings``() =
        x.DoRunTest("Parallel", runSettingsFile = "Sequenced.runsettings")

    [<Test>]
    member x.``Run sequenced - Run settings without parallel``() =
        x.DoRunTest("Parallel", runSettingsFile = "NotParallel.runsettings")

    [<Test>]
    member x.``Run sequenced - Run settings with one parallel worker``() =
        x.DoRunTest("Parallel", runSettingsFile = "OneParallelWorker.runsettings")

    [<Test>]
    member x.``Run duplicate names``() =
        x.DoRunTest("Duplicates", compileProperty = "DuplicateNames")

    [<Test>]
    member x.``Run duplicate names - Dot in name``() =
        x.DoRunTest("Dots", compileProperty = "DuplicateDotNames")

    [<Test>]
    member x.``Run group - After aborted run``() =
        x.DoRunAfterAbortedRunTest("Parallel", "Parallel.first", "Parallel.second", "All.Parser")


[<FSharpNetFramework46Test; EnsureUnitTestRepositoryIsEmpty>]
type ExpectoNetFrameworkTestRunnerTest() =
    inherit ExpectoTestRunnerTestBase(TestTargetFramework.NetFramework472, ".exe")

    [<Test>] member x.Discovery() = x.DoDiscoveryTest()
    [<Test>] member x.``Run project``() = x.DoRunTest()


[<FSharpTest; EnsureUnitTestRepositoryIsEmpty>]
type ExpectoSourceReferenceTest() =
    inherit ExpectoTestBase()

    override x.RelativeTestDataPath = "features/unitTesting/expecto"
    override val SecondProjectName = "Expecto" with get, set

    [<Test>]
    member x.``Source reference 01``() =
        let testName = x.TestName
        let fileSets = [| [| (x.TestDataPath / (testName + ".fs")).FullPath; FSharpTestUtil.referencedProjectGuid |]
                          [| (x.TestDataPath / (testName + " - Expecto.fs")).FullPath |] |]

        x.WithProjects(fileSets, fun project ->
            let configuration = project.ProjectProperties.ActiveConfigurations.Configurations.Single() :?> IManagedProjectConfiguration
            configuration.OutputType <- ProjectOutputType.CONSOLE_EXE

            let targetFrameworkId = project.GetCurrentTargetFrameworkId()
            let explorer = project.GetSolution().GetComponent<ExpectoTestExplorerFromArtifacts>()
            let support = explorer.IsSupported(project, targetFrameworkId)
            Assert.That(support.Resolution, Is.EqualTo(Pertinence.Yes)))


type ExpectoVsTestExclusionTest() =
    inherit BaseTest()

    [<Test>]
    member x.``Executor exclude list``() =
        let settingsStore = x.ShellInstance.GetComponent<ISettingsStore>()
        let excludeList = settingsStore.Schema.GetIndexedEntry<VsTestProviderSettings, string, string>(fun settings -> settings.ExecutorExcludeList)
        let executors = settingsStore.BindToContextTransient(ContextRange.ApplicationWide).EnumIndexedValues(excludeList, null).Keys

        Assert.That(executors, Does.Contain("executor://yolodev/expecto"))
        Assert.That(executors, Does.Contain("executor://NUnit3TestExecutor/"))
