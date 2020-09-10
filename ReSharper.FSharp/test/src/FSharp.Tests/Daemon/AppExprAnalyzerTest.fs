namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestPackages("FSharp.Core")>]
type AppExprAnalyzerTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/appExpr"

    override x.HighlightingPredicate(highlighting, _, _) =
        highlighting :? RedundantApplicationWarning

    [<Test>] member x.``Id 01``() = x.DoNamedTest()
    [<Test>] member x.``Id 02 - Shadowed``() = x.DoNamedTest()

    [<Test>] member x.``Ignore 01``() = x.DoNamedTest()
