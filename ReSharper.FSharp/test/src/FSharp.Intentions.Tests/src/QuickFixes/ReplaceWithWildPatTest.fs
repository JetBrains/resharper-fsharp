namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type ReplaceWithWildPatTest() =
    inherit FSharpQuickFixTestBase<ReplaceWithWildPatFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceWithWildPat"

    [<Test>] member x.``For - Scoped 01``() = x.DoNamedTest()
    [<Test>] member x.``For 01``() = x.DoNamedTest()
    [<Test>] member x.``For 02 - Tuple``() = x.DoNamedTest()
    [<Test>] member x.``For 03 - Param``() = x.DoNamedTest()

    [<Test>] member x.``Let - Bang 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Value 01``() = x.DoNamedTest()

    [<Test>] member x.``Param - Function 01``() = x.DoNamedTest()
    [<Test>] member x.``Param - Function 02``() = x.DoNamedTest()
    [<Test>] member x.``Param - Method 01``() = x.DoNamedTest()

    [<Test>] member x.``Match clause pat 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern 01``() = x.DoNamedTest()
    [<Test>] member x.``Lambda 01``() = x.DoNamedTest()

    [<Test>] member x.``Parameters list - lambda``() = x.DoNamedTest()
    [<Test>] member x.``Parameters list - method``() = x.DoNamedTest()
    [<Test>] member x.``Parameters list - parameter with attribute``() = x.DoNamedTest()
    [<Test>] member x.``Parameters list - typed parameter with attribute``() = x.DoNamedTest()
    [<Test>] member x.``Match clause pattern - parameters owner pat``() = x.DoNamedTest()
    [<Test>] member x.``Match clause pattern - simple pat``() = x.DoNamedTest()
    [<Test>] member x.``Binding pattern``() = x.DoNamedTest()

    [<Test>] member x.``Partial pattern 01``() = x.DoNamedTest()
    [<Test>] member x.``Partial pattern 02``() = x.DoNamedTest()

    [<Test; ExecuteScopedActionInFile>] member x.``Whole file``() = x.DoNamedTest()

    [<Test; NotAvailable>] member x.``Not available - As 01``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available - Ctor 01``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available - Let - Attribute 01``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available - Let - Attribute 02 - Parens``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available - Let - Attribute 03 - Typed``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available - Let - Attribute 04 - Typed``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available - Member - Optional 01``() = x.DoNamedTest()

[<FSharpTest>]
type ReplaceWithWildPatAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/replaceWithWildPat"

    [<Test>] member x.``Availability - Parameters list - lambda``() = x.DoNamedTest()
    [<Test>] member x.``Availability - Parameters list - method``() = x.DoNamedTest()
    [<Test>] member x.``Availability - Match clause pattern - parameters owner pat``() = x.DoNamedTest()
    [<Test>] member x.``Availability - Match clause pattern - simple pat``() = x.DoNamedTest()
    [<Test>] member x.``Availability - Binding pattern``() = x.DoNamedTest()
