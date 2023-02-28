namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.PostfixTemplates

open JetBrains.Diagnostics
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Navigation.CustomHighlighting
open JetBrains.ReSharper.Feature.Services.PostfixTemplates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates.Contexts
open JetBrains.ReSharper.Feature.Services.Refactorings.WorkflowOccurrences
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl
open JetBrains.UI.RichText

module WithPostfixTemplate =
    let isRecord (expr: IFSharpExpression) =
        let fcsType = expr.TryGetFcsType()
        if isNull fcsType then false else

        let fcsType = FSharpSymbolUtil.getAbbreviatedType fcsType
        if not fcsType.HasTypeDefinition then false else

        fcsType.TypeDefinition.IsFSharpRecord

    type RecordExprContext =
         { CopyExpr: IFSharpExpression
           Fields: IReferenceExpr list }

    let tryGetContext (refExpr: IReferenceExpr) =
        // get 'expr' from 'expr.__' reparsed expr
        let expr = refExpr.Qualifier.NotNull()

        let rec loop (acc: RecordExprContext list) (expr: IFSharpExpression) =
            match expr with
            | :? IReferenceExpr as refExpr ->
                let qualifierExpr = refExpr.Qualifier
                if isNotNull qualifierExpr && isRecord qualifierExpr then
                    let acc = { CopyExpr = qualifierExpr; Fields = refExpr :: acc.Head.Fields } :: acc
                    loop acc qualifierExpr
                else
                    acc

            | _ -> acc

        loop [{ CopyExpr = expr; Fields = [] }] expr

    /// Gets the innermost record expr in the generated recursive record expr
    let rec getInnermostRecordExpr (recordExpr: IRecordExpr) : IRecordExpr =
        recordExpr.FieldBindingsEnumerable
        |> Seq.tryHead
        |> Option.map (fun fieldBinding -> 
            match fieldBinding.Expression with
            | :? IRecordExpr as recordExpr -> getInnermostRecordExpr recordExpr
            | _ -> recordExpr)
        |> Option.defaultValue recordExpr

    /// Creates recursive record expr from IReferenceExpr list
    let createRecordExpr (copyExpr: IFSharpExpression) (fields: IReferenceExpr list) (factory: IFSharpElementFactory) : IRecordExpr =
        let recordExpr = factory.CreateExpr("{ x with P = 1 }") :?> IRecordExpr
        ModificationUtil.ReplaceChild(recordExpr.CopyInfoExpression, copyExpr.Copy()) |> ignore

        let innerRecordExpr = 
            (recordExpr, fields) ||> Seq.fold (fun recordExpr refExpr ->
                 let fieldBinding = recordExpr.FieldBindings[0]
                 fieldBinding.ReferenceName.SetName(refExpr.ShortName) |> ignore

                 let innerRecordExpr = factory.CreateExpr("{ x with P = 1 }") :?> IRecordExpr
                 innerRecordExpr.SetCopyInfoExpression(refExpr.Copy()) |> ignore
                 let newRecordExpr = fieldBinding.SetExpression(innerRecordExpr) :?> IRecordExpr

                 newRecordExpr
            )

        ModificationUtil.DeleteChild(innerRecordExpr.FieldBindings[0])

        recordExpr


[<PostfixTemplate("with", "Copies and updates the record field", "{ record with field }")>]
type WithPostfixTemplate() =
    inherit FSharpPostfixTemplateBase()

    override x.CreateBehavior(info) = WithPostfixTemplateBehavior(info) :> _
    override this.CreateInfo(context) = WithPostfixTemplateInfo(context) :> _

    override this.IsApplicable(node) =
        let refExpr = node.As<IReferenceExpr>()
        if isNull refExpr then false else

        let qualifierExpr = refExpr.Qualifier
        isNotNull qualifierExpr && WithPostfixTemplate.isRecord qualifierExpr


and WithPostfixTemplateInfo(expressionContext: PostfixExpressionContext) =
    inherit PostfixTemplateInfo("with", expressionContext)


and WithPostfixTemplateBehavior(info) =
    inherit FSharpPostfixTemplateBehaviorBase(info)

    override x.ExpandPostfix(context) =
        let node = context.Expression :?> IReferenceExpr
        let factory = node.CreateElementFactory()
        let psiServices = node.GetPsiServices()

        let records = WithPostfixTemplate.tryGetContext node

        let occurrences = 
            records
            |> Array.ofList
            |> Array.map (fun context ->
                // todo: shorten texts
                let range = context.CopyExpr.GetDocumentRange()
                WorkflowPopupMenuOccurrence(RichText(context.CopyExpr.GetText()), null, [context], [range])
            )

        let selectedOccurrence =
            let textControl = info.ExecutionContext.TextControl
            let popupMenu = psiServices.Solution.GetComponent<WorkflowPopupMenu>()
            popupMenu.ShowPopup(textControl.Lifetime, occurrences, CustomHighlightingKind.Other, textControl, null)

        if isNull selectedOccurrence then null else

        let record = Seq.head selectedOccurrence.Entities
        let recordExpr = WithPostfixTemplate.createRecordExpr record.CopyExpr record.Fields factory

        psiServices.Transactions.Execute(x.ExpandCommandName, fun _ ->
             use writeCookie = WriteLockCookie.Create(node.IsPhysical())
             use disableFormatter = new DisableCodeFormatter()

             ModificationUtil.ReplaceChild(node, recordExpr) :> ITreeNode
        )

    override x.AfterComplete(textControl, node, _) =
        let recordExpr = node :?> IRecordExpr

        let innermostExpr = WithPostfixTemplate.getInnermostRecordExpr recordExpr
        let range = innermostExpr.GetNavigationRange()
        textControl.Caret.MoveTo(range.EndOffset - 2, CaretVisualPlacement.DontScrollIfVisible)
        textControl.RescheduleCompletion(node.GetSolution())
