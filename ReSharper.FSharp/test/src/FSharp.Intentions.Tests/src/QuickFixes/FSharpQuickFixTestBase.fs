namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions.QuickFixes

open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.FeaturesTestFramework.Refactorings
open JetBrains.ReSharper.Plugins.FSharp.Tests
open JetBrains.ReSharper.Plugins.FSharp.Tests.Intentions
open JetBrains.ReSharper.TestFramework
open NUnit.Framework

type NoHighlightingFoundAttribute() =
    inherit ExpectedExceptionInsideSolutionAttribute(ExpectedMessage = ErrorText.NoHighlightingsFoundError)

type NotAvailableAttribute() =
    inherit ExpectedExceptionInsideSolutionAttribute(ExpectedMessage = ErrorText.NotAvailable)

type ActionNotAvailableAttribute() =
    inherit ExpectedExceptionInsideSolutionAttribute(ExpectedMessage = ErrorText.ActionNotAvailable)


[<AbstractClass; FSharpTest>]
type FSharpQuickFixTestBase<'T when 'T :> IQuickFix>() =
    inherit QuickFixTestBase<'T>()

    let [<Literal>] OccurrenceName = "OCCURRENCE"

    override x.OnQuickFixNotAvailable(_, _) = Assert.Fail(ErrorText.NotAvailable)

    override x.DoTestOnTextControlAndExecuteWithGold(project, textControl, projectFile) =
        let occurrenceName = QuickFixTestBase.GetSetting(textControl, OccurrenceName)
        if isNotNull occurrenceName then
            let workflowPopupMenu = x.Solution.GetComponent<TestWorkflowPopupMenu>()
            workflowPopupMenu.SetTestData(x.TestLifetime, fun _ occurrences _ _ _ ->
                occurrences
                |> Array.tryFind (fun occurrence -> occurrence.Name.Text = occurrenceName)
                |> Option.defaultWith (fun _ -> failwithf $"Could not find %s{occurrenceName} occurrence"))

        base.DoTestOnTextControlAndExecuteWithGold(project, textControl, projectFile)
