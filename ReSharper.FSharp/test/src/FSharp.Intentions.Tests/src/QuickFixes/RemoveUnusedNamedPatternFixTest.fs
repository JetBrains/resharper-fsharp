namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type RemoveUnusedNamedPatternFixTest() =
    inherit FSharpQuickFixTestBase<RemoveUnusedNamedPatternFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeUnusedNamedPattern"
    
    [<Test>] member x.``Unused record named pattern 1`` () = x.DoNamedTest()
    [<Test>] member x.``Unused record named pattern 2`` () = x.DoNamedTest()
    [<Test>] member x.``Unused record named pattern 3`` () = x.DoNamedTest()
    [<Test>] member x.``Unused record named pattern 4`` () = x.DoNamedTest()
    [<Test>] member x.``Unused record named pattern 5`` () = x.DoNamedTest()
    [<Test>] member x.``Unused record named pattern 6`` () = x.DoNamedTest()
    [<Test>] member x.``Unused record named pattern 7`` () = x.DoNamedTest()
    [<Test>] member x.``Unused record named pattern 8`` () = x.DoNamedTest()
    [<Test>] member x.``Unused record named pattern 9`` () = x.DoNamedTest()
    [<Test>] member x.``Unused record named pattern 10`` () = x.DoNamedTest()
    [<Test>] member x.``Unused union case named pattern 1`` () = x.DoNamedTest()
    [<Test>] member x.``Unused union case named pattern 2`` () = x.DoNamedTest()
    [<Test>] member x.``Unused union case named pattern 3`` () = x.DoNamedTest()
    [<Test>] member x.``Unused union case named pattern 4`` () = x.DoNamedTest()
    [<Test>] member x.``Unused union case named pattern 5`` () = x.DoNamedTest()
    [<Test>] member x.``Unused union case named pattern 6`` () = x.DoNamedTest()
    [<Test>] member x.``Unused union case named pattern 7`` () = x.DoNamedTest()
    [<Test>] member x.``Unused union case named pattern 8`` () = x.DoNamedTest()
    [<Test>] member x.``Unused union case named pattern 9`` () = x.DoNamedTest()
    [<Test>] member x.``Unused union case named pattern 10`` () = x.DoNamedTest()