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

    let getCopyInfoReferenceExpr (expr: IRecordExpr) =
        match expr.CopyInfoExpression.IgnoreInnerParens() with
        | :? IReferenceExpr as copyExpr when isSimpleQualifiedName copyExpr -> copyExpr
        | _ -> null

    let produceHighlighting fieldsChainMatch (innerFieldBinding: IRecordFieldBinding) (consumer: IHighlightingConsumer) =
        match fieldsChainMatch with
        | ValueNone -> ()
        | ValueSome(outerFieldBinding, qualifiedFieldNameReversed) ->

        if isNull innerFieldBinding.Expression then ()
        elif not (innerFieldBinding.ReferenceName.Reference.GetFcsSymbol() :? FSharpField) then () else
        let qualifiedFieldName = qualifiedFieldNameReversed |> List.rev
        consumer.AddHighlighting(NestedRecordUpdateCanBeSimplifiedWarning(outerFieldBinding, innerFieldBinding, qualifiedFieldName))

    let rec compareReferenceExprs (x: IReferenceExpr) (y: IReferenceExpr) =
        if isNull x then isNull y
        elif isNull y then isNull x
        elif x.ShortName <> y.ShortName then false
        else compareReferenceExprs (x.Qualifier.As()) (y.Qualifier.As())

    let rec compareFieldWithNextCopyExpr (previousCopyExpr: IReferenceExpr) (fieldName: IReferenceName) (copyExpr: IReferenceExpr) =
        if isNull copyExpr then false
        elif isNull fieldName then compareReferenceExprs copyExpr previousCopyExpr
        elif fieldName.ShortName <> copyExpr.ShortName then
            compareReferenceExprs copyExpr previousCopyExpr && not (fieldName.Reference.GetFcsSymbol() :? FSharpField)
        else compareFieldWithNextCopyExpr previousCopyExpr fieldName.Qualifier (copyExpr.Qualifier.As())

    let rec collectRecordExprsToSimplify recordExpr previousFieldBinding previousCopyExpr fieldsChainMatch (consumer: IHighlightingConsumer) =
        let copyExpr = getCopyInfoReferenceExpr recordExpr
        if isNull copyExpr then produceHighlighting fieldsChainMatch previousFieldBinding consumer else

        let fieldBindings = recordExpr.FieldBindings
        let singleField = fieldBindings.SingleItem
        let hasFieldsMatch =
            isNotNull previousFieldBinding &&
            compareFieldWithNextCopyExpr previousCopyExpr previousFieldBinding.ReferenceName copyExpr
        let searchInDepthWhileMatched = hasFieldsMatch && isNotNull singleField

        for i, currentFieldBinding in Seq.indexed fieldBindings do
            let isSingleLine = currentFieldBinding.IsSingleLine
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
                    if isSingleLine then produceHighlighting fieldsChainMatch previousFieldBinding consumer
                    ValueNone

            match currentFieldBinding.Expression.IgnoreInnerParens() with
            | :? IRecordExpr as recordExpr ->
                let nextFieldIdx = i + 1
                let nextFieldOnTheSameLine =
                    if nextFieldIdx = fieldBindings.Count then false else
                    fieldBindings[nextFieldIdx].StartLine = currentFieldBinding.EndLine

                if fieldsChainMatch.IsNone && not isSingleLine && nextFieldOnTheSameLine then
                    collectRecordExprsToSimplify recordExpr null null ValueNone consumer
                else
                    collectRecordExprsToSimplify recordExpr currentFieldBinding copyExpr fieldsChainMatch consumer
            | _ ->
                if not searchInDepthWhileMatched || not isSingleLine then () else
                produceHighlighting fieldsChainMatch currentFieldBinding consumer

    override this.Run(recordExpr, data, consumer) =
        if not data.IsFSharp80Supported then () else

        let isInnerRecordExpr =
            recordExpr.IgnoreParentParens()
            |> RecordFieldBindingNavigator.GetByExpression
            |> isNotNull

        if isInnerRecordExpr then () else
        collectRecordExprsToSimplify recordExpr null null ValueNone consumer
