namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type ToRecursiveFunctionTest() =
    inherit FSharpQuickFixTestBase<ToRecursiveFunctionFix>()

    override x.RelativeTestDataPath = "features/quickFixes/toRecursiveFunction"

    [<Test>] member x.``Top level 01``() = x.DoNamedTest()
    [<Test>] member x.``Top level 02 - Alignment``() = x.DoNamedTest()
    [<Test>] member x.``Local 01``() = x.DoNamedTest()

[<FSharpTest>]
type ToRecursiveFunctionAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/toRecursiveFunction"

    [<Test>] member x.``Availability - Function 01``() = x.DoNamedTest()
