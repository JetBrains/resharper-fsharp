namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open NUnit.Framework

type RemoveRedundantAsPatTest() =
    inherit FSharpQuickFixTestBase<RemoveRedundantAsPatFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeRedundantAsPatFix"

    [<Test>] member x.``Redundant as pat 01``() = x.DoNamedTest()
    [<Test>] member x.``Redundant as pat 02 - Parens``() = x.DoNamedTest()
