namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type AddParensToTypeTestTest() =
    inherit FSharpQuickFixTestBase<AddParensToTypeTestFix>()

    override x.RelativeTestDataPath = "features/quickFixes/addParensToTypeTest"

    [<Test>] member x.``Simple 01``() = x.DoNamedTest()


[<FSharpTest>]
type AddParensToTypeTestAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/addParensToTypeTest"

    [<Test>] member x.``Add parens to type test``() = x.DoNamedTest()
