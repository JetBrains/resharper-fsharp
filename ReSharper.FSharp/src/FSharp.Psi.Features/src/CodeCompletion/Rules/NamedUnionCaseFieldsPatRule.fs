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
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExpectedTypes
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Resources

[<Language(typeof<FSharpLanguage>)>]
type NamedUnionCaseFieldsPatRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    // Find the parameterOwner `A` in `A()` or `A(a = a; )`
    // Filter out the already used fields
    let getFieldsFromParametersOwnerPat (parametersOwnerPat: IParametersOwnerPat) (filterFields: Set<string>) =
        let referenceName = parametersOwnerPat.ReferenceName
        if isNull referenceName then None else

        // We need to figure out if `A` actually is a UnionCase.
        let fcsUnionCase = referenceName.Reference.GetFcsSymbol().As<FSharpUnionCase>()
        if isNull fcsUnionCase then None else

        let fieldNames =
            fcsUnionCase.Fields
            |> Seq.choose (fun field -> if field.IsNameGenerated then None else Some field.Name)
            |> Seq.toArray

        // Only give auto completion to the fields only when all of them are named.
        if fieldNames.Length <> fcsUnionCase.Fields.Count then None else
        if Set.isEmpty filterFields then Some fieldNames else
        fieldNames
        |> Array.except filterFields
        |> Some

    // The current scope is to have A({caret}) captured.
    // A fake identifier will be inserted in the reparseContext.
    let getFieldsFromReference (context: FSharpCodeCompletionContext) =
        let parametersOwnerPat = getParametersOwnerPatFromReference context.ReparsedContext.Reference
        if isNull parametersOwnerPat then None else

        let filteredItems =
            let namedUnionCaseFieldsPat = parametersOwnerPat.Parameters.SingleItem.As<INamedUnionCaseFieldsPat>()
            if isNull namedUnionCaseFieldsPat then Set.empty else

            namedUnionCaseFieldsPat.FieldPatterns
            |> Seq.map (fun fieldPat -> fieldPat.ShortName)
            |> Seq.filter (fun name -> name <> SharedImplUtil.MISSING_DECLARATION_NAME)
            |> Set.ofSeq

        getFieldsFromParametersOwnerPat parametersOwnerPat filteredItems

    override this.IsAvailable(context) =
        let fieldNames = getFieldsFromReference context
        Option.isSome fieldNames

    override this.TransformItems(context, collector) =
        let parametersOwnerPat = getParametersOwnerPatFromReference context.ReparsedContext.Reference
        let namedUnionCaseFieldsPat = if isNull parametersOwnerPat then null else parametersOwnerPat.Parameters.SingleItem.As<INamedUnionCaseFieldsPat>()
        if isNotNull namedUnionCaseFieldsPat then
            // In this scenario we are already in a named union case field pattern.
            // Thus there is no need to show any other completions.
            collector.RemoveWhere(fun item -> true)

        let fieldNames = getFieldsFromReference context
        match fieldNames with
        | None -> ()
        | Some fieldNames ->

        for fieldName in fieldNames do
            let info = TextualInfo(fieldName, fieldName, Ranges = context.Ranges)
            let item =
                LookupItemFactory
                    .CreateLookupItem(info)
                    .WithPresentation(fun _ ->
                        TextPresentation(info, fieldName, true, PsiSymbolsThemedIcons.Field.Id))
                    .WithBehavior(fun _ -> TextualBehavior(info))
                    .WithMatcher(fun _ -> TextualMatcher(info))
                    // Force the items to be on top in the list.
                    .WithRelevance(CLRLookupItemRelevance.ExpectedTypeMatch)

            let tailNodeTypes =
                [| FSharpTokenType.WHITESPACE
                   FSharpTokenType.EQUALS
                   FSharpTokenType.WHITESPACE
                   TailType.CaretTokenNodeType.Instance |]

            item.SetTailType(SimpleTailType(" = ", tailNodeTypes, SkipTypings = [|" = "; "= "|]))

            collector.Add(item)
