namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Refactorings.Workflow

[<Language(typeof<FSharpLanguage>)>]
type FSharpRefactoringLanguageService() =
    inherit InternalRefactoringLanguageService()

    override x.Helper = FSharpRefactoringsHelper() :> _

    override x.CreateIntroduceVariableHelper() =
        FSharpIntroduceVarHelper() :> _

    override x.CreateIntroduceVariable(workflow, solution, driver) =
        FSharpIntroduceVariable(workflow, solution, driver) :> _

    override x.CreateInlineVar(workflow, solution, driver) =
        if not (solution.FSharpExperimentalFeaturesEnabled()) then null else
        FSharpInlineVariable(workflow, solution, driver) :> _

and FSharpRefactoringsHelper() =
    inherit RefactoringsHelper()

    override x.CreateInlineVarAnalyser(workflow) =
        FSharpInlineVarAnalyser(workflow) :> _

    override x.IsLocalVariable(declaredElement) =
        let refPat = declaredElement.As<ILocalReferencePat>()
        if isNull refPat then false else

        let binding = BindingNavigator.GetByHeadPattern(refPat.IgnoreParentParens())
        let letExpr = LetOrUseExprNavigator.GetByBinding(binding)
        isNotNull letExpr
