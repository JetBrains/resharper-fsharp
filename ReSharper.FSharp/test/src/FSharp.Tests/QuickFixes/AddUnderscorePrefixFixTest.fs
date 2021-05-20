namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type AddUnderscorePrefixFixTest() =
    inherit FSharpQuickFixTestBase<AddUnderscorePrefixFix>()

    override x.RelativeTestDataPath = "features/quickFixes/addUnderscorePrefix"

    [<Test>] member x.``As 01``() = x.DoNamedTest()
    [<Test>] member x.``Let 01``() = x.DoNamedTest()

    [<Test>] member x.``Partial pattern 01``() = x.DoNamedTest()
    [<Test>] member x.``Partial pattern 02``() = x.DoNamedTest()

    [<Test; NotAvailable>] member x.``Partial pattern - Escaped 01``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Partial pattern - Escaped 02``() = x.DoNamedTest()


[<FSharpTest>]
type AddUnderscorePrefixFixAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/addUnderscorePrefix"

    [<Test>] member x.``Availability 01 - Escaped name``() = x.DoNamedTest()
    [<Test>] member x.``Availability 02 - Params``() = x.DoNamedTest()
    [<Test>] member x.``Availability 03 - As``() = x.DoNamedTest()
