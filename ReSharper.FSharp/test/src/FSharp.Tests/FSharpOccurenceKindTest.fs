module JetBrains.ReSharper.Plugins.FSharp.Tests.Features.FindUsages

open JetBrains.ReSharper.FeaturesTestFramework.Occurrences
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type FSharpOccurenceKindTest() =
    inherit OccurrenceKindTestBase()

    override x.RelativeTestDataPath = "features/findUsages/occurenceKinds"

    override x.GetProjectProperties(targetFrameworkIds, _) =
        FSharpProjectPropertiesFactory.CreateProjectProperties(targetFrameworkIds)

    [<Test>] member x.``Import 01``() = x.DoNamedTest()
    [<Test>] member x.``Unions 01``() = x.DoNamedTest()

    [<Test>] member x.``Base Type 01``() = x.DoNamedTest()
    [<Test>] member x.``Base Type 02``() = x.DoNamedTest()

    [<Test>] member x.``Base Type - Object expressions 01 - Class``() = x.DoNamedTest()
    [<Test>] member x.``Base Type - Object expressions 02 - Interface``() = x.DoNamedTest()
    [<Test>] member x.``Base Type - Object expressions 03 - Secondary interfaces``() = x.DoNamedTest()

    [<Test>] member x.``Type Argument 01``() = x.DoNamedTest()
    [<Test>] member x.``Type Cast 01``() = x.DoNamedTest()
    [<Test>] member x.``Type Test 01``() = x.DoNamedTest()

    [<Test>] member x.``Write 01 - Mutable binding``() = x.DoNamedTest()
    [<Test>] member x.``Write 02 - Val field``() = x.DoNamedTest()
    [<Test>] member x.``Write 03 - Record fields``() = x.DoNamedTest()

    [<Test>] member x.``Type Extension 01``() = x.DoNamedTest()
