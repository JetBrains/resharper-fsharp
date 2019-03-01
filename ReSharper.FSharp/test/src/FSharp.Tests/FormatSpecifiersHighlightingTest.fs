namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.FeaturesTestFramework.Daemon
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type FormatSpecifiersHighlightingTest() =
    inherit HighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/formatSpecifiersHighlighting"

    override x.CompilerIdsLanguage = FSharpLanguage.Instance :> _

    override x.GetProjectProperties(targetFrameworkIds, _) =
        FSharpProjectPropertiesFactory.CreateProjectProperties(targetFrameworkIds)

    override x.HighlightingPredicate(_, _, _) = true

    [<Test>] member x.``Bindings 01``() = x.DoNamedTest()
