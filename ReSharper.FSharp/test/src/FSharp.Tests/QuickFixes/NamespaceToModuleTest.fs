namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type NamespaceToModuleTest() =
    inherit QuickFixTestBase<NamespaceToModuleFix>()

    override x.RelativeTestDataPath = "features/quickFixes/namespaceToModule"

    [<Test>] member x.``Simple``() = x.DoNamedTest()


[<FSharpTest>]
type NamespaceToModuleAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/namespaceToModule"

    [<Test>] member x.``Global namespace - not available``() = x.DoNamedTest()
