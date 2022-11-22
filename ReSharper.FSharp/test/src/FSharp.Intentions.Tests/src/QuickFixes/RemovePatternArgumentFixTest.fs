namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type RemovePatternArgumentFixTest() =
    inherit FSharpQuickFixTestBase<RemovePatternArgumentFix>()
    override x.RelativeTestDataPath = "features/quickFixes/removePatternArgumentFix"

    [<Test>] member x.``Single parameter`` () = x.DoNamedTest()
    [<Test>] member x.``Two parameters`` () = x.DoNamedTest()
    [<Test>] member x.``Local match qualified single parameter`` () = x.DoNamedTest()
    [<Test>] member x.``Local single match parameter`` () = x.DoNamedTest()
    [<Test>] member x.``Local single parameter`` () = x.DoNamedTest()
    [<Test>] member x.``Local two match parameters`` () = x.DoNamedTest()
    [<Test>] member x.``Local two match parens parameters`` () = x.DoNamedTest()
    [<Test>] member x.``Local two parameter`` () = x.DoNamedTest()
    [<Test>] member x.``Qualified single parameter`` () = x.DoNamedTest()
    [<Test>] member x.``Qualified two parameters`` () = x.DoNamedTest()
    [<Test>] member x.``Local match qualified single parens parameter`` () = x.DoNamedTest()