namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

type ReplaceExpressionWithOperatorTest() =
    inherit FSharpQuickFixTestBase<ReplaceLambdaWithOperatorFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceExpressionWithOperator"

    [<Test>] member x.``Id - Lambda 1``() = x.DoNamedTest()
    [<Test>] member x.``Id - Lambda 2 - add whitespaces``() = x.DoNamedTest()
    [<Test>] member x.``Id - Lambda body 1``() = x.DoNamedTest()
    [<Test>] member x.``Id - Lambda body 2 - add whitespace``() = x.DoNamedTest()

    [<Test>] member x.``Fst - Lambda 1``() = x.DoNamedTest()
    [<Test>] member x.``Fst - Lambda 2 - tuple``() = x.DoNamedTest()
    
    [<Test>] member x.``Snd - Lambda 1``() = x.DoNamedTest()
    [<Test>] member x.``Snd - Lambda 2 - tuple``() = x.DoNamedTest()

    [<Test; NotAvailable>] member x.``Id - Lambda - Names collision - not available 1``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Id - Lambda - Names collision - not available 2``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Fst - Lambda - Names collision - not available``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Snd - Lambda - Names collision - not available``() = x.DoNamedTest()


[<FSharpTest>]
type ReplaceExpressionWithOperatorAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/replaceExpressionWithOperator"

    [<Test>] member x.``Text - Replace lambda with id``() = x.DoNamedTest()
    [<Test>] member x.``Text - Replace lambda body with id``() = x.DoNamedTest()
    [<Test>] member x.``Text - Replace lambda with fst``() = x.DoNamedTest()
    [<Test>] member x.``Text - Replace lambda with snd``() = x.DoNamedTest()
