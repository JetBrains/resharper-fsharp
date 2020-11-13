namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<FSharpTest; TestPackages(FSharpCorePackage)>]
type AddUnderscorePrefixFixTest() =
    inherit FSharpQuickFixTestBase<AddUnderscorePrefixFix>()

    override x.RelativeTestDataPath = "features/quickFixes/addUnderscorePrefix"

    [<Test>] member x.``Let 01``() = x.DoNamedTest()


[<FSharpTest>]
type AddUnderscorePrefixFixAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/addUnderscorePrefix"

    [<Test>] member x.``Availability 01 - Escaped name``() = x.DoNamedTest()
    [<Test>] member x.``Availability 02 - Params``() = x.DoNamedTest()
