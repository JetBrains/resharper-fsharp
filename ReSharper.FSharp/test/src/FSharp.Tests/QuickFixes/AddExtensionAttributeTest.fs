namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type AddExtensionAttributeTest() =
    inherit FSharpQuickFixTestBase<AddExtensionAttributeFix>()

    override x.RelativeTestDataPath = "features/quickFixes/addExtensionAttribute"

    [<Test>] member x.``Module``() = x.DoNamedTest()
    [<Test>] member x.``Type``() = x.DoNamedTest()
    [<Test>] member x.``Attribute namespace - add``() = x.DoNamedTest()
    [<Test>] member x.``On type member``() = x.DoNamedTest()

    [<Test; NotAvailable>] member x.``Root module - not available``() = x.DoNamedTest()
