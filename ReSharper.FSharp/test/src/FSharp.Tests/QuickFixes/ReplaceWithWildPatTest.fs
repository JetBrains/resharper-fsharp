namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type ReplaceWithWildPatTest() =
    inherit QuickFixTestBase<ReplaceWithWildPatFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceWithWildPat"

    [<Test>] member x.``Let - Bang 01``() = x.DoNamedTest()
    [<Test>] member x.``Let - Value 01``() = x.DoNamedTest()

    [<Test>] member x.``Param - Function 01``() = x.DoNamedTest()
    [<Test>] member x.``Param - Function 02``() = x.DoNamedTest()
    [<Test>] member x.``Param - Method 01``() = x.DoNamedTest()

    [<Test>] member x.``Match clause pat 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern 01``() = x.DoNamedTest()
    [<Test>] member x.``Lambda 01``() = x.DoNamedTest()

[<FSharpTest>]
type ReplaceWithWildPatAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/replaceWithWildPat"

    [<Test>] member x.``Not available - Ctor 01``() = x.DoNamedTest()
    [<Test>] member x.``Not available - Function 01``() = x.DoNamedTest()

    [<Test>] member x.``Not available - Let - Attribute 01``() = x.DoNamedTest()
    [<Test>] member x.``Not available - Let - Attribute 02 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Not available - Let - Attribute 03 - Typed``() = x.DoNamedTest()
    [<Test>] member x.``Not available - Let - Attribute 04 - Typed``() = x.DoNamedTest()
