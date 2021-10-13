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

        let pat =
            let pat = TextControlToPsi.GetElement<IWildPat>(solution, nameRange.EndOffset)

            use writeCookie = WriteLockCookie.Create(pat.IsPhysical())
            use transactionCookie =
                PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(psiServices, UnionCasePatternInfo.Id)

            FSharpPatternUtil.bindFcsSymbol pat info.UnionCase UnionCasePatternInfo.Id

        if not info.UnionCase.HasFields then () else

        let fields = FSharpDeconstruction.createUnionCaseFields pat info.UnionCase info.EntityInstance
        let deconstruction = DeconstructionFromUnionCaseFields(info.UnionCase.Name, pat :?> _, fields)

        let deconstructFields () =
            use writeCookie = WriteLockCookie.Create(pat.IsPhysical())
            use cookie =
                CompilationContextCookie.GetOrCreate(pat.GetPsiModule().GetContextFromModule())
            use transactionCookie =
                PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(psiServices, UnionCasePatternInfo.Id)

            let action = FSharpDeconstruction.deconstruct true deconstruction
            if isNotNull action then
                action.Invoke(textControl)

        if Shell.Instance.IsTestShell then
            deconstructFields () else

        solution.Locks.ExecuteOrQueueReadLockEx(solution.GetLifetime(), UnionCasePatternInfo.Id, fun _ ->
            let jetPopupMenus = solution.GetComponent<JetPopupMenus>()
            jetPopupMenus.ShowModal(JetPopupMenu.ShowWhen.NoItemsBannerIfNoItems, fun lifetime jetPopupMenu ->
                let textControlLockLifetimeDefinition = Lifetime.Define(lifetime)
                textControl.LockTextControl(textControlLockLifetimeDefinition.Lifetime, solution.Locks)

                // Adding null item keys is not allowed, wrap them into anon records as a workaround.
                // Inlining the list to AddRange changes anon record types due to allowed implicit casts on method args.
                let items = [{| Deconstruction = deconstruction |}; {| Deconstruction = null |}]
                jetPopupMenu.ItemKeys.AddRange(List.map box items)

                let (|Deconstruction|) (obj: obj) =
                    let deconstructionItem = obj :?> {| Deconstruction: DeconstructionFromUnionCaseFields |}
                    deconstructionItem.Deconstruction 

                jetPopupMenu.DescribeItem.Advise(lifetime, fun args ->
                    let (Deconstruction deconstruction) = args.Key

                    let text = if isNotNull deconstruction then deconstruction.Text else "Ignore fields"
                    args.Descriptor.Text <- RichText(text)
                    args.Descriptor.Style <- MenuItemStyle.Enabled)

                jetPopupMenu.ItemClicked.Advise(lifetime, fun (Deconstruction deconstruction) ->
                    use readLockCookie = ReadLockCookie.Create()

                    textControlLockLifetimeDefinition.Terminate()
                    psiServices.Files.AssertAllDocumentAreCommitted()

                    if isNotNull deconstruction then
                        deconstructFields ())

                jetPopupMenu.PopupWindowContextSource <- textControl.PopupWindowContextFactory.ForCaret()))

