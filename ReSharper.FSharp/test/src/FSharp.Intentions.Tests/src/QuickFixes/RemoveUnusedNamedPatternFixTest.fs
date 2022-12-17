namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type RemoveUnusedNamedPatternFixTest() =
    inherit FSharpQuickFixTestBase<RemoveUnusedNamedPatternFix>()

    override x.RelativeTestDataPath = "features/quickFixes/removeUnusedNamedPattern"
    
    [<Test>] member x.``Unused record named pattern first position`` () = x.DoNamedTest()
    
    [<Test>] member x.``Unused record named pattern middle position`` () = x.DoNamedTest()
    
    [<Test>] member x.``Unused record named pattern last position`` () = x.DoNamedTest()
    
    [<Test>] member x.``Unused record named pattern with a single field`` () = x.DoNamedTest()