namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

type ReplaceLambdaTest() =
    inherit FSharpQuickFixTestBase<ReplaceLambdaFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceLambda"

    [<Test>] member x.``Reference``() = x.DoNamedTest()
    [<Test>] member x.``Partial application``() = x.DoNamedTest()
    [<Test>] member x.``Inner lambda``() = x.DoNamedTest()
    [<Test>] member x.``Need parens``() = x.DoNamedTest()
    [<Test>] member x.``Multiline``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Non application - not available``() = x.DoNamedTest()


[<FSharpTest>]
type ReplaceLambdaAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/replaceLambda"

    [<Test>] member x.``Text - Replace lambda with reference``() = x.DoNamedTest()
    [<Test>] member x.``Text - Replace lambda with partial application``() = x.DoNamedTest()
    [<Test>] member x.``Text - Simplify lambda``() = x.DoNamedTest()
