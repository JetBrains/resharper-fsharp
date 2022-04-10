namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open NUnit.Framework

type ReplaceWithTripleQuotedInterpolatedStringFixTest() =
    inherit FSharpQuickFixTestBase<ReplaceWithTripleQuotedInterpolatedStringFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceWithTripleQuotedInterpolatedStringFix"

    [<Test>] member x.``01 - Regular interpolated string``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``02 - Not Available - Verbatim interpolated string``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``03 - Not Available - Nested interpolated string``() = x.DoNamedTest()
