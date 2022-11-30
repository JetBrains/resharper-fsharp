namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.PostfixTemplates

open System
open System.Collections.Generic
open System.Text.RegularExpressions
open FSharp.Compiler.Symbols
open JetBrains.Application.Environment
open JetBrains.Application.Environment.Helpers
open JetBrains.Application.Progress
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates
open JetBrains.ReSharper.Feature.Services.Navigation.CustomHighlighting
open JetBrains.ReSharper.Feature.Services.PostfixTemplates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates.Contexts
open JetBrains.ReSharper.Feature.Services.Refactorings.WorkflowOccurrences
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Search
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open System.Linq
open JetBrains.TextControl
open JetBrains.UI.RichText

module private Helpers =
    /// e.g. innerWith ["Customer"] "Name" "somename" -> "{ Customer with Name = somename }"
    let innerWith (prefix: string list) (propName: string) =
        let pref = prefix |> String.concat "."
        (fun (inner: string) -> $"""{{ {pref} with {propName} = {inner} }}""")

    /// createCopyAndUpdate ["Customer"; "Phone" ; "Number"] -> "{ Customer with Phone = { Customer.Phone with Number =  } }"
    let rec createCopyAndUpdate (identifiers: string list) =
        let rec loop
            (innerString: string)
            (usedIdentifers: string list)
            (identifiers: string list)
            =
            match identifiers with
            | []
            | [ _ ] -> innerString
            | _ ->
                let reference = usedIdentifers @ [ identifiers[0] ]
                let property = identifiers[1]

                let recursivepart =
                    identifiers[1 .. identifiers.Length - 1]

                innerWith reference property (loop innerString reference recursivepart)

        loop "" [] identifiers


    type RecordExprContext =
        {
            OuterRecord: IReferenceExpr option
            Fields: IReferenceExpr list
        }
        static member Default = { OuterRecord = None; Fields = [] }

    let fieldBelongsToRecord (fsf: FSharpField) =
        fsf.DeclaringEntity
        |> Option.map (fun f -> f.IsFSharpRecord)
        |> Option.defaultValue false

    /// Gets all record qualifiers, returns None if not a record field chain
    let rec tryGetContext (refExpr: IReferenceExpr) =
        let rec loop (acc: RecordExprContext) (refExpr: IReferenceExpr) =
            match refExpr.Qualifier with
            | null -> Some acc
            | :? IReferenceExpr as prev ->
                match prev.Reference.GetFcsSymbol() with
                | :? FSharpField as field ->
                    match fieldBelongsToRecord field with
                    | true -> loop { acc with Fields = prev :: acc.Fields } prev
                    | false -> None // found union case instead
                | :? FSharpMemberOrFunctionOrValue ->
                    match acc.OuterRecord with
                    // already have the outer qualified name
                    | Some _ -> Some acc
                    | _ -> loop { acc with OuterRecord = Some prev } prev
                | _ -> None
            // Some other expr, e.g. Class(r).field1.field2
            | _ -> Some acc
            

        loop RecordExprContext.Default refExpr

    /// Gets the innermost record expr in the generated recursive record expr
    let rec getInnermostRecordExpr (recordExpr: IRecordExpr) =
        // couldnt find a way to create a IRecordFieldBindingList with a single element
        // so i can only traverse by IRecordFieldBinding in children directly
        recordExpr.Children()
        |> Seq.tryPick (fun f ->
            match f with | :? IRecordFieldBinding as b -> Some b | _ -> None
        )
        |> Option.map (fun binding ->
            let innerExpression = binding.Expression
            match innerExpression with
            | null -> recordExpr
            | :? IRecordExpr as innerRecordExpr -> getInnermostRecordExpr innerRecordExpr
            | _ -> failwith "non recordexpr inner expression" 
        )
        |> Option.defaultValue recordExpr

    
    
    /// Creates recursive record expr from IReferenceExpr list
    let createRecordExpr
        (fields: IReferenceExpr list)
        (factory:IFSharpElementFactory)
        : IRecordExpr =
            
        let rec loop (xs:IReferenceExpr list) : IRecordExpr =
            match xs with
            | [] -> failwith "this should never happen"
            | head :: tail ->
                let currentRecordExpr = factory.CreateExpr("{ x with P = 1 }") :?> IRecordExpr
                ModificationUtil.ReplaceChild( currentRecordExpr.CopyInfoExpression, head.Qualifier ) |> ignore
                let fieldBindingExpr = factory.CreateRecordFieldBinding(head.ShortName,false)
                // either delete or add inner binding
                match tail with
                | [] -> ModificationUtil.DeleteChild(fieldBindingExpr.Expression) 
                | _ -> 
                    let innerBinding = loop tail
                    ModificationUtil.ReplaceChild(fieldBindingExpr.Expression, innerBinding) |> ignore
                //
                ModificationUtil.ReplaceChild(currentRecordExpr.FieldBindingList, fieldBindingExpr) |> ignore
                currentRecordExpr
            
        loop fields 
     
    /// Popup titles and respective record exprs to generate
    type PopupContext =
        { TitleAndExprList: (string*IRecordExpr) list; PrevFields: IReferenceExpr list; }
        static member Default = { TitleAndExprList = [] ; PrevFields = [] }
        
    let createPopupMenu (popupContext: PopupContext) =
        popupContext.TitleAndExprList
        |> Seq.map (fun (title,expr) ->
            WorkflowPopupMenuOccurrence(
                RichText(title),
                RichText.Empty,
                title,
                (fun appData -> [||])
            ))
        |> Array.ofSeq

    /// Creates popup titles and respective record expressions 
    let createPopupContext (ctx:RecordExprContext) (factory: IFSharpElementFactory): PopupContext =
        let defaultTitle = ctx.OuterRecord.Value :: ctx.Fields |> List.map (fun f -> f.ShortName)
        
        let rec loop (acc:PopupContext) (list: IReferenceExpr list) =
            match list with
            | [] -> acc
            | head :: tail ->
                let recordExpr = createRecordExpr (head :: acc.PrevFields) factory
                    
                let currTitle =
                    defaultTitle
                    |> Seq.skip (ctx.Fields.Length - acc.PrevFields.Length)
                    |> String.concat "."
                
                let acc' =
                    {
                        PopupContext.TitleAndExprList = (currTitle , recordExpr) :: acc.TitleAndExprList
                        PopupContext.PrevFields = head :: acc.PrevFields }
                loop acc' tail

        loop PopupContext.Default (ctx.Fields |> List.rev)

