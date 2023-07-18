namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type UpdateAccessibilityInSignatureFixTest() =
    inherit FSharpQuickFixTestBase<UpdateAccessibilityInSignatureFix>()

    override x.RelativeTestDataPath = "features/quickFixes/updateAccessibilityInSignatureFix"

    [<Test>] member x.``Binding - 01`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Binding - 02`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Binding - 03`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Binding - 04`` () = x.DoNamedTestWithSignature()
