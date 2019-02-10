namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.FeaturesTestFramework.ContextHighlighters
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type UsageHighlightingTest() =
    inherit ContextHighlighterTestBase()

    override x.RelativeTestDataPath = "features/daemon/usageHighlighting"
    override x.ExtraPath = null

    override x.GetProjectProperties(targetFrameworkIds, flavours) =
        FSharpProjectPropertiesFactory.CreateProjectProperties(targetFrameworkIds)

    [<Test>] member x.``Active pattern decl``() = x.DoNamedTest()
