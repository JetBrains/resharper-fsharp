namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open System.Collections.Generic
open FSharp.Compiler.EditorServices
open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExpectedTypes
open JetBrains.ReSharper.Psi.Resources
open JetBrains.ReSharper.Psi.Tree

[<Language(typeof<FSharpLanguage>)>]
type RecordFieldRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    let matchedFieldRelevance =
        CLRLookupItemRelevance.ExpectedTypeMatchInitializer |||
        CLRLookupItemRelevance.ExpectedTypeMatch |||
        CLRLookupItemRelevance.FieldsAndProperties

    let getRecordExprFromFieldReference (reference: FSharpSymbolReference) =
        let referenceName = reference.GetElement().As<IExpressionReferenceName>()
        let fieldBinding = RecordFieldBindingNavigator.GetByReferenceName(referenceName)
        RecordExprNavigator.GetByFieldBinding(fieldBinding)

    let getRecordReference (context: FSharpCodeCompletionContext) =
        match context.FcsCompletionContext.CompletionContext with
        | Some (CompletionContext.RecordField _) ->
            match context.ReparsedContext.Reference with
            | :? RecordCtorReference as r -> r
            | :? FSharpSymbolReference as fsRef ->
                let recordExpr = getRecordExprFromFieldReference fsRef
                if isNotNull recordExpr then recordExpr.Reference :?> _ else null
            | _ -> null
        | _ -> null

    let getRecordEntity (context: FSharpCodeCompletionContext) =
        let getRecordFromExprType (expr: IFSharpExpression) =
            if isNull expr then None else

            let expr = expr.TryGetOriginalRecordExprThroughSandBox()
            if isNull expr then None else

            let fcsType = expr.TryGetFcsType()
            if isNull fcsType || not fcsType.HasTypeDefinition then None else

            let fcsEntity = fcsType.TypeDefinition
            if not fcsEntity.IsFSharpRecord then None else

            Some(fcsEntity, expr)

        let rec getRecordEntity (reference: FSharpSymbolReference) =
            match reference with
            | :? RecordCtorReference as reference ->
                match reference.GetFcsSymbol() with
                | :? FSharpEntity as fcsEntity when fcsEntity.IsFSharpRecord ->
                    Some(fcsEntity, reference.RecordExpr :> IFSharpExpression)
                | _ ->
                    getRecordFromExprType reference.RecordExpr
            | _ -> None

        getRecordReference context
        |> Option.ofObj
        |> Option.bind getRecordEntity
        |> Option.orElseWith (fun _ ->
            match context.ReparsedContext.Reference with
            | :? FSharpSymbolReference as ref ->
                match ref.GetElement() with
                | :? IReferenceExpr as refExpr ->
                    let computationExpr = ComputationExprNavigator.GetByExpression(refExpr)
                    getRecordFromExprType computationExpr
                    |> Option.orElseWith (fun _ ->
                        if isNull computationExpr then None else

                        let expr = computationExpr.TryGetOriginalRecordExprThroughSandBox()
                        if isNotNull expr && not refExpr.IsQualified then Some (Unchecked.defaultof<_>, computationExpr) else None
                    )
                | _ ->
                    let recordExpr = getRecordExprFromFieldReference ref
                    if isNotNull recordExpr then getRecordEntity recordExpr.Reference else None
            | _ -> None
        )

    override this.IsAvailable(context) =
        context.IsBasicOrSmartCompletion &&
        not context.IsQualified &&
        Option.isSome (getRecordEntity context)

    override this.TransformItems(context, collector) =
        match getRecordEntity context with
        | None -> ()
        | Some(fcsEntity, expr) ->

        let fcsEntity =
            if isNull fcsEntity then None else Some fcsEntity

        let displayContext = FSharpDisplayContext.Empty.WithShortTypeNames(true)

        let fieldNames =
            fcsEntity
            |> Option.map (fun fcsEntity ->
                fcsEntity.FSharpFields
                |> Seq.map _.Name
                |> HashSet
            )

        let usedNames =
            let bindings = 
                match expr with
                | :? IRecordExpr as recordExpr -> recordExpr.FieldBindings
                | _ -> TreeNodeCollection.Empty

            bindings
            |> Seq.choose (fun fieldBinding -> Option.ofObj fieldBinding.ReferenceName)
            |> Seq.map _.ShortName
            |> HashSet

        let removedFields = List()

        collector.RemoveWhere(fun item ->
            match item with
            | :? FcsLookupItem as fcsItem ->
                match fcsItem.FcsSymbol with
                | :? FSharpField as field ->
                    match fieldNames with
                    | Some fieldNames ->
                        fieldNames.Contains(field.Name)
                    | None ->
                        match field.DeclaringEntity with
                        | None -> false
                        | Some fcsEntity ->
                            if fcsEntity.IsFSharpRecord then
                                removedFields.Add(field)
                                true
                            else
                                false
                | _ -> false
            | _ -> false
        )

        let fields = 
            match fcsEntity with
            | None -> removedFields :> IList<_>
            | Some fcsEntity -> fcsEntity.FSharpFields

        let emphasize = fcsEntity.IsSome

        for field in fields do
            let name = field.Name
            if usedNames.Contains(name) then () else

            let item =
                let info = TextualInfo(name, name, Ranges = context.Ranges)
                LookupItemFactory.CreateLookupItem(info)
                    .WithPresentation(fun _ ->
                        let typeText = field.FieldType.Format(displayContext)
                        TextPresentation(info, typeText, emphasize, PsiSymbolsThemedIcons.Field.Id) :> _)
                    .WithBehavior(fun _ -> TextualBehavior(info))
                    .WithMatcher(fun _ -> TextualMatcher(info) :> _)

            let item: ILookupItem =
                if not emphasize then item else

                let item = item.WithRelevance(matchedFieldRelevance)
                item.Placement.Location <- PlacementLocation.Top
                item

            collector.Add(item)
