namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Daemon
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

[<TestPackages("FSharp.Core")>]
type LambdaAnalyzerTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/lambdaAnalyzer"

    override x.HighlightingPredicate(highlighting, _, _) =
        match highlighting with
        | :? LambdaCanBeSimplifiedWarning
        | :? LambdaCanBeReplacedWithInnerExpressionWarning
        | :? LambdaBodyCanBeReplacedWithIdWarning
        | :? LambdaCanBeReplacedWithBuiltinFunctionWarning -> true
        | _ -> false

    [<Test>] member x.``Application``() = x.DoNamedTest()
    [<Test>] member x.``Partial application``() = x.DoNamedTest()
    [<Test>] member x.``Id``() = x.DoNamedTest()
    [<Test>] member x.``Fst``() = x.DoNamedTest()
    [<Test>] member x.``Snd``() = x.DoNamedTest()
    [<Test>] member x.``Not available``() = x.DoNamedTest()

    [<Test>] member x.``Used names - Nested scope``() = x.DoNamedTest()
