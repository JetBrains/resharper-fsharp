namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings

open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Refactorings.IntroduceVariable
open JetBrains.ReSharper.Refactorings.Workflow

[<Language(typeof<FSharpLanguage>)>]
type FSharpRefactoringLanguageService() =
    inherit InternalRefactoringLanguageService()

    override x.CreateIntroduceVariableHelper() = FSharpIntroduceVarHelper() :> _

    override x.CreateIntroduceVariable(workflow, solution, driver) =
           if workflow :? IntroduceVarFixWorkflow ||
              solution.RdFSharpModel.EnableExperimentalFeaturesSafe then
                FSharpIntroduceVariable(workflow, solution, driver) :> _
           else null

    override x.CreateInlineVar(workflow, solution, driver) =
//        FSharpInlineVar(workflow, solution, driver) :> _
        null
