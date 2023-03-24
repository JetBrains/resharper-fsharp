namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Refactorings.Workflow
open JetBrains.Util

[<Language(typeof<FSharpLanguage>)>]
type FSharpRefactoringLanguageService() =
    inherit InternalRefactoringLanguageService()

    override x.Helper = FSharpRefactoringsHelper() :> _

    override x.CreateIntroduceVariableHelper() =
        FSharpIntroduceVarHelper() :> _

    override x.CreateIntroduceVariable(workflow, solution, driver) =
        FSharpIntroduceVariable(workflow, solution, driver) :> _

    override x.CreateInlineVar(workflow, solution, driver) =
        FSharpInlineVariable(workflow, solution, driver) :> _

and FSharpRefactoringsHelper() =
    inherit RefactoringsHelper()

    override this.CanIntroduceFieldFrom(_: IExpression) = false
    override this.CanIntroduceFieldFrom(_: ILocalVariable) = false

    override this.CanInlineField _ = false

    override x.CreateInlineVarAnalyser(workflow) =
        FSharpInlineVarAnalyser(workflow) :> _

    override this.CanInlineVariable(declaredElement) =
        let patternDeclaredElement = declaredElement.As<IFSharpPatternDeclaredElement>()
        isNotNull patternDeclaredElement &&

        let decl = patternDeclaredElement.GetDeclarations().SingleItem()
        let binding = BindingNavigator.GetByHeadPattern(decl.As<IReferencePat>())
        isNotNull binding && binding.ParametersDeclarationsEnumerable.IsEmpty()

    override this.IsValidIntroduceInnerExpression(expr) =
        FSharpIntroduceVariable.IsValidInnerExpression(expr)
