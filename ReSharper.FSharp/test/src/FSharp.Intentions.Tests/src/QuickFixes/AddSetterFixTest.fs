namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open NUnit.Framework

type AddSetterFixTest() =
    inherit FSharpQuickFixTestBase<AddSetterFix>()
    override x.RelativeTestDataPath = "features/quickFixes/addSetterFix"

    [<Test>] member x.``Auto property 01``() = x.DoNamedTest()
    [<Test>] member x.``Auto property 02 - Static``() = x.DoNamedTest()
    [<Test>] member x.``Auto property 03 - With getter``() = x.DoNamedTest()
    [<Test>] member x.``Auto property 04 - Abstract``() = x.DoNamedTest()

    [<Test; NotAvailable>] member x.``Not available 01 - Virtual``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available 02 - Signature``() = x.DoNamedTestWithSignature()
