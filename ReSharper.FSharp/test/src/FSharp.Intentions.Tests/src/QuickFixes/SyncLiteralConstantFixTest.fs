namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type SyncLiteralConstantFixTest() =
    inherit FSharpQuickFixTestBase<SyncLiteralConstantFix>()

    override x.RelativeTestDataPath = "features/quickFixes/syncLiteralConstantValueToSignatureFix"

    [<Test>] member x.``Sync Literal Constant - 01`` () = x.DoNamedTestWithSignature()
