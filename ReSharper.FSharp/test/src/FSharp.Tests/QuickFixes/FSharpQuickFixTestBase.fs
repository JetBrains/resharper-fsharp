namespace JetBrains.ReSharper.Plugins.FSharp.Tests.Features

open JetBrains.ProjectModel
open JetBrains.ReSharper.FeaturesTestFramework.Intentions
open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.FeaturesTestFramework.Refactorings
open JetBrains.ReSharper.Plugins.FSharp.Tests
open NUnit.Framework

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