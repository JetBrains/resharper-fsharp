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
    interface IPostfixTemplate with
        member x.Language = FSharpLanguage.Instance :> _
        member x.CreateBehavior(info) = NotPostfixTemplateBehavior(info) :> _

        member x.TryCreateInfo(context) =
            NotPostfixTemplateInfo(context.AllExpressions.[0]) :> _


and NotPostfixTemplateInfo(expressionContext: PostfixExpressionContext) =
    inherit PostfixTemplateInfo("not", expressionContext)


and NotPostfixTemplateBehavior(info) =
    inherit FSharpPostfixTemplateBehaviorBase(info)

    override x.ExpandPostfix(context) =
        let psiModule = context.PostfixContext.PsiModule
        let psiServices = psiModule.GetPsiServices()

        psiServices.Transactions.Execute(x.ExpandCommandName, fun _ ->
            let node = context.Expression :?> IFSharpTreeNode
            let elementFactory = node.CreateElementFactory()
            use writeCookie = WriteLockCookie.Create(node.IsPhysical())
            use disableFormatter = new DisableCodeFormatter()
            let refExpr = x.GetExpression(context)

            let appExpr = elementFactory.CreateAppExpr("not", refExpr)
            let appExpr = ModificationUtil.ReplaceChild(refExpr, appExpr)

            addParensIfNeeded appExpr.ArgumentExpression |> ignore
            appExpr :> ITreeNode)
