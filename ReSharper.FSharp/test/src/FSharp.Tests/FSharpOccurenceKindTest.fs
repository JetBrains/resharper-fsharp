module JetBrains.ReSharper.Plugins.FSharp.Tests.Features.FindUsages

open JetBrains.ReSharper.FeaturesTestFramework.Occurrences
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type FSharpOccurenceKindTest() =
    inherit OccurrenceKindTestBase()

    override x.RelativeTestDataPath = "features/findUsages/occurenceKinds"

    override x.GetProjectProperties(targetFrameworkIds, flavours) =
        FSharpProjectPropertiesFactory.CreateProjectProperties(targetFrameworkIds)

    [<Test>] member x.``Import 01``() = x.DoNamedTest()
    [<Test>] member x.``Unions 01``() = x.DoNamedTest()
    [<Test>] member x.``Type Argument 01``() = x.DoNamedTest()
    [<Test>] member x.``Type Cast 01``() = x.DoNamedTest()
    [<Test>] member x.``Type Test 01``() = x.DoNamedTest()
