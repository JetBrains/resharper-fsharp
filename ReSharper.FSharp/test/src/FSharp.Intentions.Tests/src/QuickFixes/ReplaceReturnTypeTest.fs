namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type ReplaceReturnTypeTest() =
    inherit FSharpQuickFixTestBase<ReplaceReturnTypeFix>()

    override x.RelativeTestDataPath = "features/quickFixes/replaceReturnType"

    [<Test>] member x.``Constraint Mismatch``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Infix application``() = x.DoNamedTest()
    [<Test>] member x.Sequential() = x.DoNamedTest()
    [<Test>] member x.MatchClause() = x.DoNamedTest()
    [<Test>] member x.``Return type with attribute``() = x.DoNamedTest()
    [<Test>] member x.TryWith() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.MatchLambda() = x.DoNamedTest()
    [<Test>] member x.LetOrUse() = x.DoNamedTest()
    [<Test>] member x.``IfThenElse - If``() = x.DoNamedTest()
    [<Test>] member x.``IfThenElse - Else``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``FunctionType 01``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``FunctionType 02``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``FunctionType 03``() = x.DoNamedTest()
    [<Test>] member x.``Paren around return type``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Tuple return type``() = x.DoNamedTest()
    [<Test; NoHighlightingFound>] member x.``Tuple return type, mismatch in number of items, to few in return type``() = x.DoNamedTest()
    [<Test; NoHighlightingFound>] member x.``Tuple return type, mismatch in number of items, to many in return type``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Tuple return type, to non-tuple``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Tuple return type, tuple as part of nested expression``() = x.DoNamedTest()

    [<Test; NoHighlightingFound>] member x.``No highlighting 01``() = x.DoNamedTest()
    [<Test; NoHighlightingFound>] member x.``No highlighting 02``() = x.DoNamedTest()
    [<Test; NoHighlightingFound>] member x.``No highlighting 03``() = x.DoNamedTest()

    [<Test; NotAvailable>] member x.``Not available 01``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available 02``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available 03``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available 04``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available 05``() = x.DoNamedTest()
    [<Test; NotAvailable>] member x.``Not available 06``() = x.DoNamedTest()