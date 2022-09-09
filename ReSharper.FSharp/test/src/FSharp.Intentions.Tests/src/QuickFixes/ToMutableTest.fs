namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

// todo: add test with signature files

[<FSharpTest>]
type ToMutableFixTest() =
    inherit FSharpQuickFixTestBase<ToMutableFix>()

    override x.RelativeTestDataPath = "features/quickFixes/toMutable"

    [<Test>] member x.``Record field 01``() = x.DoNamedTest()
    [<Test>] member x.``Record field 02 - Attributes``() = x.DoNamedTest()

    [<Test>] member x.``Val - Local 01``() = x.DoNamedTest()
    [<Test>] member x.``Val - Top level 01``() = x.DoNamedTest()

    [<Test; Explicit>] member x.``Val - Parameter pattern 01 - Union case param``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Val - Parameter pattern 02 - Function param``() = x.DoNamedTest()
    [<Test>] member x.``Val - Parameter pattern 03 - Typed``() = x.DoNamedTest()

    [<Test>] member x.TopAsPat() = x.DoNamedTest()
    [<Test>] member x.LocalAsPat() = x.DoNamedTest()

    [<Test; NotAvailable>] member x.``LocalAsPat - Pattern matching, not available``() = x.DoNamedTest()


[<FSharpTest>]
type ToMutableFixAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/toMutable"

    [<Test>] member x.``Availability - Name 01``() = x.DoNamedTest()
    [<Test>] member x.``Availability - Name 02 - Parens``() = x.DoNamedTest()
