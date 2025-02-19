namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type AddMissingSeqFixTest() =
    inherit FSharpQuickFixTestBase<AddMissingSeqFix>()

    override x.RelativeTestDataPath = "features/quickFixes/addMissingSeqFix"

    [<Test>] member x.``FS0740 — Adds missing seq before { x; y }`` () = x.DoNamedTest()
