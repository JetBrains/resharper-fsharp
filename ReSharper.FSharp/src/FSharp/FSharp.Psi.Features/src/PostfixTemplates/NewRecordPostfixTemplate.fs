namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.PostfixTemplates

open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates.Contexts
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell

[<PostfixTemplate("new", "Construct new record instance", "{ Field = expr }")>]
type NewRecordPostfixTemplate() =
    inherit FSharpPostfixTemplateBase()

    override x.CreateBehavior(info) = NewRecordPostfixTemplateBehavior(info) :> _
    override x.CreateInfo(context) = NewRecordPostfixTemplateInfo(context) :> _

    override this.IsApplicable(node) =
        let refExpr = node.As<IReferenceExpr>()
        isNotNull refExpr &&

        let expr = node.As<IFSharpExpression>()
        FSharpPostfixTemplates.canBecomeStatement false expr &&

        match refExpr.Qualifier with
        | :? IReferenceExpr as refExpr ->
            let typeElement = refExpr.Reference.Resolve().DeclaredElement.As<ITypeElement>()
            typeElement.IsRecord()
        | _ -> false

    override this.IsEnabled _ = true


and NewRecordPostfixTemplateInfo(expressionContext: PostfixExpressionContext) =
    inherit PostfixTemplateInfo("new", expressionContext)

and NewRecordPostfixTemplateBehavior(info) =
    inherit FSharpPostfixTemplateBehaviorBase(info)

    override this.ExpandPostfix(context) =
        let node = context.Expression
        let psiServices = node.GetPsiServices()

        psiServices.Transactions.Execute(this.ExpandCommandName, fun _ ->
            let factory = node.CreateElementFactory()
            use writeCookie = WriteLockCookie.Create(node.IsPhysical())
            use disableFormatter = new DisableCodeFormatter()
            let refExpr = this.GetExpression(context) :?> IReferenceExpr
            let typeElement = refExpr.Reference.Resolve().DeclaredElement.As<ITypeElement>()

            let recordExpr = factory.CreateExpr("{ }") :?> IRecordExpr

            let recordExpr = ModificationUtil.ReplaceChild(refExpr, recordExpr)
            RecordExprUtil.generateBindings typeElement recordExpr |> ignore
            recordExpr
        )

    override this.AfterComplete(textControl, node, _) =
        let recordExpr = node :?> IRecordExpr
        let endOffset = recordExpr.RightBrace.GetDocumentEndOffset()
        
        let hotspotInfos =
            recordExpr.FieldBindings.ToArray()
            |> Array.map (fun binding ->
                let templateField = TemplateField(binding.ReferenceName.ShortName, SimpleHotspotExpression(null), 0)
                HotspotInfo(templateField, binding.Expression.GetDocumentRange(), KeepExistingText = true))

        let hotspotSession =
            LiveTemplatesManager.Instance.CreateHotspotSessionAtopExistingText(
                node.GetSolution(), endOffset, textControl,
                LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, hotspotInfos)

        hotspotSession.ExecuteAndForget()
