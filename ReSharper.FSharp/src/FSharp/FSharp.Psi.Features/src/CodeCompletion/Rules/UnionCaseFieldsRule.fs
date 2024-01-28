namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.Util
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions.Deconstruction
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpPatternUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpPatternUtil.ParentTraversal
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.MatchTree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Resources.Shell

type UnionCaseFieldsInfo(fieldsString, deconstruction: IFSharpDeconstruction) =
    inherit TextualInfo(fieldsString, fieldsString)

    member this.Deconstruction = deconstruction

    override this.IsRiderAsync = false


type UnionCaseFieldsBehaviour(info) =
    inherit TextualBehavior<UnionCaseFieldsInfo>(info)

    override this.Accept(textControl, nameRange, _, _, solution, _) =
        let parametersOwnerPat = TextControlToPsi.GetElement<IParametersOwnerPat>(solution, nameRange.EndOffset)
        let psiServices = parametersOwnerPat.GetPsiServices()

        let action =
            use prohibitTypeCheckCookie = ProhibitTypeCheckCookie.Create()
            use writeCookie = WriteLockCookie.Create(parametersOwnerPat.IsPhysical())
            use cookie = CompilationContextCookie.GetOrCreate(parametersOwnerPat.GetPsiModule().GetContextFromModule())
            use transactionCookie =
                PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(psiServices, nameof UnionCaseFieldsBehaviour)

            let pat = parametersOwnerPat.ParametersEnumerable.FirstOrDefault()
            FSharpDeconstruction.deconstruct false parametersOwnerPat info.Deconstruction pat

        if isNotNull action then
            action.Invoke(textControl)


[<Language(typeof<FSharpLanguage>)>]
type UnionCaseFieldsPatternRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    let [<Literal>] Relevance =
        CLRLookupItemRelevance.ExpectedTypeMatch |||
        CLRLookupItemRelevance.ExpectedTypeMatchLambda

    let getUnionCaseReference (context: FSharpCodeCompletionContext) =
        let reference = context.ReparsedContext.Reference.As<FSharpSymbolReference>()
        if isNull reference then null else

        let referenceName = reference.GetElement().As<IExpressionReferenceName>()
        if isNull referenceName || isNotNull referenceName.Qualifier then null else

        let refPat = ReferencePatNavigator.GetByReferenceName(referenceName)
        let parenPat = ParenPatNavigator.GetByPattern(refPat)
        let parametersOwnerPat = ParametersOwnerPatNavigator.GetByParameter(parenPat)
        if isNull parametersOwnerPat then null else

        parametersOwnerPat.Reference

    override this.IsAvailable(context) =
        let reference = getUnionCaseReference context
        isNotNull reference &&

        let fcsUnionCase = reference.GetFcsSymbol().As<FSharpUnionCase>()
        isNotNull fcsUnionCase && fcsUnionCase.HasFields

    override this.AddLookupItems(context, collector) =
        let reference = getUnionCaseReference context
        let unionCase = reference.GetFcsSymbol() :?> FSharpUnionCase

        let referenceName = reference.GetElement().As<IExpressionReferenceName>()
        let parametersOwnerPat = ParametersOwnerPatNavigator.GetByReferenceName(referenceName)
        isNotNull parametersOwnerPat &&

        let matchClause = referenceName.GetContainingNode<IMatchClause>()
        let matchExpr = MatchExprNavigator.GetByClause(matchClause)
        isNotNull matchExpr &&

        let topLevelPattern, patternPath = ParentTraversal.makePatPath parametersOwnerPat
        topLevelPattern == matchClause.Pattern &&

        let matchType = MatchTree.getMatchExprMatchType matchExpr
        let matchValue = { Type = matchType; Path = [] }
        let pattern = MatchTree.ofMatchClause matchValue matchClause
        let matchNode = MatchNode.Create(matchValue, pattern)

        let rec navigatePath (path: PatternParentTraverseStep list) (matchNode: MatchNode) =
            match path with
            | [] -> Some matchNode
            | pathStep :: restPath ->

            match pathStep, matchNode.Pattern with
            | PatternParentTraverseStep.Tuple(i, _), (MatchTest.Tuple _, nodes) ->
                nodes
                |> List.tryItem i
                |> Option.bind (navigatePath restPath)

            | PatternParentTraverseStep.Or(i, _), (MatchTest.Or _, nodes) ->
                nodes
                |> List.tryItem i
                |> Option.bind (navigatePath restPath)

            | _ -> None

        let getExpectedType (node: MatchNode) =
            let rec loop (nodeType: MatchType) =
                match nodeType with
                | MatchType.Tuple(_, types) ->
                    types
                    |> Seq.tryHead
                    |> Option.bind loop

                | MatchType.Union expectedUnionInstance when
                        expectedUnionInstance.Entity = unionCase.ReturnType.TypeDefinition ->
                    Some expectedUnionInstance

                | _ -> None

            loop node.Value.Type

        let patternNode = navigatePath patternPath matchNode

        let unionInstance =
            patternNode
            |> Option.bind getExpectedType
            |> Option.defaultWith (fun _ -> FcsEntityInstance.create unionCase.ReturnType)

        let deconstruction =
            let fields = FSharpDeconstructionImpl.createUnionCaseFields parametersOwnerPat unionCase unionInstance
            DeconstructionFromUnionCaseFields(unionCase.Name, fields)

        let usedNames = FSharpNamingService.getPatternContextUsedNames parametersOwnerPat
        let names =
            FSharpDeconstructionImpl.getComponentNames parametersOwnerPat usedNames deconstruction.Components
            |> List.map (List.tryHead >> Option.defaultValue "_")

        let fieldsString = String.concat ", " names

        let info = UnionCaseFieldsInfo(fieldsString, deconstruction, Ranges = context.Ranges)
        let item =
            LookupItemFactory.CreateLookupItem(info)
                .WithPresentation(fun _ -> TextPresentation(info, null, false))
                .WithBehavior(fun _ -> UnionCaseFieldsBehaviour(info))
                .WithMatcher(fun _ -> TextualMatcher(info))
                .WithRelevance(Relevance)

        item.Placement.Location <- PlacementLocation.Top

        collector.Add(item)
        false
