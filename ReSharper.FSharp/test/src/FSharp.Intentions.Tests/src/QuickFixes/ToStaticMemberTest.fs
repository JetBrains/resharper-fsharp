namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type ToStaticMemberTest() =
    inherit FSharpQuickFixTestBase<ToStaticMemberFix>()

    override x.RelativeTestDataPath = "features/quickFixes/toStaticMember"

    [<Test>] member x.``Simple 01``() = x.DoNamedTest()
    [<Test>] member x.``Simple 02``() = x.DoNamedTest()

    [<Test>] member x.``Not available 01 - Override``() = x.DoNamedTest()
