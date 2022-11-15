namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type AddMatchAllClauseFixTest() =
    inherit FSharpQuickFixTestBase<AddMatchAllClauseFix>()

    override x.RelativeTestDataPath = "features/quickFixes/addMatchAllClause"

    [<Test>] member x.``Enum 01``() = x.DoNamedTest()
    [<Test>] member x.``Enum 02``() = x.DoNamedTest()
    [<Test>] member x.``Function 01``() = x.DoNamedTest()
    [<Test>] member x.``Function 02``() = x.DoNamedTest()
    [<Test>] member x.``Function 03``() = x.DoNamedTest()
    [<Test>] member x.``Simple 01``() = x.DoNamedTest()
    [<Test>] member x.``Simple 02 - Indent``() = x.DoNamedTest()
    [<Test>] member x.``Simple 03 - Multiple clauses``() = x.DoNamedTest()
    [<Test>] member x.``Simple 04 - Generate single line``() = x.DoNamedTest()
    [<Test>] member x.``Simple 05 - Missing bar``() = x.DoNamedTest()

    [<Test>] member x.``Type binding 01``() = x.DoNamedTest()

    [<Test>] member x.``Multiline 01``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 02``() = x.DoNamedTest()
    [<Test>] member x.``Multiline 03``() = x.DoNamedTest()
