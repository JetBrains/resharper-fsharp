namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

type NamespaceToModuleTest() =
    inherit FSharpQuickFixTestBase<NamespaceToModuleFix>()

    override x.RelativeTestDataPath = "features/quickFixes/namespaceToModule"

    [<Test>] member x.``Binding 01``() = x.DoNamedTest()
    [<Test>] member x.``Binding 02``() = x.DoNamedTest()
    [<Test>] member x.``Binding 03``() = x.DoNamedTest()
    [<Test>] member x.``Expression 01``() = x.DoNamedTest()
    [<Test>] member x.``Expression 02 - do``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Global namespace``() = x.DoNamedTest()


[<FSharpTest>]
type NamespaceToModuleAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/namespaceToModule"

    [<Test>] member x.``Availability - Binding 01``() = x.DoNamedTest()
    [<Test>] member x.``Availability - Binding 02``() = x.DoNamedTest()
    [<Test>] member x.``Availability - Binding 03``() = x.DoNamedTest()
    [<Test>] member x.``Availability - Expression 01``() = x.DoNamedTest()
    [<Test>] member x.``Availability - Expression 02 - do``() = x.DoNamedTest()
