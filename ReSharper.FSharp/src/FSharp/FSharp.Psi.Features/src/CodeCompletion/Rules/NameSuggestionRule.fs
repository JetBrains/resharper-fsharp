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
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Services.Util.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

// todo: unify instance of and type rules
// todo: provide suggestions based on field names
// todo: filter fcs suggestions
// todo: support try-with expressions
// todo: support function expressions

type FSharpNameSuggestionInfo(name) =
    inherit TextualInfo(name, name)

module Module =
    let getSuggestionItems (context: ITreeNode) ranges (t: IDeclaredType) =
        let names =
            FSharpNamingService.createEmptyNamesCollection context
            |> FSharpNamingService.addNamesForType t
            |> FSharpNamingService.prepareNamesCollection EmptySet.Instance context

        names
        |> Seq.map (fun name ->
            let info = FSharpNameSuggestionInfo(name, Ranges = ranges)
            LookupItemFactory.CreateLookupItem(info)
                .WithPresentation(fun _ -> TextualPresentation(info) :> _)
                .WithBehavior(fun _ -> TextualBehavior(info))
                .WithMatcher(fun _ -> TextualMatcher(info))
                .WithRelevance(CLRLookupItemRelevance.LocalVariablesAndParameters)
        )


[<Language(typeof<FSharpLanguage>)>]
type NameSuggestionFromAsInstanceOfRule() =
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

        let items = Module.getSuggestionItems asPat context.Ranges asType
        for item in items do
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

[<Language(typeof<FSharpLanguage>)>]
type NameSuggestionFromTypeRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    let getReferencePat (context: FSharpCodeCompletionContext) =
        let reference = context.ReparsedContext.Reference.As<FSharpSymbolReference>()
        if isNull reference || reference.IsQualified then null else

        ReferencePatNavigator.GetByReferenceName(reference.GetElement().As())

    override this.IsAvailable(context) =
        context.IsBasicOrSmartCompletion &&

        let refPat = getReferencePat context
        isNotNull refPat

    override this.AddLookupItems(context, collector) =
        let refPat = getReferencePat context
        let pat, path = FSharpPatternUtil.ParentTraversal.makePatPath refPat
        let matchClause = MatchClauseNavigator.GetByPattern(pat)
        let matchExpr = MatchExprNavigator.GetByClause(matchClause)
        if isNull matchExpr then false else

        let matchValue = MatchTree.getMatchValue matchExpr
        let clausePattern = MatchTree.ofMatchClause matchValue matchClause
        let matchNode = MatchTree.MatchNode.Create(matchValue, clausePattern)
        match MatchTree.tryNavigatePatternPath path matchNode with
        | None -> false
        | Some node ->

        let rec getFcsType valueType : FSharpType list =
            match valueType with
            | MatchTree.MatchType.List fcsType ->
                [fcsType.GenericArguments[0]; fcsType]

            | MatchTree.MatchType.Tuple(_, types) ->
                Array.tryHead types
                |> Option.map getFcsType
                |> Option.defaultValue []

            | valueType -> Option.toList valueType.FcsType

        let tryReplaceWithParameterBaseTypes (fcsType: FSharpType) =
            if isUnit fcsType then [] else
            if not fcsType.IsGenericParameter then [fcsType] else

            match fcsType.BaseType with
            | Some baseType -> [baseType]
            | _ -> fcsType.AllInterfaces |> List.ofSeq

        let nodeTypes = getFcsType node.Value.Type
        let nodeTypes = nodeTypes |> List.collect tryReplaceWithParameterBaseTypes

        for fcsType in nodeTypes do
            let patType = fcsType.MapType(refPat).As<IDeclaredType>()
            if isNull patType || patType.IsUnknown then () else

            let items = Module.getSuggestionItems refPat context.Ranges patType
            for item in items do
                collector.Add(item)

        false
