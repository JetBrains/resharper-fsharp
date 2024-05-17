namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.Util

[<Language(typeof<FSharpLanguage>)>]
type NameSuggestionRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    let getAsPat (context: FSharpCodeCompletionContext) =
        let node = context.ReparsedContext.TreeNode
        if isNull node then null else

        let referenceName = node.Parent.As<IExpressionReferenceName>()
        if isNull referenceName || referenceName.IsQualified then null else

        let refPat = ReferencePatNavigator.GetByReferenceName(referenceName)
        AsPatNavigator.GetByRightPattern(refPat)

    let getIsInstType (isInstPat: IIsInstPat) =
        let typeUsage = isInstPat.TypeUsage.As<INamedTypeUsage>()
        if isNull typeUsage then null else

        let reference = typeUsage.ReferenceName.Reference
        reference.ResolveType()

    override this.IsAvailable(context) =
        context.IsBasicOrSmartCompletion &&
        isNotNull (getAsPat context)

    override this.AddLookupItems(context, collector) =
        let asPat = getAsPat context
        let isInstPat = asPat.LeftPattern.As<IIsInstPat>()
        if isNull isInstPat then false else

        let asType = getIsInstType isInstPat
        if isNull asType then false else

        let names =
            FSharpNamingService.createEmptyNamesCollection asPat
            |> FSharpNamingService.addNamesForType asType
            |> FSharpNamingService.prepareNamesCollection EmptySet.Instance asPat

        for name in names do
            let info = TextualInfo(name, name, Ranges = context.Ranges)
            let item =
                LookupItemFactory.CreateLookupItem(info)
                    .WithPresentation(fun _ -> TextualPresentation(info) :> _)
                    .WithBehavior(fun _ -> TextualBehavior(info))
                    .WithMatcher(fun _ -> TextualMatcher(info))
                    .WithRelevance(CLRLookupItemRelevance.LocalVariablesAndParameters)

            collector.Add(item)

        FSharpCodeCompletionContext.disableFullEvaluation context.BasicContext
        false

    override this.TransformItems(context, collector) =
        let asPat = getAsPat context
        let isInstPat = asPat.LeftPattern.As<IIsInstPat>()
        if isNull isInstPat then () else

        let isInstType = getIsInstType isInstPat
        let isUnionType =
            isNotNull isInstType &&

            let typeElement = isInstType.GetTypeElement()
            isNotNull typeElement && typeElement.IsUnion()

        collector.RemoveWhere(fun item ->
            match item with
            | :? FcsLookupItem -> true
            | :? IAspectLookupItem<IEnumCaseLikePatternInfo> as item ->
                not isUnionType && item.Placement.Location <> PlacementLocation.Top
            | _ -> false
        )
