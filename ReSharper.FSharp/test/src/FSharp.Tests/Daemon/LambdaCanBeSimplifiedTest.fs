namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestPackages("FSharp.Core")>]
type LambdaCanBeSimplifiedTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/lambdaCanBeSimplified"

    override x.HighlightingPredicate(highlighting, _, _) =
        highlighting :? LambdaCanBeSimplifiedWarning

    [<Test>] member x.``Application``() = x.DoNamedTest()
    [<Test>] member x.``Partial application``() = x.DoNamedTest()
    [<Test>] member x.``Id``() = x.DoNamedTest()
    [<Test>] member x.``Not available``() = x.DoNamedTest()
