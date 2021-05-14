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

    [<Test>] member x.``Expr - Tuple - Match 01``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Tuple - Match 02``() = x.DoNamedTest()
    [<Test>] member x.``Expr - Tuple - Match 03``() = x.DoNamedTest()

    [<Test>] member x.``Pattern - Tuple 01``() = x.DoNamedTest()
    [<Test>] member x.``Pattern - Tuple 02``() = x.DoNamedTest()

    [<Test>] member x.``Enum case 01``() = x.DoNamedTest()
    [<Test>] member x.``Enum case 02 - Xml doc``() = x.DoNamedTest()

    [<Test>] member x.``Field - Exception 01``() = x.DoNamedTest()

    [<Test>] member x.``Field - Record - Down 01``() = x.DoNamedTest()
    [<Test>] member x.``Field - Record - Down 02 - Semicolon``() = x.DoNamedTest()
    [<Test>] member x.``Field - Record - Down 03 - Attributes``() = x.DoNamedTest()
    [<Test>] member x.``Field - Record - Down 04 - Attributes and XmlDoc``() = x.DoNamedTest()
    [<Test; Explicit("Fix Direction.All")>] member x.``Field - Record - Right 01``() = x.DoNamedTest()
    [<Test; Explicit("Fix Direction.All")>] member x.``Field - Record - Right 02``() = x.DoNamedTest()

    [<Test>] member x.``Field - Union case - Left 01``() = x.DoNamedTest()
    [<Test>] member x.``Field - Union case - Left 02 - Can't move``() = x.DoNamedTest()
    [<Test>] member x.``Field - Union case - Right 01``() = x.DoNamedTest()
    [<Test>] member x.``Field - Union case - Right 02 - Can't move``() = x.DoNamedTest()
    [<Test>] member x.``Field - Union case - Separate line 01``() = x.DoNamedTest()
    [<Test>] member x.``Field - Union case - Separate line 02``() = x.DoNamedTest()

    [<Test>] member x.``Match clause - Last - Down 01``() = x.DoNamedTest()
    [<Test>] member x.``Match clause - Last - Down 02 - Multiline``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Match clause - Last - Down 03 - Lambda body``() = x.DoNamedTest()
    [<Test>] member x.``Match clause - Last - Down 04 - Add empty line``() = x.DoNamedTest()
    [<Test>] member x.``Match clause - Last - Down 05 - Not applicable``() = x.DoNamedTest()
    [<Test>] member x.``Match clause - Last - Down 06 - App``() = x.DoNamedTest()
    [<Test>] member x.``Match clause - Last - Down 07 - New line``() = x.DoNamedTest()
    [<Test>] member x.``Match clause - Last - Down 08 - Operator deindent``() = x.DoNamedTest()
    [<Test>] member x.``Match clause - Last - Down 09 - Operator deindent``() = x.DoNamedTest()
    [<Test>] member x.``Match clause - Last - Up 01 - Single line``() = x.DoNamedTest()
    [<Test>] member x.``Match clause - Last - Up 02``() = x.DoNamedTest()
    [<Test>] member x.``Match clause - Last - Up 03 - Multiline``() = x.DoNamedTest()
    [<Test; Explicit>] member x.``Match clause - Last - Up 04 - Lambda body``() = x.DoNamedTest()
    [<Test>] member x.``Match clause - Last - Up 05 - App``() = x.DoNamedTest()
    [<Test>] member x.``Match clause - Match 01``() = x.DoNamedTest()
    [<Test>] member x.``Match clause - Match - Fix indent 01``() = x.DoNamedTest()
    [<Test>] member x.``Match clause - Match - Fix indent 02 - Multiline``() = x.DoNamedTest()

    [<Test>] member x.``Union case - No bar 01``() = x.DoNamedTest()
    [<Test>] member x.``Union case - No bar 02 - Single line``() = x.DoNamedTest()
    [<Test>] member x.``Union case - No bar 03 - Wrong indent``() = x.DoNamedTest()
    [<Test>] member x.``Union case 01``() = x.DoNamedTest()
    [<Test>] member x.``Union case 02``() = x.DoNamedTest()

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
