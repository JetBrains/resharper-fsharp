namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open System
open System.IO
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.FeaturesTestFramework.Daemon
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Files
open JetBrains.ReSharper.Psi.Resolve
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestPackages("FSharp.Core")>]
type CSharpResolveTest() =
    inherit TestWithTwoProjects()

    let highlightingManager = HighlightingSettingsManager.Instance

    override x.GetProjectProperties(targetFrameworkIds, flavours) =
        FSharpProjectPropertiesFactory.CreateProjectProperties(targetFrameworkIds)

    [<Test>] member x.``Records 01 - Generated members``() = x.DoNamedTest()
    [<Test>] member x.``Records 02 - CliMutable``() = x.DoNamedTest()
    [<Test>] member x.``Records 03 - Override generated members``() = x.DoNamedTest()
    [<Test>] member x.``Records 04 - Sealed``() = x.DoNamedTest()
    [<Test>] member x.``Records 05 - Struct``() = x.DoNamedTest()
    [<Test>] member x.``Records 06 - Struct CliMutable``() = x.DoNamedTest()
    [<Test>] member x.``Records 07 - Field compiled name ignored``() = x.DoNamedTest()

    [<Test>] member x.``Exceptions 01 - Empty``() = x.DoNamedTest()
    [<Test>] member x.``Exceptions 02 - Single field``() = x.DoNamedTest()
    [<Test>] member x.``Exceptions 03 - Multiple fields``() = x.DoNamedTest()
    [<Test>] member x.``Exceptions 04 - Protected ctor``() = x.DoNamedTest()

    [<Test>] member x.``Unions 01 - Simple generated members``() = x.DoNamedTest()
    [<Test>] member x.``Unions 02 - Singletons``() = x.DoNamedTest()
    [<Test>] member x.``Unions 03 - Nested types``() = x.DoNamedTest()
    [<Test>] member x.``Unions 04 - Single case with fields``() = x.DoNamedTest()
    [<Test>] member x.``Unions 05 - Struct single case with fields``() = x.DoNamedTest()
    [<Test>] member x.``Unions 06 - Struct nested types``() = x.DoNamedTest()
    [<Test>] member x.``Unions 07 - Private representation 01, singletons``() = x.DoNamedTest()
    [<Test>] member x.``Unions 08 - Private representation 02, nested types``() = x.DoNamedTest()
    [<Test>] member x.``Unions 09 - Private representation 03, struct``() = x.DoNamedTest()
    [<Test>] member x.``Unions 10 - Case compiled name ignored``() = x.DoNamedTest()

    [<Test>] member x.``Simple types 01 - Members``() = x.DoNamedTest()

    [<Test>] member x.``Class 01 - Abstract``() = x.DoNamedTest()
    [<Test>] member x.``Class 02 - Sealed``() = x.DoNamedTest()

    [<Test>] member x.``Val fields 01``() = x.DoNamedTest()
    [<Test>] member x.``Val fields 02, compiled name ignored``() = x.DoNamedTest()
    [<Test>] member x.``Val fields 03, struct``() = x.DoNamedTest()

    [<Test>] member x.``Auto properties 01``() = x.DoNamedTest()
    [<Test>] member x.``Auto properties 02, compiled name``() = x.DoNamedTest()

    [<Test>] member x.``Methods 01``() = x.DoNamedTest()
    [<Test>] member x.``Methods 02, compiled name``() = x.DoNamedTest()
    [<Test>] member x.``Methods 03, optional param``() = x.DoNamedTest()
    [<Test>] member x.``Methods 04, extension methods``() = x.DoNamedTest()

    [<Test>] member x.``Properties 01``() = x.DoNamedTest()

    [<Test>] member x.``Module bindings 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Module bindings 02 - Records``() = x.DoNamedTest()
    [<Test>] member x.``Module bindings 03 - extension methods 01``() = x.DoNamedTest()
    [<Test>] member x.``Module bindings 04 - extension methods 02``() = x.DoNamedTest()

    [<Test>] member x.``Operators 01 - Module``() = x.DoNamedTest()
    [<Test>] member x.``Operators 02 - Type``() = x.DoNamedTest()
    [<Test>] member x.``Operators 03 - Greater, Less``() = x.DoNamedTest()
    [<Test>] member x.``Operators 04 - Implicit, Explicit``() = x.DoNamedTest()
    [<Test>] member x.``Operators 05 - Equals``() = x.DoNamedTest()

    [<Test>] member x.``Enum 01``() = x.DoNamedTest()

    [<Test>] member x.``Events 01``() = x.DoNamedTest()

    override x.RelativeTestDataPath = "cache/csharpResolve"

    override x.MainFileExtension = CSharpProjectFileType.CS_EXTENSION
    override x.SecondFileExtension = FSharpProjectFileType.FsExtension

    override x.DoTest(project: IProject, secondProject: IProject) =
        x.Solution.GetPsiServices().Files.CommitAllDocuments()
        x.ExecuteWithGold(fun writer ->
            let projectFile = project.GetAllProjectFiles() |> Seq.exactlyOne
            let sourceFile = projectFile.ToSourceFiles().Single()
            let psiFile = sourceFile.GetPrimaryPsiFile()

            let daemon = TestHighlightingDumper(sourceFile, writer, null, Func<_,_,_,_>(x.ShouldHighlight))
            daemon.DoHighlighting(DaemonProcessKind.VISIBLE_DOCUMENT)
            daemon.Dump()

            let referenceProcessor = RecursiveReferenceProcessor(fun r -> x.ProcessReference(r, writer))
            psiFile.ProcessThisAndDescendants(referenceProcessor)) |> ignore

    member x.ShouldHighlight highlighting sourceFile settings =
        let severity = highlightingManager.GetSeverity(highlighting, sourceFile, x.Solution, settings)
        severity = Severity.ERROR

    member x.ProcessReference(reference: IReference, writer: TextWriter) =
        match reference.Resolve().DeclaredElement with
        | :? IFSharpTypeMember as typeMember -> writer.WriteLine(typeMember.XMLDocId)
        | _ -> ()
