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

    let produceHighlighting fieldsChainMatch fieldUpdateExpr (consumer: IHighlightingConsumer) =
        match fieldsChainMatch with
        | ValueNone -> ()
        | ValueSome(fieldBinding, qualifiedFieldNameReversed) ->
        let qualifiedFieldName = qualifiedFieldNameReversed |> List.rev
        consumer.AddHighlighting(NestedRecordUpdateCanBeSimplifiedWarning(fieldBinding, qualifiedFieldName, fieldUpdateExpr))

    let isSimpleReferencePart (ref: IReferenceExpr) =
        isNull ref.TypeArgumentList && ref.Identifier :? IFSharpIdentifierToken

    let rec compareReferenceExprs x y =
        if isNull x then isNull y
        elif isNull y then isNull x
        elif not (isSimpleReferencePart x) || not (isSimpleReferencePart y) then false
        elif x.ShortName <> y.ShortName then false
        else compareReferenceExprs (x.Qualifier.As<_>()) (y.Qualifier.As<_>())

    let rec compareFieldWithNextCopyRefExpr (previousCopyRefExpr: IReferenceExpr) (fieldName: IReferenceName) (copyRefExpr: IReferenceExpr) =
        if isNull copyRefExpr then isNull fieldName
        elif isNull fieldName then compareReferenceExprs copyRefExpr previousCopyRefExpr
        elif not (isSimpleReferencePart copyRefExpr) then false
        elif fieldName.ShortName <> copyRefExpr.ShortName then
            compareReferenceExprs copyRefExpr previousCopyRefExpr && not (fieldName.Reference.GetFcsSymbol() :? FSharpField)
        else compareFieldWithNextCopyRefExpr previousCopyRefExpr fieldName.Qualifier (copyRefExpr.Qualifier.As<_>())

    let rec collectRecordExprsToSimplify recordExpr (previousFieldBinding: IRecordFieldBinding) previousCopyRefExpr fieldsChainMatch (consumer: IHighlightingConsumer) =

        let copyRefExpr = getCopyInfoExpressionAsReferenceExpr recordExpr
        if isNull copyRefExpr then produceHighlighting fieldsChainMatch recordExpr consumer else

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
                    produceHighlighting fieldsChainMatch recordExpr consumer
                    ValueNone

            match currentFieldBinding.Expression.IgnoreInnerParens() with
            | :? IRecordExpr as recordExpr ->
                collectRecordExprsToSimplify recordExpr currentFieldBinding copyRefExpr fieldsChainMatch consumer
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
        collectRecordExprsToSimplify recordExpr null null ValueNone consumer
