namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open System.Collections.Generic
open System.Linq
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
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExpectedTypes
open JetBrains.ReSharper.Psi.Resources
open JetBrains.ReSharper.Psi.Tree
open JetBrains.UI.RichText

type NamedUnionCaseFieldItem(text,identity, ranges) =
    inherit TextualInfo(text,identity, Ranges = ranges)
    override this.IsRiderAsync = false

[<Language(typeof<FSharpLanguage>)>]
type NamedUnionCaseFieldsPatRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()
    
    // Find the parameterOwner `A` in `A()` or `A(a = a; )`
    // Filter out the already used fields
    let getFieldsFromParametersOwnerPat (parameterPat: IFSharpPattern) (filterFields: Set<string>) =
        let parametersOwnerPat = ParametersOwnerPatNavigator.GetByParameter(parameterPat)
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
    
    // The current scope is to have A({caret}) captured.
    // A fake identifier will be inserted in the reparseContext.
    let getFieldsFromReference (context: FSharpCodeCompletionContext) =
        let reference = context.ReparsedContext.Reference.As<FSharpSymbolReference>()
        if isNull reference then Array.empty else

        let exprRefName = reference.GetElement().As<IExpressionReferenceName>()
        if isNull exprRefName then Array.empty else
        let refPat = ReferencePatNavigator.GetByReferenceName(exprRefName)
        if isNull refPat then Array.empty else

        // I'm assuming that the parent of the fake refPat is a IParenPat for now.
        let parentPat = refPat.Parent :?> IParenPat
        if isNull parentPat then Array.empty else
        getFieldsFromParametersOwnerPat parentPat Set.empty

    // Assumption: A(a = foo; {caret})
    let getFieldsAfterSemicolon (context: FSharpCodeCompletionContext) =
        let potentialNamedUnionCaseFieldsPat =
            if isSemicolon context.TokenBeforeCaret then
                context.TokenBeforeCaret.Parent
            elif isWhitespace context.TokenBeforeCaret then
                // This is rather weird though
                // I was expecting the PrevSibling to be the semicolon instead.
                match context.TokenBeforeCaret.PrevSibling with
                | :? IParametersOwnerPat as ownerPat when ownerPat.Parameters.Count = 1 ->
                    ownerPat.Parameters.[0]
                | _ -> null
            else
                null

        if isNull potentialNamedUnionCaseFieldsPat then Array.empty else
        match potentialNamedUnionCaseFieldsPat with
        | :? INamedUnionCaseFieldsPat as namedUnionCaseFieldsPat ->
            let usedFields =
                namedUnionCaseFieldsPat.FieldPatterns
                |> Seq.map (fun fieldPat -> fieldPat.ReferenceName.Identifier.Name)
                |> set

            getFieldsFromParametersOwnerPat namedUnionCaseFieldsPat usedFields
        | _ -> Array.empty

    let getFields context =
        let fieldNames = getFieldsFromReference context
        if not (Array.isEmpty fieldNames) then
            fieldNames
        else
            getFieldsAfterSemicolon context

    override this.IsAvailable(context) =
        let fieldNames = getFields context
        not (Array.isEmpty fieldNames)
    
    override this.AddLookupItems(context, collector) =
        let fieldNames = getFields context
        assert (not (Array.isEmpty fieldNames))
        for fieldName in fieldNames do
            let info = NamedUnionCaseFieldItem(fieldName, fieldName, context.Ranges)
            let item =
                LookupItemFactory
                    .CreateLookupItem(info)
                    .WithPresentation(fun _ ->
                        TextPresentation(info, fieldName, true, PsiSymbolsThemedIcons.Field.Id) :> _)
                    .WithBehavior(fun _ -> TextualBehavior(info) :> _)
                    .WithMatcher(fun _ -> TextualMatcher(info) :> _)
                    .WithRelevance(CLRLookupItemRelevance.NamedArguments ||| CLRLookupItemRelevance.FieldsAndProperties)
                    .WithHighSelectionPriority()

            let tailNodeTypes =
                [| FSharpTokenType.WHITESPACE
                   FSharpTokenType.EQUALS
                   FSharpTokenType.WHITESPACE
                   TailType.CaretTokenNodeType.Instance |]

            item.SetTailType(SimpleTailType(" = ", tailNodeTypes, SkipTypings = [|" = "; "= "|]))
            item.Placement.Location <- PlacementLocation.Top
            
            collector.Add(item)

        true