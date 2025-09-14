namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type RemoveUnusedNamedPatternFixTest() =
    inherit FSharpQuickFixTestBase<RemoveUnusedNamedPatternFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeUnusedNamedPattern"
    
    [<Test>] member x.``Unused record named pattern 01`` () = x.DoNamedTest()
    [<Test>] member x.``Unused record named pattern 02`` () = x.DoNamedTest()
    [<Test>] member x.``Unused record named pattern 03`` () = x.DoNamedTest()
    [<Test>] member x.``Unused record named pattern 04`` () = x.DoNamedTest()
    [<Test>] member x.``Unused record named pattern 05`` () = x.DoNamedTest()
    [<Test>] member x.``Unused record named pattern 06`` () = x.DoNamedTest()
    [<Test>] member x.``Unused record named pattern 07`` () = x.DoNamedTest()
    [<Test>] member x.``Unused record named pattern 08`` () = x.DoNamedTest()
    [<Test>] member x.``Unused record named pattern 09`` () = x.DoNamedTest()
    [<Test>] member x.``Unused record named pattern 10`` () = x.DoNamedTest()
    [<Test>] member x.``Unused record named pattern 11`` () = x.DoNamedTest()
    [<Test>] member x.``Unused union case named pattern 01`` () = x.DoNamedTest()
    [<Test>] member x.``Unused union case named pattern 02`` () = x.DoNamedTest()
    [<Test>] member x.``Unused union case named pattern 03`` () = x.DoNamedTest()
    [<Test>] member x.``Unused union case named pattern 04`` () = x.DoNamedTest()
    [<Test>] member x.``Unused union case named pattern 05`` () = x.DoNamedTest()
    [<Test>] member x.``Unused union case named pattern 06`` () = x.DoNamedTest()
    [<Test>] member x.``Unused union case named pattern 07`` () = x.DoNamedTest()
    [<Test>] member x.``Unused union case named pattern 08`` () = x.DoNamedTest()
    [<Test>] member x.``Unused union case named pattern 09`` () = x.DoNamedTest()
    [<Test>] member x.``Unused union case named pattern 10`` () = x.DoNamedTest()
    [<Test>] member x.``Unused union case named pattern 11`` () = x.DoNamedTest()
