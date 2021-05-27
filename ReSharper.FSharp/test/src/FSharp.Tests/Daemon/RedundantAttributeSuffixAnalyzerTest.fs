namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon
open NUnit.Framework

type RedundantAttributeAnalyzerTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/redundantAttributeSuffix"

    override x.HighlightingPredicate(highlighting, _, _) =
        highlighting :? RedundantAttributeSuffixWarning

    [<Test>] member x.``Single attribute 01 - Redundant suffix``() = x.DoNamedTest()
    [<Test>] member x.``Single attribute 02 - Needed suffix``() = x.DoNamedTest()
    [<Test>] member x.``Single attribute 03 - With constructor``() = x.DoNamedTest()
    [<Test>] member x.``Single attribute 04 - With target``() = x.DoNamedTest()
    [<Test>] member x.``Single attribute 05 - Name is just Attribute``() = x.DoNamedTest()
