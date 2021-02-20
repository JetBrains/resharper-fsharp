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


[<PostfixTemplate("for", "Iterates over enumerable collection", "for _ in expr do ()")>]
type ForPostfixTemplate() =
    interface IPostfixTemplate with
        member x.Language = FSharpLanguage.Instance :> _
        member x.CreateBehavior(info) = ForPostfixTemplateBehavior(info) :> _

        member x.TryCreateInfo(context) =
            ForPostfixTemplateInfo(context.AllExpressions.[0]) :> _

and ForPostfixTemplateInfo(expressionContext: PostfixExpressionContext) =
    inherit PostfixTemplateInfo("for", expressionContext)


and ForPostfixTemplateBehavior(info) =
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

            let forEachExpr = elementFactory.CreateForEachExpr(refExpr)
            ModificationUtil.ReplaceChild(refExpr, forEachExpr) :> ITreeNode)

    override x.AfterComplete(textControl, node, _) =
        match node.As<IForEachExpr>() with
        | null -> ()
        | forEachExpr ->

        let templatesManager = LiveTemplatesManager.Instance
        let solution = info.ExecutionContext.Solution

        let hotspotInfos =
            let headPattern = forEachExpr.Pattern
            let templateField = TemplateField("Foo", SimpleHotspotExpression(null), 0)
            HotspotInfo(templateField, headPattern.GetDocumentRange(), KeepExistingText = true)

        let hotspotSession =
            templatesManager.CreateHotspotSessionAtopExistingText(
                solution, forEachExpr.GetDocumentEndOffset(), textControl,
                LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, hotspotInfos)

        hotspotSession.Execute()
