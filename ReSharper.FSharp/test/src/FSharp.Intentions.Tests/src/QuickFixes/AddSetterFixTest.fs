namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<AssertCorrectTreeStructure>]
type AddSetterFixTest() =
    inherit FSharpQuickFixTestBase<AddSetterFix>()
    override x.RelativeTestDataPath = "features/quickFixes/addSetterFix"

    [<Test>] member x.``Auto property 01``() = x.DoNamedTest()
    [<Test>] member x.``Auto property 02 - Static``() = x.DoNamedTest()
    [<Test>] member x.``Auto property 03 - With getter``() = x.DoNamedTest()
    [<Test>] member x.``Auto property 04 - Abstract``() = x.DoNamedTest()
    [<Test>] member x.``Auto property 05 - With getter - Access modifier``() = x.DoNamedTest()

    // TODO: fix in the formatter
    [<Test>] member x.``Auto property 06 - Comment``() = x.DoNamedTest()
    [<Test>] member x.``Auto property 07 - Comment``() = x.DoNamedTest()

    [<Test>] member x.``Not available 01 - Virtual``() = x.DoNamedTest()
    [<Test>] member x.``Not available 02 - Signature``() = x.DoNamedTestWithSignature()
