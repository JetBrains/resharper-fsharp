namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon
open NUnit.Framework

type IfExpressionAnalyzerTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/ifAnalyzer"

    override x.HighlightingPredicate(highlighting, _, _) =
        match highlighting with
        | :? ExpressionCanBeReplacedWithConditionWarning -> true
        | _ -> false

    [<Test>] member x.``Replace if with condition 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Replace if with condition 02 - Not available``() = x.DoNamedTest()
