namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type UpdateMutabilityInSignatureFixTest() =
    inherit FSharpQuickFixTestBase<UpdateMutabilityInSignatureFix>()

    override x.RelativeTestDataPath = "features/quickFixes/updateMutabilityInSignatureFix"

    [<Test>] member x.``Add Mutability - 01`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Add Mutability - 02`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Remove Mutability - 01`` () = x.DoNamedTestWithSignature()
    [<Test>] member x.``Remove Mutability - 02`` () = x.DoNamedTestWithSignature()
