namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.PostfixTemplates

open JetBrains.ReSharper.Feature.Services.PostfixTemplates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates.Contexts
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

[<PostfixTemplate("not", "Apply not function", "not (expr)")>]
type NotPostfixTemplate() =
    inherit FSharpPostfixTemplateBase()

    override x.CreateBehavior(info) = NotPostfixTemplateBehavior(info) :> _
    override x.CreateInfo(context) = NotPostfixTemplateInfo(context) :> _
    override this.IsApplicable _ = true


and NotPostfixTemplateInfo(expressionContext: PostfixExpressionContext) =
    inherit PostfixTemplateInfo("not", expressionContext)


and NotPostfixTemplateBehavior(info) =
    inherit FSharpPostfixTemplateBehaviorBase(info)

    override x.ExpandPostfix(context) =
        let psiServices = context.PostfixContext.PsiModule.GetPsiServices()
        psiServices.Transactions.Execute(x.ExpandCommandName, fun _ ->
            let node = context.Expression :?> IFSharpTreeNode
            use writeCookie = WriteLockCookie.Create(node.IsPhysical())
            use disableFormatter = new DisableCodeFormatter()
            let refExpr = x.GetExpression(context)

            let appExpr = node.CreateElementFactory().CreateAppExpr("not", refExpr)
            let appExpr = ModificationUtil.ReplaceChild(refExpr, appExpr)

            addParensIfNeeded appExpr.ArgumentExpression |> ignore
            appExpr :> ITreeNode)
