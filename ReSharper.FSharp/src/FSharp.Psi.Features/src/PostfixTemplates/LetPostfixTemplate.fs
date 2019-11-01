namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.PostfixTemplates

open JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates.Contexts
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

[<PostfixTemplate("let", "Introduce let binding", "let _ = expr")>]
type LetPostfixTemplate() =
    interface IPostfixTemplate with
        member x.Language = FSharpLanguage.Instance :> _
        member x.CreateBehavior(info) = LetPostfixTemplateBehavior(info) :> _

        member x.TryCreateInfo(context) =
            LetPostfixTemplateInfo(context.AllExpressions.[0]) :> _


and LetPostfixTemplateInfo(expressionContext: PostfixExpressionContext) =
    inherit PostfixTemplateInfo("let", expressionContext)


and LetPostfixTemplateBehavior(info) =
    inherit FSharpPostfixTemplateBehaviorBase(info)

    override x.ExpandPostfix(context) =
        let psiModule = context.PostfixContext.PsiModule
        let psiServices = psiModule.GetPsiServices()

        psiServices.Transactions.Execute(x.ExpandCommandName, fun _ ->
            let node = context.Expression :?> IFSharpTreeNode
            let elementFactory = node.CreateElementFactory()
            use writeCookie = WriteLockCookie.Create(node.IsPhysical())
            let refExpr = x.GetExpression(context)

            // todo: we create wrong let bindings node here
            // todo: top level bindings should be create as module members, not as let expressions
            let letOrUseExpr = elementFactory.CreateLetBindingExpr("_", refExpr)
            ModificationUtil.ReplaceChild(refExpr, letOrUseExpr) :> ITreeNode)

    override x.AfterComplete(textControl, node, _) =
        match node.As<ILetOrUseExpr>() with
        | null -> ()
        | letExpr ->

        let templatesManager = LiveTemplatesManager.Instance
        let solution = info.ExecutionContext.Solution

        let hotspotInfos =
            let headPattern = letExpr.Bindings.[0].HeadPattern
            let templateField = TemplateField("Foo", SimpleHotspotExpression(null), 0)
            HotspotInfo(templateField, headPattern.GetDocumentRange(), KeepExistingText = true)

        let hotspotSession =
            templatesManager.CreateHotspotSessionAtopExistingText(
                solution, letExpr.GetDocumentEndOffset(), textControl,
                LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, hotspotInfos)

        hotspotSession.Execute() |> ignore
