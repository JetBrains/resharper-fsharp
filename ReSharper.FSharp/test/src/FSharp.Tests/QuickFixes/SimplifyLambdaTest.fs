namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

type SimplifyLambdaTest() =
    inherit FSharpQuickFixTestBase<SimplifyLambdaFix>()

    override x.RelativeTestDataPath = "features/quickFixes/simplifyLambda"

    [<Test>] member x.``Reference``() = x.DoNamedTest()
    [<Test>] member x.``Partial application 1``() = x.DoNamedTest()
    [<Test>] member x.``Partial application 2``() = x.DoNamedTest()
    [<Test>] member x.``Multiline pats``() = x.DoNamedTest()
    [<Test>] member x.``Multiline body``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Non application - not available``() = x.DoNamedTest()


[<FSharpTest>]
type SimplifyLambdaAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/simplifyLambda"

    [<Test>] member x.``Text - Simplify lambda``() = x.DoNamedTest()
