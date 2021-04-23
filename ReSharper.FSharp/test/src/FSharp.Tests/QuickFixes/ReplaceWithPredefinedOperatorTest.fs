namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features
open NUnit.Framework

[<FSharpTest>]
type ReplaceWithPredefinedOperatorTest() =
    inherit FSharpQuickFixTestBase<ReplaceWithPredefinedOperatorFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceWithPredefinedOperator"

    [<Test>] member x.Equality() = x.DoNamedTest()
    [<Test>] member x.Inequality() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Prefix form``() = x.DoNamedTest()


[<FSharpTest>]
type ReplaceWithPredefinedOperatorAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/replaceWithPredefinedOperator"

    [<Test>] member x.``Text - Equality``() = x.DoNamedTest()
    [<Test>] member x.``Text - Inequality``() = x.DoNamedTest()
