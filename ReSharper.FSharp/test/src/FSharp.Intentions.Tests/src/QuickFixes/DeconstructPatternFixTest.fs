namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open NUnit.Framework

type DeconstructPatternFixTest() =
    inherit FSharpQuickFixTestBase<DeconstructPatternFix>()

    override x.RelativeTestDataPath = "features/quickFixes/deconstruct"

    [<Test>] member x.``Union case fields 01``() = x.DoNamedTest()
    [<Test>] member x.``Union case fields 02 - Used``() = x.DoNamedTest()
