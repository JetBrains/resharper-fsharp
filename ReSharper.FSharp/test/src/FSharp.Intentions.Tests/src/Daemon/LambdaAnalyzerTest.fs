namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon

open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.Daemon
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

type LambdaAnalyzerTest() =
    inherit FSharpHighlightingTestBase()

    override x.RelativeTestDataPath = "features/daemon/lambdaAnalyzer"

    override x.HighlightingPredicate(highlighting, _, _) =
        match highlighting with
        | :? DotLambdaCanBeUsedWarning
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

    [<FSharpLanguageLevel(FSharpLanguageLevel.FSharp50)>]
    [<Test; TestReferences("System.Core")>] member x.``Delegates - Not available``() = x.DoNamedTest()

    [<FSharpLanguageLevel(FSharpLanguageLevel.FSharp60)>]
    [<Test; TestReferences("System.Core")>] member x.``Delegates - F# 6``() = x.DoNamedTest()

    [<Test; Description("RIDER-78171")>] member x.``Implicit conversions``() = x.DoNamedTest()
    [<Test>] member x.``Not available``() = x.DoNamedTest()
    [<Test>] member x.``Overloads 01``() = x.DoNamedTest()
    [<Test>] member x.``Forced calculations``() = x.DoNamedTest()
    [<Test>] member x.``Used names - Nested scope``() = x.DoNamedTest()
    [<Test>] member x.``Optional parameters``() = x.DoNamedTest()

    [<FSharpLanguageLevel(FSharpLanguageLevel.FSharp80)>]
    [<Test; TestReferences("System.Core")>] member x.``Dot lambda - Availability``() = x.DoNamedTest()
    [<FSharpLanguageLevel(FSharpLanguageLevel.FSharp90)>]
    [<Test; TestReferences("System.Core")>] member x.``Dot lambda - Availability - F# 9``() = x.DoNamedTest()

    [<Test>] member x.``Dot lambda - Availability - Modules``() = x.DoNamedTest()
