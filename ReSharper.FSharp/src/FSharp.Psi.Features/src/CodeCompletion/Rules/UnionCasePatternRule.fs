namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open System.Collections.Generic
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
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Resources
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl

module UnionCasePatternInfo =
    let [<Literal>] Id = "Union case pattern"

type EnumCaseLikePatternInfo<'T when 'T :> FSharpSymbol>(text, symbol: 'T, fcsEntityInstance: FcsEntityInstance,
        context: FSharpCodeCompletionContext) =
    inherit TextualInfo(text, UnionCasePatternInfo.Id)

    member val Case = symbol
    member val EntityInstance = fcsEntityInstance

    interface IDescriptionProvidingLookupItem with
        member this.GetDescription() =
            match context.GetCheckResults(UnionCasePatternInfo.Id) with
            | None -> null
            | Some(checkResults) ->

            let _, range = context.ReparsedContext.TreeNode.TryGetFcsRange()
            let toolTipText = checkResults.GetDescription(symbol, fcsEntityInstance.Substitution, false, range)

            toolTipText
            |> FcsLookupCandidate.getOverloads
            |> List.tryHead
            |> Option.map (FcsLookupCandidate.getDescription context.XmlDocService context.PsiModule)
            |> Option.defaultValue null

    override this.IsRiderAsync = false


