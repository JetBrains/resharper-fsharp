namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open NUnit.Framework

type NamespaceToModuleTest() =
    inherit FSharpQuickFixTestBase<NamespaceToModuleFix>()

    override x.RelativeTestDataPath = "features/quickFixes/namespaceToModule"

    [<Test>] member x.``Simple``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Global namespace``() = x.DoNamedTest()
