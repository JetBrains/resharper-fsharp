namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestPackages("FSharp.Core")>]
type RedundantParenPatTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/redundantParens/pat"

    override x.HighlightingPredicate(highlighting, _, _) =
        highlighting :? RedundantParenPatWarning

    [<Test>] member x.``Pattern param 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern param 02 - Nested``() = x.DoNamedTest()

    [<Test>] member x.``Value 01``() = x.DoNamedTest()
    [<Test>] member x.``Function param 01``() = x.DoNamedTest()
