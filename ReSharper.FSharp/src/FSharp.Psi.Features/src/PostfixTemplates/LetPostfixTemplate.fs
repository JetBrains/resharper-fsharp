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

    let isApplicable (node: ITreeNode) =
        match node with
        | :? IFSharpExpression as fsExpr ->
            fsExpr :? IFromErrorExpr || FSharpIntroduceVariable.CanIntroduceVar(fsExpr, true)
        | :? ITypeReferenceName as typeReferenceName ->
            let typeUsage = NamedTypeUsageNavigator.GetByReferenceName(typeReferenceName)
            FSharpPostfixTemplates.isApplicableTypeUsage typeUsage
        | _ -> false

    override this.IsEnabled _ = true

    override this.CreateBehavior(info) =
        LetPostfixTemplateBehavior(info) :> _

    override this.TryCreateInfo(context) =
        let context = context.AllExpressions[0]
        if not (isApplicable context.Expression) then null else

        LetPostfixTemplateInfo(context) :> _

and LetPostfixTemplateInfo(expressionContext: PostfixExpressionContext) =
    inherit PostfixTemplateInfo("let", expressionContext)

and LetPostfixTemplateBehavior(info) =
    inherit FSharpPostfixTemplateBehaviorBase(info)

    override this.ExpandPostfix(context) =
        let psiServices = context.PostfixContext.PsiModule.GetPsiServices()
        psiServices.Transactions.Execute(this.ExpandCommandName, fun _ ->
            use writeCookie = WriteLockCookie.Create(context.Expression.IsPhysical())
            let expr = this.GetExpression(context)
            FSharpPostfixTemplates.getContainingAppExprFromLastArg expr :> ITreeNode)

    override this.AfterComplete(textControl, node, _) =
        FSharpIntroduceVariable.IntroduceVar(node :?> _, textControl, false, false, false)
