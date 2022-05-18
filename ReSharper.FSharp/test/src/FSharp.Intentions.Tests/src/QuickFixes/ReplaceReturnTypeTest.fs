namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type ReplaceReturnTypeTest() =
    inherit FSharpQuickFixTestBase<ReplaceReturnTypeFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceReturnType"

    [<Test>] member x.``Replace type in Constraint Mismatch``() = x.DoNamedTest()
    [<Test>] member x.``Replace type in infix application``() = x.DoNamedTest()
    [<Test>] member x.``Replace type in Sequential``() = x.DoNamedTest()
    [<Test>] member x.``Replace type in MatchClause``() = x.DoNamedTest()
    [<Test>] member x.``Replace type with attribute``() = x.DoNamedTest()
    [<Test>] member x.``Replace type in TryWith``() = x.DoNamedTest()
    [<Test>] member x.``Replace type in MatchLambda``() = x.DoNamedTest()
    [<Test; NoHighlightingFound>] member x.``Replace type - Negative 01``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Replace type - Negative 02``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Replace type - Negative 03``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Replace type - Negative 04``() = x.DoNamedTest()
    [<Test; NoHighlightingFound>] member x.``Replace type - Negative 05``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Replace type - Negative 06``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Replace type - Negative 07``() = x.DoNamedTest()
    [<Test; NoHighlightingFound>] member x.``Replace type - Negative 08``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Replace type - Negative 09``() = x.DoNamedTest()