namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type AddExtensionAttributeTest() =
    inherit QuickFixTestBase<AddExtensionAttributeFix>()

    override x.RelativeTestDataPath = "features/quickFixes/addExtensionAttribute"

    [<Test>] member x.``Module``() = x.DoNamedTest()
    [<Test>] member x.``Type``() = x.DoNamedTest()


[<FSharpTest>]
type AddExtensionAttributeAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/addExtensionAttribute"

    [<Test>] member x.``Root module - not available``() = x.DoNamedTest()
