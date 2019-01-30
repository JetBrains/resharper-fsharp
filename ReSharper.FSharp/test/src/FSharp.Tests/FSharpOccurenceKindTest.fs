module JetBrains.ReSharper.Plugins.FSharp.Tests.Features.FindUsages

open JetBrains.ReSharper.FeaturesTestFramework.Occurrences
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type FSharpOccuerenceKindTest() =
    inherit OccurrenceKindTestBase()

    override x.RelativeTestDataPath = "features/findUsages/occurenceKinds"

    override x.GetProjectProperties(targetFrameworkIds, flavours) =
        FSharpProjectPropertiesFactory.CreateProjectProperties(targetFrameworkIds)

    [<Test>] member x.``Unions 01``() = x.DoNamedTest()
    [<Test>] member x.``Type arguments 01``() = x.DoNamedTest()
