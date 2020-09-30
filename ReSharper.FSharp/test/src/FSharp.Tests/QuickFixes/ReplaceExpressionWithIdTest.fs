namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

type ReplaceExpressionWithIdTest() =
    inherit FSharpQuickFixTestBase<ReplaceExpressionWithIdFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceExpressionWithId"

    [<Test>] member x.``Lambda 1``() = x.DoNamedTest()
    [<Test>] member x.``Lambda 2 - add whitespaces``() = x.DoNamedTest()
    [<Test>] member x.``Lambda body 1``() = x.DoNamedTest()
    [<Test>] member x.``Lambda body 2 - add whitespace``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Lambda - Names collision - not available 1``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Lambda - Names collision - not available 2``() = x.DoNamedTest()


[<FSharpTest>]
type ReplaceExpressionWithIdAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/replaceExpressionWithId"

    [<Test>] member x.``Text - Replace lambda with id``() = x.DoNamedTest()
    [<Test>] member x.``Text - Replace lambda body with id``() = x.DoNamedTest()
