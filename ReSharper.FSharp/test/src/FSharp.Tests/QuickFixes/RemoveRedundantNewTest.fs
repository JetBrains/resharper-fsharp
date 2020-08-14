namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type RemoveRedundantNewTest() =
    inherit FSharpQuickFixTestBase<RemoveRedundantNewFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeRedundantNew"

    [<Test>] member x.``Simple 01``() = x.DoNamedTest()
    [<Test>] member x.``Simple 02 - Type args``() = x.DoNamedTest()

    [<Test; NoHighlightingFound>] member x.``Function 01 - String, not available``() = x.DoNamedTest()


[<FSharpTest>]
type RemoveRedundantNewAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/removeRedundantNew"

    [<Test>] member x.``Function 02 - Qualified name``() = x.DoNamedTest()

    [<Test>] member x.``String 01 - Type``() = x.DoNamedTest()
    [<Test>] member x.``String 02 - Type, qualified``() = x.DoNamedTest()
    [<Test>] member x.``String 03 - Redefined type``() = x.DoNamedTest()
