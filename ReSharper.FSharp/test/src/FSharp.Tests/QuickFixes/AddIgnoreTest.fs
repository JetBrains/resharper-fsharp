namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ProjectModel
open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.FeaturesTestFramework.Refactorings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

[<FSharpTest>]
type AddIgnoreTest() =
    inherit FSharpQuickFixTestBase<AddIgnoreFix>()

    let [<Literal>] OccurrenceName = "OCCURRENCE"

    override x.RelativeTestDataPath = "features/quickFixes/addIgnore"

    [<Test>] member x.``Module 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Module 02 - App``() = x.DoNamedTest()
    [<Test>] member x.``Module 03 - Multiline``() = x.DoNamedTest()

    [<Test>] member x.``Expression 01 - Simple``() = x.DoNamedTest()
    [<Test>] member x.``Expression 02 - App``() = x.DoNamedTest()
    [<Test>] member x.``Expression 03 - Multiline``() = x.DoNamedTest()
    [<Test>] member x.``Expression 04 - Lazy``() = x.DoNamedTest()
    [<Test>] member x.``Expression 05 - &&``() = x.DoNamedTest()
    [<Test>] member x.``Expression 06 - Same precedence``() = x.DoNamedTest()
    [<Test>] member x.``Expression 07 - Same precedence``() = x.DoNamedTest()

    [<Test>] member x.``New line - Lazy 01``() = x.DoNamedTest()

    [<Test>] member x.``New line - Match - Deindent 01``() = x.DoNamedTest()
    [<Test>] member x.``New line - Match - Deindent 02``() = x.DoNamedTest()

    [<Test>] member x.``New line - Match 01``() = x.DoNamedTest()
    [<Test>] member x.``New line - Match 02``() = x.DoNamedTest()
    [<Test>] member x.``New line - Match 03 - Single line``() = x.DoNamedTest()

    [<Test>] member x.``Inner expression - If 01``() = x.DoNamedTest()
    [<Test>] member x.``Inner expression - Match 01``() = x.DoNamedTest()
    [<Test>] member x.``Inner expression - Try 01 - With``() = x.DoNamedTest()
    [<Test>] member x.``Inner expression - Try 02 - Finally``() = x.DoNamedTest()

    [<Test>] member x.``Missing else branch 01``() = x.DoNamedTest()

    override x.DoTestOnTextControlAndExecuteWithGold(project, textControl, projectFile) =
        let occurrenceName = QuickFixTestBase.GetSetting(textControl, OccurrenceName)
        if isNotNull occurrenceName then
            let workflowPopupMenu = x.Solution.GetComponent<TestWorkflowPopupMenu>()
            workflowPopupMenu.SetTestData(x.TestLifetime, fun _ occurrences _ _ _ ->
                occurrences
                |> Array.tryFind (fun occurrence -> occurrence.Name.Text = occurrenceName)
                |> Option.defaultWith (fun _ -> failwithf "Could not find %s occurrence" occurrenceName))

        base.DoTestOnTextControlAndExecuteWithGold(project, textControl, projectFile)


[<FSharpTest>]
type AddIgnoreAvailabilityTest() =
    inherit QuickFixAvailabilityTestBase()

    override x.RelativeTestDataPath = "features/quickFixes/addIgnore"

    [<Test>] member x.``Availability - Unexpected expression type``() = x.DoNamedTest()
    [<Test>] member x.``Availability - If expression wrong type``() = x.DoNamedTest()
    [<Test>] member x.``Availability - Else branch wrong type``() = x.DoNamedTest()
