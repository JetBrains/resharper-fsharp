namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

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
    [<Test>] member x.``Simple 03 - Add space``() = x.DoNamedTest()

    [<Test; NoHighlightingFound>] member x.``Function 01 - String, not available``() = x.DoNamedTest()
    [<Test; NoHighlightingFound>] member x.``Function 03 - String, not available, anon module``() = x.DoNamedTest()


[<FSharpTest>]
type RemoveRedundantNewAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/removeRedundantNew"

    [<Test>] member x.``Function 02 - Qualified name``() = x.DoNamedTest()

    [<Test>] member x.``Delegate 01 - Partially shadowed``() = x.DoNamedTest()

    [<Test>] member x.``String 01 - Type``() = x.DoNamedTest()
    [<Test>] member x.``String 02 - Type, qualified``() = x.DoNamedTest()
    [<Test>] member x.``String 03 - Redefined type``() = x.DoNamedTest()

    [<Test>] member x.``Type parameters 01``() = x.DoNamedTest()
    [<Test>] member x.``Type parameters 02``() = x.DoNamedTest()
