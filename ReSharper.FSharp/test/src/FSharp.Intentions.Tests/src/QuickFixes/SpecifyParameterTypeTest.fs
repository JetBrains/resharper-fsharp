namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type SpecifyParameterTypeTest() =
    inherit FSharpQuickFixTestBase<SpecifyParameterTypeFix>()

    override x.RelativeTestDataPath = "features/quickFixes/specifyType"

    [<Test>] member x.``Indexer 01``() = x.DoNamedTest()

    [<Test>] member x.``Ref 01``() = x.DoNamedTest()
    [<Test>] member x.``Ref 02 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Ref 03 - Parens``() = x.DoNamedTest()

    [<Test>] member x.``Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 02 - Parens``() = x.DoNamedTest()
    [<Test>] member x.``Tuple 03 - Parens``() = x.DoNamedTest()

    [<Test>] member x.``Constructor 01``() = x.DoNamedTest()
    [<Test>] member x.``Method 01``() = x.DoNamedTest()

    [<Test>] member x.``Qualified type 01 - Rqa``() = x.DoNamedTest()
    [<Test>] member x.``Qualified type 02 - Not imported``() = x.DoNamedTest()


[<FSharpTest>]
type SpecifyPropertyTypeTest() =
    inherit FSharpQuickFixTestBase<SpecifyPropertyTypeFix>()

    override x.RelativeTestDataPath = "features/quickFixes/specifyType"

    [<Test>] member x.``Property 01``() = x.DoNamedTest()


[<FSharpTest>]
type SpecifyTypeFixAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/specifyType"

    [<Test>] member x.``Text 01``() = x.DoNamedTest()


[<FSharpTest>]
type SpecifyParameterBaseTypeTest() =
    inherit FSharpQuickFixTestBase<SpecifyParameterBaseTypeFix>()

    override x.RelativeTestDataPath = "features/quickFixes/specifyBaseType"

    [<Test>] member x.``Expr 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr 03``() = x.DoNamedTest()
    [<Test>] member x.``Expr 04``() = x.DoNamedTest()
    [<Test>] member x.``Param 01``() = x.DoNamedTest()
    [<Test>] member x.``Param 02 - No base interface``() = x.DoNamedTest()
    [<Test>] member x.``Param 03 - Base type``() = x.DoNamedTest()
    [<Test>] member x.``Param 04 - Base type``() = x.DoNamedTest()
    [<Test>] member x.``Param 05``() = x.DoNamedTest()
    [<Test>] member x.``Param 06``() = x.DoNamedTest()
