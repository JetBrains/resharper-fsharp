namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.PostfixTemplates

open System
open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.Application.Environment
open JetBrains.Application.Environment.Helpers
open JetBrains.Application.Progress
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates.Contexts
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
            Fields: string list
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
                    | true -> loop { acc with Fields = field.Name :: acc.Fields } prev
                    | false -> None // found union case instead
                | :? FSharpMemberOrFunctionOrValue as outer ->
                    match acc.OuterRecord with
                    // would have multiple function calls, don't want that
                    // e.g { r() with r1 = { r().r1 with {caret} }
                    | _ when outer.IsFunction -> None
                    // already have the outer qualified name
                    | Some _ -> Some acc
                    | _ -> loop { acc with OuterRecord = Some prev } prev
                | _ -> None
            | _ -> None

        loop RecordExprContext.Default refExpr

    /// Gets the innermost record expr in the generated recursive record expr
    let rec getInnermostExpr (recordExpr: IRecordExpr) =
        match recordExpr.FieldBindingList with
        | null -> recordExpr
        | bindingList ->
            let innerChildren =
                // there is only one child in bindingList
                bindingList
                    .Children()
                    .First()
                    .As<IRecordFieldBinding>()
                    .Children()

            innerChildren
            |> Seq.tryPick (fun f ->
                match f with
                | :? IRecordExpr as innerRecordExpr -> Some(getInnermostExpr innerRecordExpr)
                | _ -> None)
            |> Option.defaultValue recordExpr


[<PostfixTemplate("with", "Copies and updates the record field", "{ record with field }")>]
type CopyAndUpdatePostfixTemplate() =
    inherit FSharpPostfixTemplateBase()

    //FSharpGlobalUtil
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

        psiServices.Transactions.Execute(
            x.ExpandCommandName,
            fun _ ->
                let node = context.Expression :?> IReferenceExpr

                match node |> Helpers.tryGetContext with
                | None -> context.Expression // same expression
                | Some ctx ->
                    use writeCookie =
                        WriteLockCookie.Create(node.IsPhysical())

                    use disableFormatter = new DisableCodeFormatter()

                    let factory = node.CreateElementFactory()

                    let copyAndUpdate =
                        ctx.OuterRecord.Value.QualifiedName :: ctx.Fields
                        |> Helpers.createCopyAndUpdate

                    let newExpr = factory.CreateExpr(copyAndUpdate)
                    ModificationUtil.ReplaceChild(node.Parent, newExpr) :> ITreeNode
        )

    override x.AfterComplete(textControl, node, _) =
        let recordExpr = node.As<IRecordExpr>()

        if isNull recordExpr then
            ()
        else
            let innermostExpr =
                Helpers.getInnermostExpr recordExpr

            let range = innermostExpr.GetNavigationRange()
            textControl.Caret.MoveTo(range.EndOffset - 2, CaretVisualPlacement.DontScrollIfVisible)
