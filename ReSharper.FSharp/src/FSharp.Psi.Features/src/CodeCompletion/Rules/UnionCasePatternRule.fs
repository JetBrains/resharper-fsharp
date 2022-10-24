namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion.Rules

open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.Application.UI.Controls.JetPopupMenu
open JetBrains.DocumentModel
open JetBrains.Application.Threading
open JetBrains.Lifetimes
open JetBrains.ProjectModel
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
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Resources
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl
open JetBrains.UI.RichText

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
            |> Option.map (FcsLookupCandidate.getDescription context.XmlDocService)
            |> Option.defaultValue null

    override this.IsRiderAsync = false


type UnionCasePatternInfo(text, fcsUnionCase, fcsEntityInstance, context) =
    inherit EnumCaseLikePatternInfo<FSharpUnionCase>(text, fcsUnionCase, fcsEntityInstance, context)


type EnumCaseLikePatternBehavior<'T when 'T :> FSharpSymbol>(info: EnumCaseLikePatternInfo<'T>) =
    inherit TextualBehavior<EnumCaseLikePatternInfo<'T>>(info)

    abstract Deconstruct: IFSharpPattern * ITextControl * ISolution * IPsiServices -> unit
    default this.Deconstruct(_, _, _, _) = ()

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
        this.Deconstruct(pat, textControl, solution, psiServices)


type UnionCasePatternBehavior(info) =
    inherit EnumCaseLikePatternBehavior<FSharpUnionCase>(info)

    override this.Deconstruct(pat, textControl, solution, psiServices) =
        if not info.Case.HasFields || not (pat :? IReferencePat) then () else

        let fields = FSharpDeconstructionImpl.createUnionCaseFields pat info.Case info.EntityInstance
        let fieldsDeconstruction: IFSharpDeconstruction =
            DeconstructionFromUnionCaseFields(info.Case.Name, fields) :> _

        let singleField = Seq.tryExactlyOne info.Case.Fields

        let singleFieldDeconstruction =
            singleField
            |> Option.map (fun fcsField -> fcsField.FieldType.Instantiate(info.EntityInstance.Substitution))
            |> Option.bind (FSharpDeconstruction.tryGetDeconstruction pat)
            |> Option.map (fun deconstruction -> singleField.Value.DisplayNameCore, deconstruction)

        let fieldsDeconstructionText =
            singleFieldDeconstruction
            |> Option.map (fun (name, _) -> $"Use named pattern for '{name}'")
            |> Option.defaultValue fieldsDeconstruction.Text

        let deconstruct (deconstruction: IFSharpDeconstruction) (parametersOwnerPat: IParametersOwnerPat) =
            use prohibitTypeCheckCookie = ProhibitTypeCheckCookie.Create()
            use writeCookie = WriteLockCookie.Create(parametersOwnerPat.IsPhysical())
            use cookie =
                CompilationContextCookie.GetOrCreate(parametersOwnerPat.GetPsiModule().GetContextFromModule())
            use transactionCookie =
                PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(psiServices, UnionCasePatternInfo.Id)

            let pat = parametersOwnerPat.ParametersEnumerable.FirstOrDefault()
            let action = FSharpDeconstruction.deconstruct false parametersOwnerPat deconstruction pat
            if isNotNull action then
                action.Invoke(textControl)

        if Shell.Instance.IsTestShell then
            let pat = FSharpPatternUtil.toParameterOwnerPat pat UnionCasePatternInfo.Id
            deconstruct fieldsDeconstruction pat else

        let lifetime = solution.GetSolutionLifetimes().UntilSolutionCloseLifetime
        solution.Locks.ExecuteOrQueueReadLockEx(lifetime, UnionCasePatternInfo.Id, fun _ ->
            let jetPopupMenus = solution.GetComponent<JetPopupMenus>()
            jetPopupMenus.ShowModal(JetPopupMenu.ShowWhen.NoItemsBannerIfNoItems, fun lifetime jetPopupMenu ->
                let textControlLockLifetimeDefinition = Lifetime.Define(lifetime)
                textControl.LockTextControl(textControlLockLifetimeDefinition.Lifetime, solution.Locks)

                // Adding null item keys is not allowed, wrap them into anon records as a workaround.
                // Inlining the list to AddRange changes anon record types due to allowed implicit casts on method args.
                let items =
                    [ {| Deconstruction = fieldsDeconstruction; Text = fieldsDeconstructionText |}

                      match singleFieldDeconstruction with
                      | None _ -> ()
                      | Some (_, deconstruction) ->
                          {| Deconstruction = deconstruction; Text = deconstruction.Text |}

                      {| Deconstruction = null; Text = null |} ]

                jetPopupMenu.ItemKeys.AddRange(List.map box items)

                let (|Deconstruction|) (obj: obj) =
                    let deconstructionItem = obj :?> {| Deconstruction: IFSharpDeconstruction; Text: string |}
                    deconstructionItem.Deconstruction, deconstructionItem.Text 

                jetPopupMenu.DescribeItem.Advise(lifetime, fun args ->
                    let (Deconstruction (deconstruction, text)) = args.Key

                    let text =
                        match deconstruction, singleField with
                        | null, None -> "Ignore fields"
                        | null, Some _ -> "Ignore field"
                        | _ -> text

                    args.Descriptor.Text <- RichText(text)
                    args.Descriptor.Style <- MenuItemStyle.Enabled)

                jetPopupMenu.ItemClicked.Advise(lifetime, fun (Deconstruction (deconstruction, _)) ->
                    use readLockCookie = ReadLockCookie.Create()

                    textControlLockLifetimeDefinition.Terminate()
                    psiServices.Files.AssertAllDocumentAreCommitted()

                    let pat = FSharpPatternUtil.toParameterOwnerPat pat UnionCasePatternInfo.Id
                    if isNotNull deconstruction then
                        deconstruct deconstruction pat
                    else
                        let endOffset = pat.GetNavigationRange().EndOffset
                        textControl.Caret.MoveTo(endOffset, CaretVisualPlacement.DontScrollIfVisible))

                jetPopupMenu.PopupWindowContextSource <- textControl.PopupWindowContextFactory.ForCaret()))


[<Language(typeof<FSharpLanguage>)>]
type UnionCasePatternRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    // todo: always allow going from left, only allow going from right is left part type matches
    let rec skipParentOrPats (pat: IFSharpPattern) =
        match OrPatNavigator.GetByPattern(pat) with
        | null -> pat
        | orPat -> skipParentOrPats orPat

    let getExpectedUnionOrEnumType (referenceName: IExpressionReferenceName) =
        if referenceName.IsQualified then None else

        let referencePat = ReferencePatNavigator.GetByReferenceName(referenceName)
        let pat, path = FSharpPatternUtil.ParentTraversal.makeTuplePatPath referencePat
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
                markRelevance item CLRLookupItemRelevance.ExpectedTypeMatch
                item.Placement.Location <- PlacementLocation.Top

            item

        let createUnionCaseItem (fcsEntityInstance: FcsEntityInstance) (returnType: FSharpType) displayContext name
                symbol matchesType =
            let fcsType = returnType.Instantiate(fcsEntityInstance.Substitution)
            let info = UnionCasePatternInfo(name, symbol, fcsEntityInstance, context, Ranges = context.Ranges)
            let behavior = UnionCasePatternBehavior(info)
            createItem info behavior fcsType displayContext matchesType

        let createEnumCaseItem (fcsEntityInstance: FcsEntityInstance) (returnType: FSharpType) displayContext name
                symbol matchesType =
            let fcsType = returnType.Instantiate(fcsEntityInstance.Substitution)
            let info = EnumCaseLikePatternInfo(name, symbol, fcsEntityInstance, context, Ranges = context.Ranges)
            let behavior = EnumCaseLikePatternBehavior(info)
            createItem info behavior fcsType displayContext matchesType

        let expectedType = getExpectedUnionOrEnumType referenceName

        let matchesType (returnType: FSharpType) =
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
            let fcsEntity = fcsEntityInstance.Entity
            let typeElement = fcsEntity.GetTypeElement(context.NodeInFile.GetPsiModule())
            let requiresQualifiedName = isNotNull typeElement && typeElement.RequiresQualifiedAccess()
            let typeName = typeElement.GetSourceName()

            if fcsEntity.IsFSharpUnion then
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
