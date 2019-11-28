namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.QuickFixes

open System
open JetBrains.Application.DataContext
open JetBrains.Application.UI.Actions.ActionManager
open JetBrains.DocumentModel.DataContext
open JetBrains.Lifetimes
open JetBrains.ProjectModel.DataContext
open JetBrains.ReSharper.Feature.Services.QuickFixes
open JetBrains.ReSharper.Feature.Services.Refactorings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.DataContext
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl.DataContext
open JetBrains.Util

type IntroduceVarFix(warning: UnitTypeExpectedWarning) =
    inherit QuickFixBase()

    let expr = warning.Expr

    override x.Text = "Introduce 'let' binding"

    override x.IsAvailable _ =
        if not (isValid expr) then false else

        let sequentialExpr = SequentialExprNavigator.GetByExpression(expr)
        if isNull sequentialExpr then false else

        let nextMeaningfulSibling = expr.GetNextMeaningfulSibling()
        nextMeaningfulSibling :? ISynExpr && nextMeaningfulSibling.Indent = expr.Indent

    override x.ExecutePsiTransaction(solution, _) =
        Action<_>(fun textControl ->
            let name = "IntroduceVarFix"
            let document = textControl.Document

            let rules =
                DataRules
                    .AddRule(name, ProjectModelDataConstants.SOLUTION, solution)
                    .AddRule(name, DocumentModelDataConstants.DOCUMENT, document)
                    .AddRule(name, TextControlDataConstants.TEXT_CONTROL, textControl)
                    .AddRule(name, PsiDataConstants.SELECTED_EXPRESSION, expr)

            use lifetime = Lifetime.Define(Lifetime.Eternal)

            let actionManager = Shell.Instance.GetComponent<IActionManager>()
            let dataContext = actionManager.DataContexts.CreateWithDataRules(lifetime.Lifetime, rules)

            expr.UserData.PutKey(FSharpIntroduceVariable.Key)
            let workflow = IntroduceVarFixWorkflow(solution)
            RefactoringActionUtil.ExecuteRefactoring(dataContext, workflow))
