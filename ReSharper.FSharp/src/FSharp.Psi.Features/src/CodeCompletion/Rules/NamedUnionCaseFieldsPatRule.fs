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
    let mutable fieldNames = Array.empty
    
    override this.IsAvailable(context) =
        // The current scope is to have A({caret}) captured.
        // A fake identifier will be inserted in the reparseContext.
        
        let reference = context.ReparsedContext.Reference.As<FSharpSymbolReference>()
        if isNull reference then false else

        let exprRefName = reference.GetElement().As<IExpressionReferenceName>()
        if isNull exprRefName then false else
        let refPat = ReferencePatNavigator.GetByReferenceName(exprRefName)
        if isNull refPat then false else

        // I'm assuming that the parent of the fake refPat is a IParenPat for now.
        let parentPat = refPat.Parent :?> IParenPat
        if isNull parentPat then false else
        let parametersOwnerPat = ParametersOwnerPatNavigator.GetByParameter(parentPat)
        if isNull parametersOwnerPat then false else
        // We need to figure out if `A` actually is a UnionCase.
        let fcsUnionCase = parametersOwnerPat.ReferenceName.Reference.GetFcsSymbol().As<FSharpUnionCase>()
        if isNull fcsUnionCase then false else

        fieldNames <-
            fcsUnionCase.Fields
            |> Seq.choose (fun field -> if field.IsNameGenerated then None else Some field.Name)
            |> Seq.toArray
        
        // Only give auto completion to the fields only when all of them are named.
        fieldNames.Length = fcsUnionCase.Fields.Count
    
    override this.AddLookupItems(context, collector) =
        for fieldName in fieldNames do
            let info = NamedUnionCaseFieldItem(fieldName, fieldName, context.Ranges)
            let item =
                LookupItemFactory
                    .CreateLookupItem(info)
                    .WithPresentation(fun _ -> TextualPresentation(RichText(fieldName), info) :> _)
                    .WithBehavior(fun _ -> TextualBehavior(info) :> _)
                    .WithMatcher(fun _ -> TextualMatcher(info) :> _)
                    // .WithRelevance(CLRLookupItemRelevance.NamedArguments)
                    .WithHighSelectionPriority()

            collector.Add(item)

        true