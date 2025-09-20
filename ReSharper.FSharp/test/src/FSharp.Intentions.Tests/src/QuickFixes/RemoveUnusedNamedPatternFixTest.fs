namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type RemoveUnusedNamedPatternFixTest() =
    inherit FSharpQuickFixTestBase<RemoveUnusedNamedPatternFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeUnusedNamedPattern"
    
    [<Test>] member x.``Record 01`` () = x.DoNamedTest()
    [<Test>] member x.``Record 02`` () = x.DoNamedTest()
    [<Test>] member x.``Record 03`` () = x.DoNamedTest()
    [<Test>] member x.``Record 04`` () = x.DoNamedTest()
    [<Test>] member x.``Record 05`` () = x.DoNamedTest()
    [<Test>] member x.``Record 06`` () = x.DoNamedTest()
    [<Test>] member x.``Record 07`` () = x.DoNamedTest()
    [<Test>] member x.``Record 08`` () = x.DoNamedTest()
    [<Test>] member x.``Record 09`` () = x.DoNamedTest()
    [<Test>] member x.``Record 10`` () = x.DoNamedTest()
    [<Test>] member x.``Record 11`` () = x.DoNamedTest()
    [<Test>] member x.``Record 12`` () = x.DoNamedTest()
    [<Test>] member x.``Union 01`` () = x.DoNamedTest()
    [<Test>] member x.``Union 02`` () = x.DoNamedTest()
    [<Test>] member x.``Union 03`` () = x.DoNamedTest()
    [<Test>] member x.``Union 04`` () = x.DoNamedTest()
    [<Test>] member x.``Union 05`` () = x.DoNamedTest()
    [<Test>] member x.``Union 06`` () = x.DoNamedTest()
    [<Test>] member x.``Union 07`` () = x.DoNamedTest()
    [<Test>] member x.``Union 08`` () = x.DoNamedTest()
    [<Test>] member x.``Union 09`` () = x.DoNamedTest()
    [<Test>] member x.``Union 10`` () = x.DoNamedTest()
    [<Test>] member x.``Union 11`` () = x.DoNamedTest()
    [<Test>] member x.``Union 12`` () = x.DoNamedTest()
