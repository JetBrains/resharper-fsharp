namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Debugger

open JetBrains.ReSharper.Feature.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Services.Debugger
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework


[<FSharpTest>]
type ExpressionEvaluationInfoTest() =
    inherit BaseTestWithTextControl()

    override x.RelativeTestDataPath = "features/debugger/evaluateExpression"

    override x.DoTest(lifetime, _) =
        let textControl = x.OpenTextControl(lifetime)
        let expr = TextControlToPsi.GetElementFromCaretPosition<IFSharpExpression>(x.Solution, textControl)
        let textToEvaluate = FSharpExpressionEvaluationInfoProvider.GetTextToEvaluate(expr)
        x.ExecuteWithGold(fun writer ->
            writer.WriteLine(textToEvaluate)) |> ignore

    [<Test>] member x.``Id 01``() = x.DoNamedTest()
    [<Test>] member x.``Id 02 - Qualifier``() = x.DoNamedTest()

    [<Test>] member x.``SelfId 01 - Member``() = x.DoNamedTest()
    [<Test>] member x.``SelfId 02 - Type``() = x.DoNamedTest()
    [<Test>] member x.``SelfId 03 - As qualifier``() = x.DoNamedTest()

    [<Test>] member x.``Indexer 01``() = x.DoNamedTest()
