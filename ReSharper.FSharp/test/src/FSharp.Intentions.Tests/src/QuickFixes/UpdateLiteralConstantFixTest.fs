namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type UpdateLiteralConstantFixTest() =
    inherit FSharpQuickFixTestBase<UpdateLiteralConstantFix>()

    override x.RelativeTestDataPath = "features/quickFixes/updateLiteralConstantValueToSignatureFix"

    [<Test>] member x.``Update Literal Constant - 01`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Update Literal Constant - 02`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Update including type - 01`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Update with inline comments - 01`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Update with complex expr in sig - 01`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Update with enum case - 01`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Update with other literal expr - 01`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Update with module symbol - 01`` () = x.DoNamedTestWithSignature()
    [<FSharpLanguageLevel(FSharpLanguageLevel.Preview)>]
    [<Test>] member x.``Update with complex expr - 01`` () = x.DoNamedTestWithSignature()
    [<FSharpLanguageLevel(FSharpLanguageLevel.Preview)>]
    [<Test>] member x.``Update with complex expr - 02`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Update with missing attribute - 01`` () = x.DoNamedTestWithSignature()
    [<Test; NotAvailable>] member x.``No update for unknown type - 01`` () = x.DoNamedTestWithSignature()
    [<Test; NotAvailable>] member x.``No update with unknown module symbol - 01`` () = x.DoNamedTestWithSignature()
    [<FSharpLanguageLevel(FSharpLanguageLevel.Preview)>]
    [<Test; NotAvailable>] member x.``No update with unknown module symbol - 02`` () = x.DoNamedTestWithSignature()
    [<Test; NotAvailable>] member x.``No update with unknown other literal - 01`` () = x.DoNamedTestWithSignature()
