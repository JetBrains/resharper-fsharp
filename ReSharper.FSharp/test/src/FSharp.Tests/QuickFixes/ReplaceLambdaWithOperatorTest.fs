namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

type ReplaceLambdaWithOperatorTest() =
    inherit FSharpQuickFixTestBase<ReplaceLambdaWithOperatorFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceLambdaWithOperator"

    [<Test>] member x.``Id 1``() = x.DoNamedTest()
    [<Test>] member x.``Id 2 - add whitespaces``() = x.DoNamedTest()

    [<Test>] member x.``Fst 1``() = x.DoNamedTest()
    [<Test>] member x.``Fst 2 - tuple``() = x.DoNamedTest()

    [<Test>] member x.``Snd``() = x.DoNamedTest()

    [<Test; NotAvailable>] member x.``Id - Names collision - not available``() = x.DoNamedTest()


[<FSharpTest>]
type ReplaceLambdaWithOperatorAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/replaceLambdaWithOperator"

    [<Test>] member x.``Text - Replace lambda with id``() = x.DoNamedTest()
    [<Test>] member x.``Text - Replace lambda with fst``() = x.DoNamedTest()
    [<Test>] member x.``Text - Replace lambda with snd``() = x.DoNamedTest()
