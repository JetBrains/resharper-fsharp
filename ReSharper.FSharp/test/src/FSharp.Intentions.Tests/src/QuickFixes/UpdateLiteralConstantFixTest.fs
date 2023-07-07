namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type UpdateLiteralConstantFixTest() =
    inherit FSharpQuickFixTestBase<UpdateLiteralConstantFix>()

    override x.RelativeTestDataPath = "features/quickFixes/updateLiteralConstantValueToSignatureFix"

    [<Test>] member x.``Update Literal Constant - 01`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Update Literal Constant - 02`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Update Literal Constant - 03`` () = x.DoNamedTestWithSignature()
