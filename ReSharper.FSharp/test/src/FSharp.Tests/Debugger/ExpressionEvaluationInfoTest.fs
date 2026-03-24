namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features.Debugger

open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Services.Debugger
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.TestFramework
open NUnit.Framework


[<FSharpTest>]
type ExpressionEvaluationInfoTest() =
    inherit BaseTestWithTextControl()

    override x.RelativeTestDataPath = "features/debugger/evaluateExpression"

    override x.DoTest(lifetime, project: IProject) =
        let textControl = x.OpenTextControl(lifetime)
        let fsFile = textControl.GetFSharpFile(project.GetSolution())
        let documentRange = DocumentRange(textControl.Document, textControl.Caret.Position.Value.ToDocOffset() |> int)
        let _, textToEvaluate = FSharpExpressionEvaluationInfoProvider.GetTextToEvaluate(fsFile, documentRange)
        x.ExecuteWithGold(_.WriteLine(textToEvaluate)) |> ignore

    [<Test>] member x.``App - Method 01``() = x.DoNamedTest()
    [<Test>] member x.``App - Method 02``() = x.DoNamedTest()
    [<Test>] member x.``App - Method 03``() = x.DoNamedTest()
    [<Test>] member x.``App - Method 04``() = x.DoNamedTest()
    [<Test>] member x.``App - Method 05``() = x.DoNamedTest()
    [<Test>] member x.``App - Method 06``() = x.DoNamedTest()
    [<Test>] member x.``App - Method 07``() = x.DoNamedTest()

    [<Test>] member x.``Ref 01``() = x.DoNamedTest()
    [<Test>] member x.``Ref 02 - Qualifier``() = x.DoNamedTest()

    [<Test>] member x.``Indexer 01``() = x.DoNamedTest()

    [<Test>] member x.``SelfId 01 - Member``() = x.DoNamedTest()
    [<Test>] member x.``SelfId 02 - Type``() = x.DoNamedTest()
    [<Test>] member x.``SelfId 03 - As qualifier``() = x.DoNamedTest()
