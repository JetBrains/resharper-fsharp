namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon

open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Analyzers
open NUnit.Framework

type RecursionGutterStageTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/recursion"

    override x.HighlightingPredicate(highlighting, _, _) =
        highlighting :? FSharpRecursionGutterHighlighting

    [<Test>] member x.``Expr - If 01``() = x.DoNamedTest()

    [<Test>] member x.``Function - Local 01``() = x.DoNamedTest()
    [<Test>] member x.``Function - Top 01``() = x.DoNamedTest()

    [<Test>] member x.``Method 01``() = x.DoNamedTest()
    [<Test>] member x.``Method 02``() = x.DoNamedTest()
