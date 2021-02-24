namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

type ReplaceLambdaWithInnerExpressionTest() =
    inherit FSharpQuickFixTestBase<ReplaceLambdaWithInnerExpressionFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceLambdaWithInnerExpression"

    [<Test>] member x.``Reference``() = x.DoNamedTest()
    [<Test>] member x.``Partial application``() = x.DoNamedTest()
    [<Test>] member x.``Inner lambda``() = x.DoNamedTest()
    [<Test>] member x.``Need parens``() = x.DoNamedTest()
    [<Test>] member x.``Multiline``() = x.DoNamedTest()


[<FSharpTest>]
type ReplaceLambdaWithInnerExpressionAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/replaceLambdaWithInnerExpression"

    [<Test>] member x.``Text - Replace lambda with reference``() = x.DoNamedTest()
    [<Test>] member x.``Text - Replace lambda with qualified reference``() = x.DoNamedTest()
    [<Test>] member x.``Text - Replace with reference partial application``() = x.DoNamedTest()
    [<Test>] member x.``Text - Replace with operator partial application``() = x.DoNamedTest()
    [<Test>] member x.``Text - Replace lambda with partial application``() = x.DoNamedTest()
    [<Test>] member x.``Text - Simplify lambda 1``() = x.DoNamedTest()
    [<Test>] member x.``Text - Simplify lambda 2``() = x.DoNamedTest()
    [<Test>] member x.``Text - Simplify lambda 3``() = x.DoNamedTest()