type EnumCaseLikePatternBehavior<'T when 'T :> FSharpSymbol>(info: EnumCaseLikePatternInfo<'T>) =
    inherit TextualBehavior<EnumCaseLikePatternInfo<'T>>(info)

    override this.Accept(textControl, nameRange, _, _, solution, _) =
        use writeCookie = WriteLockCookie.Create(true)
        use pinCheckResultsCookie =
            textControl.GetFSharpFile(solution).PinTypeCheckResults(true, UnionCasePatternInfo.Id)

        textControl.Document.ReplaceText(nameRange, "__")
        let nameRange = nameRange.StartOffset.ExtendRight("__".Length)

        let psiServices = solution.GetPsiServices()
        psiServices.Files.CommitAllDocuments()

        let pat =
            let pat = TextControlToPsi.GetElement<IReferencePat>(solution, nameRange.EndOffset)

            use writeCookie = WriteLockCookie.Create(pat.IsPhysical())
            use transactionCookie =
                PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(psiServices, UnionCasePatternInfo.Id)

            FSharpPatternUtil.bindFcsSymbol pat info.Case UnionCasePatternInfo.Id

        textControl.Caret.MoveTo(pat.GetNavigationRange().EndOffset, CaretVisualPlacement.DontScrollIfVisible)


type UnionCasePatternBehavior(info) =
    inherit EnumCaseLikePatternBehavior<FSharpUnionCase>(info)


[<Language(typeof<FSharpLanguage>)>]
type UnionCasePatternRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    let getExpectedUnionOrEnumType (referenceName: IExpressionReferenceName) =
        if referenceName.IsQualified then None else

        let referencePat = ReferencePatNavigator.GetByReferenceName(referenceName)
        let pat, path = FSharpPatternUtil.ParentTraversal.makePatPath referencePat
        let matchClause = MatchClauseNavigator.GetByPattern(pat)
        let matchExpr = MatchExprNavigator.GetByClause(matchClause)
        if isNull matchExpr then None else

        let expr = matchExpr.Expression
        if isNull expr then None else

        let rec tryToGetInnerExpr (IgnoreInnerParenExpr expr) path =
            match path with
            | [] -> Some(expr, [])
            | step :: rest ->

            match step, expr with
            | FSharpPatternUtil.ParentTraversal.PatternParentTraverseStep.Tuple(i, _), (:? ITupleExpr as tupleExpr) ->
                let tupleItems = tupleExpr.Expressions
                if tupleItems.Count <= i then None else
                tryToGetInnerExpr tupleItems[i] rest

            | FSharpPatternUtil.ParentTraversal.PatternParentTraverseStep.Or _, _ ->
                tryToGetInnerExpr expr rest

            | _ -> Some(expr, path)

        let rec tryGetInnerType (fcsType: FSharpType) path =
            match path with
            | [] -> Some(fcsType, [])
            | step :: rest ->

            match step with
            | FSharpPatternUtil.ParentTraversal.PatternParentTraverseStep.Tuple(i, _) ->
                if not fcsType.IsTupleType then None else

                let typeArguments = fcsType.GenericArguments
                if typeArguments.Count <= i then None else

                tryGetInnerType typeArguments[i] rest

            | _ -> Some(fcsType, path)

        let innerExpr = tryToGetInnerExpr expr path
        match innerExpr with
        | None -> None
        | Some(expr, path) ->

        let expr =
            match expr with
            | :? ITupleExpr as tupleExpr ->
                tupleExpr.ExpressionsEnumerable.FirstOrDefault()
                |> Option.ofObj
                |> Option.defaultValue expr
            | _ -> expr

        let fcsType = expr.TryGetFcsType()
        if isNull fcsType then None else

        let innerType = tryGetInnerType fcsType path
        match innerType with
        | None | Some(_, _ :: _) -> None
        | Some(fcsType, _) ->

        let fcsType =
            if not fcsType.IsTupleType then fcsType else

            let typeArguments = fcsType.GenericArguments
            if typeArguments.Count <> 0 then typeArguments[0] else fcsType

        if not fcsType.HasTypeDefinition then None else

        let displayContext = expr.TryGetFcsDisplayContext()
        if isNull displayContext then None else

        let fcsEntity = getAbbreviatedEntity fcsType.TypeDefinition
        if not (fcsEntity.IsFSharpUnion || fcsEntity.IsEnum) then None else

        Some (FcsEntityInstance.create fcsType, fcsType, displayContext)

    override this.IsAvailable(context) =
        context.IsBasicOrSmartCompletion &&

        let reference = context.ReparsedContext.Reference.As<FSharpSymbolReference>()
        isNotNull reference &&

        let referenceOwner = reference.GetElement()
        isNotNull (ReferencePatNavigator.GetByReferenceName(referenceOwner.As()))

    override this.TransformItems(context, collector) =
        let reference = context.ReparsedContext.Reference :?> FSharpSymbolReference
        let referenceName = reference.GetElement() :?> IExpressionReferenceName

        let createItem info behavior (fcsType: FSharpType) displayContext matchesType =
            let item =
                LookupItemFactory.CreateLookupItem(info)
                    .WithPresentation(fun _ ->
                        let typeText = fcsType.Format(displayContext)
                        TextPresentation(info, typeText, matchesType, PsiSymbolsThemedIcons.EnumMember.Id) :> _)
                    .WithBehavior(fun _ -> behavior)
                    .WithMatcher(fun _ -> TextualMatcher(info) :> _)
                    .WithRelevance(CLRLookupItemRelevance.Methods)

            if matchesType then
                markRelevance item (CLRLookupItemRelevance.ExpectedTypeMatch ||| CLRLookupItemRelevance.ExpectedTypeMatchStaticMember)
                item.Placement.Location <- PlacementLocation.Top

            item

        let createUnionCaseItem (fcsEntityInstance: FcsEntityInstance) (returnType: FSharpType) displayContext name
                symbol matchesType =
            let fcsType = returnType.Instantiate(fcsEntityInstance.Substitution)
            let info = EnumCaseLikePatternInfo(name, symbol, fcsEntityInstance, context, Ranges = context.Ranges)
            let behavior = UnionCasePatternBehavior(info)
            createItem info behavior fcsType displayContext matchesType

        let createEnumCaseItem (fcsEntityInstance: FcsEntityInstance) (returnType: FSharpType) displayContext name
                symbol matchesType =
            let fcsType = returnType.Instantiate(fcsEntityInstance.Substitution)
            let info = EnumCaseLikePatternInfo(name, symbol, fcsEntityInstance, context, Ranges = context.Ranges)
            let behavior = EnumCaseLikePatternBehavior(info)
            createItem info behavior fcsType displayContext matchesType

        let expectedType = getExpectedUnionOrEnumType referenceName

        let expectedTypeElement =
            match expectedType with
            | None -> null
            | Some(fcsEntityInstance, _, _) ->
                fcsEntityInstance.Entity.GetTypeElement(context.NodeInFile.GetPsiModule())

        let matchesType (returnType: FSharpType) =
            isNotNull expectedTypeElement &&

            match expectedType with
            | None -> false
            | Some(fcsEntityInstance, _, _) ->

            returnType.HasTypeDefinition &&
            getAbbreviatedEntity returnType.TypeDefinition = fcsEntityInstance.Entity

        // todo: move the filtering to the item provider instead of hacky modification of the collection?
        let unionCaseItems = List()
        collector.RemoveWhere(fun item ->
            let lookupItem = item.As<FcsLookupItem>()
            if isNull lookupItem then false else

            // Replace provided items for union cases with special ones.
            // Expected type items provided separately below: we don't know if they were provided by FCS.
            match lookupItem.FcsSymbol with
            | :? FSharpUnionCase as fcsUnionCase ->
                let returnType = fcsUnionCase.ReturnType
                if not (matchesType returnType) then
                    let text = fcsUnionCase.Name
                    let fcsEntityInstance = FcsEntityInstance.create returnType
                    let displayContext = lookupItem.FcsSymbolUse.DisplayContext
                    let item = createUnionCaseItem fcsEntityInstance returnType displayContext text fcsUnionCase false
                    unionCaseItems.Add(item)

                true

            | :? FSharpField as fcsField when FSharpSymbolUtil.isEnumMember fcsField ->
                let fieldType = fcsField.FieldType
                if not (matchesType fieldType) then
                    let text = fcsField.Name
                    let fcsEntityInstance = FcsEntityInstance.create fieldType
                    let displayContext = lookupItem.FcsSymbolUse.DisplayContext
                    let item = createEnumCaseItem fcsEntityInstance fieldType displayContext text fcsField false 
                    unionCaseItems.Add(item)

                true

            | _ -> false)

        unionCaseItems |> Seq.iter collector.Add

        match expectedType with
        | None -> ()
        | Some(fcsEntityInstance, fcsType, displayContext) ->
            if isNull expectedTypeElement then () else

            let fcsEntity = fcsEntityInstance.Entity
            let typeName = expectedTypeElement.GetSourceName()

            if fcsEntity.IsFSharpUnion then
                let requiresQualifiedName = expectedTypeElement.RequiresQualifiedAccess()
                for fcsUnionCase in fcsEntity.UnionCases do
                    let text = if requiresQualifiedName then $"{typeName}.{fcsUnionCase.Name}" else fcsUnionCase.Name
                    let item = createUnionCaseItem fcsEntityInstance fcsType displayContext text fcsUnionCase true
                    collector.Add(item)

            elif fcsEntity.IsEnum then
                for field in fcsEntity.FSharpFields do
                    if not field.IsLiteral then () else

                    let text = $"{typeName}.{field.Name}"
                    let item = createEnumCaseItem fcsEntityInstance fcsType displayContext text field true
                    collector.Add(item)
