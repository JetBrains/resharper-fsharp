namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

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
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExpectedTypes
open JetBrains.ReSharper.Psi.Resources

[<Language(typeof<FSharpLanguage>)>]
type NamedUnionCaseFieldsPatRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    // Find the parameterOwner `A` in `A()` or `A(a = a; )`
    // Filter out the already used fields
    let getFieldsFromParametersOwnerPat (parametersOwnerPat: IParametersOwnerPat) (filterFields: Set<string>) =
        if isNull parametersOwnerPat then Array.empty else
        // We need to figure out if `A` actually is a UnionCase.
        let fcsUnionCase = parametersOwnerPat.ReferenceName.Reference.GetFcsSymbol().As<FSharpUnionCase>()
        if isNull fcsUnionCase then Array.empty else

        let fieldNames =
            fcsUnionCase.Fields
            |> Seq.choose (fun field -> if field.IsNameGenerated then None else Some field.Name)
            |> Seq.toArray

        // Only give auto completion to the fields only when all of them are named.
        if fieldNames.Length <> fcsUnionCase.Fields.Count then Array.empty else
        if Set.isEmpty filterFields then fieldNames else
        fieldNames
        |> Array.except filterFields

    let getParametersOwnerPatAndFilteredItemsFromReference (context: FSharpCodeCompletionContext) : IParametersOwnerPat * Set<string> =
        let reference = context.ReparsedContext.Reference.As<FSharpSymbolReference>()
        if isNull reference then (null, Set.empty) else

        let exprRefName = reference.GetElement().As<IExpressionReferenceName>()
        if isNull exprRefName || exprRefName.IsQualified then (null, Set.empty) else

        let refPat = ReferencePatNavigator.GetByReferenceName(exprRefName)
        let parentPat : IFSharpPattern =
            // Try finding a reference pattern in case there is no existing named access.
            if isNotNull refPat then
                ParenPatNavigator.GetByPattern(refPat)
            else
                // Otherwise, try and find an incomplete field pat
                let fieldPat = FieldPatNavigator.GetByReferenceName(exprRefName)
                NamedUnionCaseFieldsPatNavigator.GetByFieldPattern(fieldPat)

        let filteredItems =
            match parentPat with
            | :? INamedUnionCaseFieldsPat as namedUnionCaseFieldsPat ->
                namedUnionCaseFieldsPat.FieldPatterns
                |> Seq.choose (fun fieldPat ->
                    if isNull fieldPat.ReferenceName
                       || isNull fieldPat.ReferenceName.Identifier then
                        None
                    else
                        Some fieldPat.ReferenceName.Identifier.Name)
                |> Set.ofSeq
            | _ -> Set.empty
        
        ParametersOwnerPatNavigator.GetByParameter(parentPat), filteredItems
    
    // The current scope is to have A({caret}) captured.
    // A fake identifier will be inserted in the reparseContext.
    let getFieldsFromReference (context: FSharpCodeCompletionContext) =
        let parametersOwnerPat, filteredItems = getParametersOwnerPatAndFilteredItemsFromReference context
        getFieldsFromParametersOwnerPat parametersOwnerPat filteredItems

    override this.IsAvailable(context) =
        let fieldNames = getFieldsFromReference context
        not (Array.isEmpty fieldNames)

    override this.TransformItems(context, collector) =
        let parametersOwnerPat, _ = getParametersOwnerPatAndFilteredItemsFromReference context
        let namedUnionCaseFieldsPat = if isNull parametersOwnerPat then null else parametersOwnerPat.Parameters.SingleItem.As<INamedUnionCaseFieldsPat>()
        if isNotNull namedUnionCaseFieldsPat then
            // In this scenario we are already in a named union case field pattern.
            // Thus there is no need to show any other completions.
            collector.RemoveWhere(fun item -> true)

        let fieldNames = getFieldsFromReference context
        assert (not (Array.isEmpty fieldNames))

        for fieldName in fieldNames do
            let info = TextualInfo(fieldName, fieldName, Ranges = context.Ranges)
            let item =
                LookupItemFactory
                    .CreateLookupItem(info)
                    .WithPresentation(fun _ ->
                        TextPresentation(info, fieldName, true, PsiSymbolsThemedIcons.Field.Id) :> _)
                    .WithBehavior(fun _ -> TextualBehavior(info) :> _)
                    .WithMatcher(fun _ -> TextualMatcher(info) :> _)
                    // Force the items to be on top in the list.
                    .WithRelevance(CLRLookupItemRelevance.ExpectedTypeMatch)

            let tailNodeTypes =
                [| FSharpTokenType.WHITESPACE
                   FSharpTokenType.EQUALS
                   FSharpTokenType.WHITESPACE
                   TailType.CaretTokenNodeType.Instance |]

            item.SetTailType(SimpleTailType(" = ", tailNodeTypes, SkipTypings = [|" = "; "= "|]))

            collector.Add(item)
