namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open NUnit.Framework

type ReplaceWithTripleQuotedInterpolatedStringFixTest() =
    inherit FSharpQuickFixTestBase<ReplaceWithTripleQuotedInterpolatedStringFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceWithTripleQuotedInterpolatedStringFix"

    [<Test>] member x.``01 - Regular interpolated string``() = x.DoNamedTest()
    [<Test>] member x.``02 - Verbatim interpolated string``() = x.DoNamedTest()