[<PostfixTemplate("with", "Copies and updates the record field", "{ record with field }")>]
type CopyAndUpdatePostfixTemplate() =
    inherit FSharpPostfixTemplateBase()

    let isApplicable (expr: IFSharpExpression) =
        let refExpr = expr.As<IReferenceExpr>()

        if isNull refExpr then
            false
        else
            match Helpers.tryGetContext refExpr with
            | None -> false
            | Some { Fields = [] } -> false
            | Some _ -> true


    override x.CreateBehavior(info) = CopyAndUpdatePostfixTemplateBehavior(info) :> _

    override x.TryCreateInfo(context) =
        let context = context.AllExpressions[0]

        let fsExpr =
            context.Expression.As<IFSharpExpression>()

        if not (isApplicable fsExpr) then
            null
        else
            CopyAndUpdatePostfixTemplateInfo(context) :> _

    override this.IsEnabled(solution) =
        let configurations =
            solution.GetComponent<RunsProducts.ProductConfigurations>()

        configurations.IsInternalMode()
        || ``base``.IsEnabled(solution)


and CopyAndUpdatePostfixTemplateInfo(expressionContext: PostfixExpressionContext) =
    inherit PostfixTemplateInfo("with", expressionContext)


and CopyAndUpdatePostfixTemplateBehavior(info) =
    inherit FSharpPostfixTemplateBehaviorBase(info)

    override x.ExpandPostfix(context) =
        let psiModule = context.PostfixContext.PsiModule
        let psiServices = psiModule.GetPsiServices()

        let popupMenu =
            psiServices.Solution.GetComponent<WorkflowPopupMenu>()

        let textControl = info.ExecutionContext.TextControl

        let lifetime =
            // textControl.Lifetime // closes instantly
            psiServices
                .Solution
                .GetSolutionLifetimes()
                .UntilSolutionCloseLifetime

        let showPopup occurrences id =
            popupMenu.ShowPopup(
                lifetime,
                occurrences,
                CustomHighlightingKind.Other,
                textControl,
                null,
                id
            )

        

        psiServices.Transactions.Execute(
            x.ExpandCommandName,
            fun _ ->
                let node = context.Expression :?> IReferenceExpr
                let factory = node.CreateElementFactory()


                match node |> Helpers.tryGetContext with
                | None -> context.Expression // something failed, do nothing
                | Some ctx ->
                    
                    let popupContext = Helpers.createPopupContext ctx factory
                    let popupMenu = popupContext |> Helpers.createPopupMenu

                    let chosenPopup =
                        showPopup popupMenu (nameof CopyAndUpdatePostfixTemplate)

                    use writeCookie =
                        WriteLockCookie.Create(node.IsPhysical())

                    use disableFormatter = new DisableCodeFormatter()

                    match chosenPopup with
                    | null -> context.Expression
                    | occ ->
                        popupContext.TitleAndExprList
                        |> Seq.tryFind (fun (title,expr) -> title = occ.Name.Text)
                        |> Option.map (fun (title,expr) ->
                            ModificationUtil.ReplaceChild(node.Parent, expr) :> ITreeNode)
                        |> Option.defaultValue context.Expression

        )

    override x.AfterComplete(textControl, node, _) =
        let recordExpr = node.As<IRecordExpr>()

        if isNull recordExpr then
            ()
        else
            let innermostExpr =
                Helpers.getInnermostRecordExpr recordExpr

            let range = innermostExpr.GetNavigationRange()
            textControl.Caret.MoveTo(range.EndOffset - 1, CaretVisualPlacement.DontScrollIfVisible)
