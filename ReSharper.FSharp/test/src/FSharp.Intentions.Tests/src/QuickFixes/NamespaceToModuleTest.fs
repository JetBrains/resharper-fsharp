namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

type NamespaceToModuleTest() =
    inherit FSharpQuickFixTestBase<NamespaceToModuleFix>()

    override x.RelativeTestDataPath = "features/quickFixes/namespaceToModule"

    [<Test>] member x.``Binding 01``() = x.DoNamedTest()
    [<Test>] member x.``Binding 02``() = x.DoNamedTest()
    [<Test>] member x.``Binding 03``() = x.DoNamedTest()
    [<Test; FSharpSignatureTest>] member x.``Binding 04 - val``() = x.DoNamedTest()
    [<Test>] member x.``Binding 05``() = x.DoNamedTest()
    [<Test>] member x.``Binding 06``() = x.DoNamedTest()

    [<Test>] member x.``Expression 01``() = x.DoNamedTest()
    [<Test>] member x.``Expression 02 - do``() = x.DoNamedTest()

    [<Test; NotAvailable>] member x.``Global namespace``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Two namespaces``() = x.DoNamedTest()
