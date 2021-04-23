namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.RearrangeCode
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

// todo: move the existing base test to public SDK

[<FSharpTest>]
type RearrangeCodeTest() =
    inherit BaseTestWithTextControl()

    override x.RelativeTestDataPath = "features/rearrangeCode"

    [<Test>] member x.``Field - Exception 01``() = x.DoNamedTest()
    [<Test>] member x.``Field - Union case - Left 01``() = x.DoNamedTest()
    [<Test>] member x.``Field - Union case - Left 02 - Can't move``() = x.DoNamedTest()
    [<Test>] member x.``Field - Union case - Right 01``() = x.DoNamedTest()
    [<Test>] member x.``Field - Union case - Right 02 - Can't move``() = x.DoNamedTest()
    [<Test>] member x.``Field - Union case - Separate line 01``() = x.DoNamedTest()
    [<Test>] member x.``Field - Union case - Separate line 02``() = x.DoNamedTest()

    override this.DoTest(lifetime: Lifetime, testProject: IProject) =
        let textControl = this.OpenTextControl(lifetime)

        let directionString = BaseTestWithTextControl.GetSetting(textControl, "DIRECTION")
        Assert.IsNotNull(directionString, "Setting ${DIRECTION=...} in missing in test file")
        let direction = directionString.ToEnum<Direction>()

        let solution = this.Solution
        let elementRearranger = solution.GetComponent<ElementRearranger>()

        let elements = elementRearranger.GetApplicableElements(direction, PsiEditorView(solution, textControl))

        elements
        |> Seq.tryHead
        |> Option.iter (fun element ->
            Assert.IsTrue(element.CanMove(direction), "Cannot move in given direction")
            elementRearranger.ExecuteMove(element,direction, solution, textControl))

        this.CheckProject(testProject)
