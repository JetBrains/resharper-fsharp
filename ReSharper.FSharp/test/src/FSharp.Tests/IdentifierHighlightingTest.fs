namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.FeaturesTestFramework.Daemon
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type IdentifierHighlightingTest() =
    inherit HighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/identifierHighlighting"

    override x.CompilerIdsLanguage = FSharpLanguage.Instance :> _

    override x.GetProjectProperties(targetFrameworkIds, _) =
        FSharpProjectPropertiesFactory.CreateProjectProperties(targetFrameworkIds)

    override x.HighlightingPredicate(_, _, _) = true

    [<Test>] member x.``Backticks 01``() = x.DoNamedTest()

    [<Test>] member x.``Operators 01 - ==``() = x.DoNamedTest()
    [<Test>] member x.``Operators 02 - Custom``() = x.DoNamedTest()

    [<Test>] member x.``Active pattern decl``() = x.DoNamedTest()

    [<Test>] member x.``Delegates``() = x.DoNamedTest()
