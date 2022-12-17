namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type RemoveUnusedNamedPatternFixTest() =
    inherit FSharpQuickFixTestBase<RemoveUnusedNamedPatternFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeUnusedNamedPattern"
    
    [<Test>] member x.``local literal constant pattern qualified parens parameter`` () = x.DoNamedTest()
    
    [<Test>] member x.``local literal constant pattern qualified parens parameter 2`` () = x.DoNamedTest()
    
    [<Test>] member x.``local literal constant pattern qualified parens parameter 3`` () = x.DoNamedTest()
