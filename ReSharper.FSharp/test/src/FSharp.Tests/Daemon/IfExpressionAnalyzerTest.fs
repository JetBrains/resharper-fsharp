namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon
open NUnit.Framework

type IfExpressionAnalyzerTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/ifAnalyzer"

    override x.HighlightingPredicate(highlighting, _, _) =
        match highlighting with
        | :? IfCanBeReplacedByConditionOperandWarning -> true
        | _ -> false

    [<Test>] member x.``Replace if by condition operand 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Replace if by condition operand 02 - Not available``() = x.DoNamedTest()
