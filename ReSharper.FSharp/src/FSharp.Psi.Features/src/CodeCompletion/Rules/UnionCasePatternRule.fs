namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open FSharp.Compiler.Symbols
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Behaviors
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations
open JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems
open JetBrains.ReSharper.Feature.Services.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.FSharpCompletionUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions.Deconstruction
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Resources
open JetBrains.ReSharper.Psi.Transactions

module UnionCasePatternInfo =
    let [<Literal>] Id = "Union case pattern"

type UnionCasePatternInfo(text, fcsUnionCase: FSharpUnionCase, fcsEntityInstance: FcsEntityInstance) =
    inherit TextualInfo(text, UnionCasePatternInfo.Id)

    member val UnionCase = fcsUnionCase
    member val EntityInstance = fcsEntityInstance

    override this.IsRiderAsync = false

type UnionCasePatternBehavior(info: UnionCasePatternInfo) =
    inherit TextualBehavior<UnionCasePatternInfo>(info)

    override this.Accept(textControl, nameRange, _, _, solution, _) =
        textControl.Document.ReplaceText(nameRange, "_")
        let nameRange = nameRange.StartOffset.ExtendRight("_".Length)

        let psiServices = solution.GetPsiServices()
        psiServices.Files.CommitAllDocuments()

        let pat = TextControlToPsi.GetElement<IWildPat>(solution, nameRange.EndOffset)
        let deconstruction = DeconstructionFromUnionCase.Create(pat, info.UnionCase, info.EntityInstance)

        use transactionCookie =
            PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(psiServices, UnionCasePatternInfo.Id)

        let action = FSharpDeconstruction.deconstruct true deconstruction
        if isNotNull action then
            action.Invoke(textControl)

[<Language(typeof<FSharpLanguage>)>]
type UnionCasePatternRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    override this.IsAvailable(context) =
        context.IsBasicOrSmartCompletion && not context.IsQualified

    override this.TransformItems(context, collector) =
        let reference = context.ReparsedContext.Reference.As<FSharpSymbolReference>()
        if isNull reference then () else

        let referenceName = reference.GetElement().As<IExpressionReferenceName>()
        if isNull referenceName then () else

        let referencePat = ReferencePatNavigator.GetByReferenceName(referenceName)
        let matchClause = MatchClauseNavigator.GetByPattern(referencePat)
        let matchExpr = MatchExprNavigator.GetByClause(matchClause)
        if isNull matchExpr then () else

        let expr = matchExpr.Expression
        if isNull expr then () else

        let fcsType = expr.TryGetFcsType()
        if isNull fcsType || not fcsType.HasTypeDefinition then () else

        let displayContext = expr.TryGetFcsDisplayContext()
        if isNull displayContext then () else

        let fcsEntity = getAbbreviatedEntity fcsType.TypeDefinition
        if not fcsEntity.IsFSharpUnion then () else

        collector.RemoveWhere(fun lookupItem ->
            let lookupItem = lookupItem.As<FcsLookupItem>()
            if isNull lookupItem then false else

            match lookupItem.FcsSymbol with
            | :? FSharpUnionCase as fcsUnionCase ->
                let returnType = fcsUnionCase.ReturnType
                returnType.HasTypeDefinition && getAbbreviatedEntity returnType.TypeDefinition = fcsEntity
            | _ -> false)

        let typeElement = fcsEntity.GetTypeElement(expr.GetPsiModule())
        let requiresQualifiedName = isNotNull typeElement && typeElement.RequiresQualifiedAccess()
        let typeName = typeElement.GetSourceName()
        let displayContext = displayContext.WithShortTypeNames(true)

        for unionCase in fcsEntity.UnionCases do
            let text = if requiresQualifiedName then $"{typeName}.{unionCase.Name}" else unionCase.Name
            let info = UnionCasePatternInfo(text, unionCase, FcsEntityInstance.create fcsType, Ranges = context.Ranges)
            let item =
                LookupItemFactory.CreateLookupItem(info)
                    .WithPresentation(fun _ ->
                        let typeText = fcsType.Format(displayContext)
                        TextPresentation(info, typeText, true, PsiSymbolsThemedIcons.EnumMember.Id) :> _)
                    .WithBehavior(fun _ -> UnionCasePatternBehavior(info) :> _)
                    .WithMatcher(fun _ -> TextualMatcher(info) :> _)
                    .WithRelevance(CLRLookupItemRelevance.ExpectedTypeMatch)
            collector.Add(item)
