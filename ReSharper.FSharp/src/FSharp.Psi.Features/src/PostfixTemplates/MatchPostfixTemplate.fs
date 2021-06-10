namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.PostfixTemplates

open JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates.Contexts
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

[<PostfixTemplate("match", "Pattern match expression", "match expr with | _ -> ()")>]
type MatchPostfixTemplate() =
    interface IPostfixTemplate with
        member x.Language = FSharpLanguage.Instance :> _
        member x.CreateBehavior(info) = MatchPostfixTemplateBehavior(info) :> _

        member x.TryCreateInfo(context) =
            MatchPostfixTemplateInfo(context.AllExpressions.[0]) :> _


and MatchPostfixTemplateInfo(expressionContext: PostfixExpressionContext) =
    inherit PostfixTemplateInfo("match", expressionContext)


and MatchPostfixTemplateBehavior(info) =
    inherit FSharpPostfixTemplateBehaviorBase(info)

    override x.ExpandPostfix(context) =
        let psiServices = context.PostfixContext.PsiModule.GetPsiServices()

        psiServices.Transactions.Execute(x.ExpandCommandName, fun _ ->
            let node = context.Expression :?> IFSharpTreeNode
            use writeCookie = WriteLockCookie.Create(node.IsPhysical())
            use disableFormatter = new DisableCodeFormatter()

            let expr = x.GetExpression(context)
            let appExpr = expr.CreateElementFactory().CreateMatchExpr(expr)
            ModificationUtil.ReplaceChild(expr, appExpr) :> ITreeNode)

    override x.AfterComplete(textControl, node, _) =
        let matchExpr = node.As<IMatchExpr>()
        if isNull matchExpr then () else

        let matchClause = matchExpr.Clauses.[0]
        let hotspotInfos =
            let templateField = TemplateField("Foo", SimpleHotspotExpression(null), 0)
            HotspotInfo(templateField, matchClause.Pattern.GetDocumentRange(), KeepExistingText = true)

        let hotspotSession =
            LiveTemplatesManager.Instance.CreateHotspotSessionAtopExistingText(
                info.ExecutionContext.Solution, matchClause.Expression.GetDocumentEndOffset(), textControl,
                LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, hotspotInfos)

        hotspotSession.ExecuteAndForget()
