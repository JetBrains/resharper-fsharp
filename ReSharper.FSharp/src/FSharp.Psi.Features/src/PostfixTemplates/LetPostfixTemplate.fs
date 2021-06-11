namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.PostfixTemplates

open JetBrains.ReSharper.Feature.Services.PostfixTemplates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates.Contexts
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

[<PostfixTemplate("let", "Introduce let binding", "let _ = expr")>]
type LetPostfixTemplate() =
    interface IPostfixTemplate with
        member this.Language = FSharpLanguage.Instance :> _
        member this.CreateBehavior(info) = LetPostfixTemplateBehavior(info) :> _

        member this.TryCreateInfo(context) =
            LetPostfixTemplateInfo(context.AllExpressions.[0]) :> _

and LetPostfixTemplateInfo(expressionContext: PostfixExpressionContext) =
    inherit PostfixTemplateInfo("let", expressionContext)

and LetPostfixTemplateBehavior(info) =
    inherit FSharpPostfixTemplateBehaviorBase(info)

    override this.ExpandPostfix(context) =
        let psiServices = context.PostfixContext.PsiModule.GetPsiServices()
        psiServices.Transactions.Execute(this.ExpandCommandName, fun _ ->
            use writeCookie = WriteLockCookie.Create(context.Expression.IsPhysical())
            this.GetExpression(context) :> ITreeNode)

    override this.AfterComplete(textControl, node, _) =
        FSharpIntroduceVariable.IntroduceVar(node :?> _, textControl, false, false)
