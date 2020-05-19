namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests.Common
open NUnit.Framework

[<FSharpTest>]
[<ExpectErrors 588>]
type ReplaceLetWithExpressionTest() =
    inherit QuickFixTestBase<ReplaceLetWithExpressionFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceLetWithExpression"

    [<Test>] member x.``Inside other 01 - If``() = x.DoNamedTest()
    [<Test>] member x.``Simple 01``() = x.DoNamedTest()
    [<Test>] member x.``Use 01``() = x.DoNamedTest()

    [<Test>] member x.``In 01``() = x.DoNamedTest()
    [<Test>] member x.``In 02 - Inline``() = x.DoNamedTest()
    [<Test>] member x.``In 03 - Before other``() = x.DoNamedTest()
    

[<FSharpTest>]
[<ExpectErrors 588>]
type ReplaceLetWithExpressionAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/replaceLetWithExpression"

    [<Test>] member x.``Text - Let 01``() = x.DoNamedTest()
    [<Test>] member x.``Text - Use 01``() = x.DoNamedTest()
