namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type ReplaceLetWithExpressionTest() =
    inherit FSharpQuickFixTestBase<ReplaceLetWithExpressionFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceLetWithExpression"

    [<Test>] member x.``Inside other 01 - If``() = x.DoNamedTest()
    [<Test>] member x.``Simple 01``() = x.DoNamedTest()
    [<Test>] member x.``Use 01``() = x.DoNamedTest()

    [<Test>] member x.``In 01``() = x.DoNamedTest()
    [<Test>] member x.``In 02 - Inline``() = x.DoNamedTest()
    [<Test>] member x.``In 03 - Before other``() = x.DoNamedTest()


[<FSharpTest>]
type ReplaceLetWithExpressionAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/replaceLetWithExpression"

    [<Test>] member x.``Text - Let 01``() = x.DoNamedTest()
    [<Test>] member x.``Text - Use 01``() = x.DoNamedTest()
