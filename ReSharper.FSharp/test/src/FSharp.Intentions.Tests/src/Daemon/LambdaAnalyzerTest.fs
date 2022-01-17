﻿namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon

open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon
open NUnit.Framework

type LambdaAnalyzerTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/lambdaAnalyzer"

    override x.HighlightingPredicate(highlighting, _, _) =
        match highlighting with
        | :? LambdaCanBeReplacedWithInnerExpressionWarning
        | :? LambdaCanBeSimplifiedWarning
        | :? LambdaCanBeReplacedWithBuiltinFunctionWarning
        | :? RedundantApplicationWarning -> true
        | _ -> false

    [<Test>] member x.``Application``() = x.DoNamedTest()
    [<Test>] member x.``Partial application``() = x.DoNamedTest()
    [<Test>] member x.``Id 01``() = x.DoNamedTest()
    [<Test>] member x.``Id 02 - Pipe``() = x.DoNamedTest()
    [<Test>] member x.``Fst``() = x.DoNamedTest()
    [<Test>] member x.``Snd``() = x.DoNamedTest()
    [<Test>] member x.``Delegates 01``() = x.DoNamedTest()
    [<Test>] member x.``Delegates 02 - Method overloads``() = x.DoNamedTest()
    [<Test>] member x.``Delegates - Not available``() = x.DoNamedTest()
    [<Test>] member x.``Not available``() = x.DoNamedTest()

    [<Test>] member x.``Used names - Nested scope``() = x.DoNamedTest()
