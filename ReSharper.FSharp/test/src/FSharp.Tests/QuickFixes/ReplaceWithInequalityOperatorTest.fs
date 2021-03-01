namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features
open NUnit.Framework

[<FSharpTest>]
type ReplaceWithInequalityOperatorTest() =
    inherit FSharpQuickFixTestBase<ReplaceWithInequalityOperatorFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceWithInequalityOperator"

    [<Test>] member x.``Simple 01``() = x.DoNamedTest()


[<FSharpTest>]
type ReplaceWithInequalityOperatorAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/replaceWithInequalityOperator"

    [<Test>] member x.``Text``() = x.DoNamedTest()
