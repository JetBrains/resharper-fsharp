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
        let psiModule = context.PostfixContext.PsiModule
        let psiServices = psiModule.GetPsiServices()

        psiServices.Transactions.Execute(x.ExpandCommandName, fun _ ->
            let node = context.Expression :?> IFSharpTreeNode
            let elementFactory = node.CreateElementFactory()
            use writeCookie = WriteLockCookie.Create(node.IsPhysical())
            use disableFormatter = new DisableCodeFormatter()
            let expr = x.GetExpression(context)
            let appExpr = elementFactory.CreateMatchExpr(expr)
            ModificationUtil.ReplaceChild(expr, appExpr) :> ITreeNode)

    override x.AfterComplete(textControl, node, _) =
        match node.As<IMatchExpr>() with
        | null -> ()
        | matchExpr ->

        let templatesManager = LiveTemplatesManager.Instance
        let solution = info.ExecutionContext.Solution

        let matchClause = matchExpr.Clauses.[0]
        let hotspotInfos =
            let pattern = matchClause.Pattern
            let templateField = TemplateField("Foo", SimpleHotspotExpression(null), 0)
            HotspotInfo(templateField, pattern.GetDocumentRange(), KeepExistingText = true)

        let hotspotSession =
            templatesManager.CreateHotspotSessionAtopExistingText(
                solution, matchClause.Expression.GetDocumentEndOffset(), textControl,
                LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, hotspotInfos)

        hotspotSession.Execute()