namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type NamespaceToModuleTest() =
    inherit QuickFixTestBase<NamespaceToModuleFix>()

    override x.RelativeTestDataPath = "features/quickFixes/namespaceToModule"
    
    override x.OnQuickFixNotAvailable(_, _) = Assert.Fail(ErrorText.NotAvailable);

    [<Test>] member x.``Simple``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Global namespace``() = x.DoNamedTest()
