namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Analyzers.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings.UseNestedRecordFieldSyntax
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.PsiUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree

[<ElementProblemAnalyzer([| typeof<IRecordExpr> |], HighlightingTypes = [| typeof<NestedRecordUpdateCanBeSimplifiedWarning> |])>]
type RecordExprAnalyzer() =
    inherit ElementProblemAnalyzer<IRecordExpr>()

    let getCopyInfoExpressionAsReferenceExpr (expr: IRecordExpr) =
        match expr.CopyInfoExpression.IgnoreInnerParens() with
        | :? IReferenceExpr as copyRefExpr when isSimpleQualifiedName copyRefExpr -> copyRefExpr
        | _ -> null

    let produceHighlighting fieldsChainMatch (innerFieldBinding: IRecordFieldBinding) (consumer: IHighlightingConsumer) =
        match fieldsChainMatch with
        | ValueNone -> ()
        | ValueSome(outerFieldBinding, qualifiedFieldNameReversed) ->
        if not (innerFieldBinding.ReferenceName.Reference.GetFcsSymbol() :? FSharpField) then () else
        let qualifiedFieldName = qualifiedFieldNameReversed |> List.rev
        consumer.AddHighlighting(NestedRecordUpdateCanBeSimplifiedWarning(outerFieldBinding, innerFieldBinding, qualifiedFieldName))

    let rec compareReferenceExprs (x: IReferenceExpr) (y: IReferenceExpr) =
        if isNull x then isNull y
        elif isNull y then isNull x
        elif x.ShortName <> y.ShortName then false
        else compareReferenceExprs (x.Qualifier.As()) (y.Qualifier.As())

    let rec compareFieldWithNextCopyRefExpr (previousCopyRefExpr: IReferenceExpr) (fieldName: IReferenceName) (copyRefExpr: IReferenceExpr) =
        if isNull copyRefExpr then false
        elif isNull fieldName then compareReferenceExprs copyRefExpr previousCopyRefExpr
        elif fieldName.ShortName <> copyRefExpr.ShortName then
            compareReferenceExprs copyRefExpr previousCopyRefExpr && not (fieldName.Reference.GetFcsSymbol() :? FSharpField)
        else compareFieldWithNextCopyRefExpr previousCopyRefExpr fieldName.Qualifier (copyRefExpr.Qualifier.As())

    let rec collectRecordExprsToSimplify recordExpr previousFieldBinding previousCopyRefExpr fieldsChainMatch (consumer: IHighlightingConsumer) =
        let copyRefExpr = getCopyInfoExpressionAsReferenceExpr recordExpr
        if isNull copyRefExpr then produceHighlighting fieldsChainMatch previousFieldBinding consumer else

        let fieldBindings = recordExpr.FieldBindingsEnumerable
        let singleField = fieldBindings.SingleItem
        let hasFieldsMatch =
            isNotNull previousFieldBinding &&
            compareFieldWithNextCopyRefExpr previousCopyRefExpr previousFieldBinding.ReferenceName copyRefExpr
        let searchInDepthWhileMatched = hasFieldsMatch && isNotNull singleField

        for currentFieldBinding in fieldBindings do
            let fieldsChainMatch =
                if searchInDepthWhileMatched then
                    match fieldsChainMatch with
                    | ValueNone ->
                        let fieldNamePrefixReversed = getNamesReversed previousFieldBinding.ReferenceName
                        let currentFieldNameReversed = currentFieldBinding.ReferenceName |> appendFieldNameReversed fieldNamePrefixReversed
                        ValueSome(previousFieldBinding, currentFieldNameReversed)
                    | ValueSome(rootFieldBinding, previousFieldNameReversed) ->
                        let currentFieldNameReversed = currentFieldBinding.ReferenceName |> appendFieldNameReversed previousFieldNameReversed
                        ValueSome(rootFieldBinding, currentFieldNameReversed)
                else
                    produceHighlighting fieldsChainMatch previousFieldBinding consumer
                    ValueNone

            match currentFieldBinding.Expression.IgnoreInnerParens() with
            | :? IRecordExpr as recordExpr ->
                collectRecordExprsToSimplify recordExpr currentFieldBinding copyRefExpr fieldsChainMatch consumer
            | _ ->
                if not searchInDepthWhileMatched then () else
                produceHighlighting fieldsChainMatch currentFieldBinding consumer

    override this.Run(recordExpr, data, consumer) =
        if not data.IsFSharp80Supported || not recordExpr.IsSingleLine then () else

        let isInnerRecordExpr =
            recordExpr.IgnoreParentParens()
            |> RecordFieldBindingNavigator.GetByExpression
            |> isNotNull

        if isInnerRecordExpr then () else
        collectRecordExprsToSimplify recordExpr null null ValueNone consumer
