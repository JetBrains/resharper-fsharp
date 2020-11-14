namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

type ReplaceLambdaBodyWithIdTest() =
    inherit FSharpQuickFixTestBase<ReplaceLambdaBodyWithIdFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceLambdaBodyWithId"

    [<Test>] member x.``Lambda body 1``() = x.DoNamedTest()
    [<Test>] member x.``Lambda body 2 - add whitespace``() = x.DoNamedTest()

    [<Test; NotAvailable>] member x.``Names collision - not available``() = x.DoNamedTest()


[<FSharpTest>]
type ReplaceLambdaBodyWithIdAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/replaceLambdaBodyWithId"

    [<Test>] member x.``Text - Replace lambda body with id``() = x.DoNamedTest()

