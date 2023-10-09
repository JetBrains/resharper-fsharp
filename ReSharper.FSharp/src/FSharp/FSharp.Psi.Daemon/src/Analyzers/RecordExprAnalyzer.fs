namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Analyzers

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

    let produceHighlighting fieldsChainMatch fieldUpdateExpr (consumer: IHighlightingConsumer) =
        match fieldsChainMatch with
        | ValueNone -> ()
        | ValueSome(fieldBinding, qualifiedFieldNameReversed) ->
        let qualifiedFieldName = qualifiedFieldNameReversed |> List.rev
        consumer.AddHighlighting(NestedRecordUpdateCanBeSimplifiedWarning(fieldBinding, qualifiedFieldName, fieldUpdateExpr))

    let rec collectRecordExprsToSimplify recordExpr (previousFieldBinding: IRecordFieldBinding) previousFieldNameReversed fieldsChainMatch
        (consumer: IHighlightingConsumer) =

        let copyRefExpr = getCopyInfoExpressionAsReferenceExpr recordExpr
        if isNull copyRefExpr then produceHighlighting fieldsChainMatch recordExpr consumer else

        let copyRefExprNamesReversed = getRefExprNamesReversed copyRefExpr
        let fieldBindings = recordExpr.FieldBindingsEnumerable
        let singleField = fieldBindings.SingleItem
        let hasFieldsMatch = isNotNull previousFieldBinding && previousFieldNameReversed = copyRefExprNamesReversed
        let searchInDepthWhileMatched = hasFieldsMatch && isNotNull singleField

        for currentFieldBinding in fieldBindings do
            let currentFieldNameReversed = currentFieldBinding.ReferenceName |> appendFieldNameReversed copyRefExprNamesReversed
            let fieldsChainMatch =
                if searchInDepthWhileMatched then
                    let fieldNamePrefixReversed =
                        match fieldsChainMatch with
                        | ValueNone -> getNamesReversed previousFieldBinding.ReferenceName
                        | ValueSome(_, previousFieldNameReversed) -> previousFieldNameReversed

                    let currentFieldNameReversed = currentFieldBinding.ReferenceName |> appendFieldNameReversed fieldNamePrefixReversed
                    ValueSome(previousFieldBinding, currentFieldNameReversed)                    
                else
                    produceHighlighting fieldsChainMatch recordExpr consumer
                    ValueNone

            match currentFieldBinding.Expression.IgnoreInnerParens() with
            | :? IRecordExpr as recordExpr ->
                collectRecordExprsToSimplify recordExpr currentFieldBinding currentFieldNameReversed fieldsChainMatch consumer
            | expr ->
                if not searchInDepthWhileMatched then () else
                produceHighlighting fieldsChainMatch expr consumer

    override this.Run(recordExpr, data, consumer) =
        if not data.IsFSharp80Supported || not recordExpr.IsSingleLine then () else

        let isInnerRecordExpr =
            recordExpr.IgnoreParentParens()
            |> RecordFieldBindingNavigator.GetByExpression
            |> isNotNull

        if isInnerRecordExpr then () else
        collectRecordExprsToSimplify recordExpr null [] ValueNone consumer