[<Language(typeof<FSharpLanguage>)>]
type UnionCasePatternRule() =
    inherit ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>()

    // todo: always allow going from left, only allow going from right is left part type matches
    let rec skipParentOrPats (pat: IFSharpPattern) =
        match OrPatNavigator.GetByPattern(pat) with
        | null -> pat
        | orPat -> skipParentOrPats orPat

    let getExpectedUnionType referenceName =
        let referencePat = ReferencePatNavigator.GetByReferenceName(referenceName)
        let pat = skipParentOrPats referencePat
        let matchClause = MatchClauseNavigator.GetByPattern(pat)
        let matchExpr = MatchExprNavigator.GetByClause(matchClause)
        if isNull matchExpr then None else

        let expr = matchExpr.Expression
        if isNull expr then None else

        let fcsType = expr.TryGetFcsType()
        if isNull fcsType || not fcsType.HasTypeDefinition then None else

        let displayContext = expr.TryGetFcsDisplayContext()
        if isNull displayContext then None else

        let fcsEntity = getAbbreviatedEntity fcsType.TypeDefinition
        if not fcsEntity.IsFSharpUnion then None else

        Some (FcsEntityInstance.create fcsType, fcsType, displayContext)

    override this.IsAvailable(context) =
        context.IsBasicOrSmartCompletion && not context.IsQualified &&

        let reference = context.ReparsedContext.Reference.As<FSharpSymbolReference>()
        isNotNull reference &&

        let referenceOwner = reference.GetElement()
        isNotNull (ReferencePatNavigator.GetByReferenceName(referenceOwner.As()))

    override this.TransformItems(context, collector) =
        let reference = context.ReparsedContext.Reference :?> FSharpSymbolReference
        let referenceName = reference.GetElement() :?> IExpressionReferenceName

        let createItem fcsEntityInstance (fcsType: FSharpType) displayContext text matchesType (fcsUnionCase: FSharpUnionCase) =
            let info = UnionCasePatternInfo(text, fcsUnionCase, fcsEntityInstance, Ranges = context.Ranges)
            let item = 
                LookupItemFactory.CreateLookupItem(info)
                    .WithPresentation(fun _ ->
                        let typeText = fcsType.Format(displayContext)
                        TextPresentation(info, typeText, matchesType, PsiSymbolsThemedIcons.EnumMember.Id) :> _)
                    .WithBehavior(fun _ -> UnionCasePatternBehavior(info) :> _)
                    .WithMatcher(fun _ -> TextualMatcher(info) :> _)
                    .WithRelevance(CLRLookupItemRelevance.Methods)

            if matchesType then
                markRelevance item CLRLookupItemRelevance.ExpectedTypeMatch

            item

        let expectedUnionType = getExpectedUnionType referenceName

        // todo: move the filtering to the item provider instead of hacky modification of the collection?
        let unionCaseItems = List()
        collector.RemoveWhere(fun item ->
            match item with
            | :? FcsLookupItem as lookupItem ->
                // Replace provided items for union cases with special ones.
                match lookupItem.FcsSymbol with
                | :? FSharpUnionCase as fcsUnionCase ->
                    let matchesType =
                        match expectedUnionType with
                        | None -> false
                        | Some(fcsEntityInstance, _, _) ->
                            let returnType = fcsUnionCase.ReturnType
                            returnType.HasTypeDefinition && getAbbreviatedEntity returnType.TypeDefinition = fcsEntityInstance.Entity

                    // Expected type cases provided separately below: we don't know if they were provided by FCS.
                    if not matchesType then
                        let fcsType = fcsUnionCase.ReturnType.Instantiate(lookupItem.FcsSymbolUse.GenericArguments)
                        let fcsEntityInstance = FcsEntityInstance.create fcsType
                        let item = createItem fcsEntityInstance fcsType lookupItem.FcsSymbolUse.DisplayContext fcsUnionCase.Name false fcsUnionCase
                        unionCaseItems.Add(item)

                    true

                | _ -> false
            | _ -> false)

        unionCaseItems |> Seq.iter collector.Add

        match expectedUnionType with
        | None -> ()
        | Some(fcsEntityInstance, fcsType, displayContext) ->
            let typeElement = fcsEntityInstance.Entity.GetTypeElement(context.NodeInFile.GetPsiModule())
            let requiresQualifiedName = isNotNull typeElement && typeElement.RequiresQualifiedAccess()
            let typeName = typeElement.GetSourceName()

            for fcsUnionCase in fcsEntityInstance.Entity.UnionCases do
                let text = if requiresQualifiedName then $"{typeName}.{fcsUnionCase.Name}" else fcsUnionCase.Name
                let item = createItem fcsEntityInstance fcsType displayContext text true fcsUnionCase
                collector.Add(item)
