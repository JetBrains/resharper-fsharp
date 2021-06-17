namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.PostfixTemplates

open JetBrains.ReSharper.Feature.Services.PostfixTemplates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates.Contexts
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

[<PostfixTemplate("let", "Introduce let binding", "let _ = expr")>]
type LetPostfixTemplate() =
    inherit FSharpPostfixTemplateBase()

    override this.IsEnabled _ = true

    override this.CreateBehavior(info) =
        LetPostfixTemplateBehavior(info) :> _

    override this.TryCreateInfo(context) =
        let context = context.AllExpressions.[0]
        let node = context.Expression
        if isNull node then null else

        let expr = node.Parent.As<IFSharpExpression>()
        if not (FSharpIntroduceVariable.CanIntroduceVar(expr)) then null else

        LetPostfixTemplateInfo(context) :> _

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
