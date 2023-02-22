namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.PostfixTemplates

open System.Collections.Generic
open System.Linq
open FSharp.Compiler.Symbols
open JetBrains.Application.Progress
open JetBrains.Application.Threading
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Bulbs
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Feature.Services.Navigation.CustomHighlighting
open JetBrains.ReSharper.Feature.Services.PostfixTemplates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates.Contexts
open JetBrains.ReSharper.Feature.Services.Refactorings.WorkflowOccurrences
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Intentions.Deconstruction
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Psi.Search
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl
open JetBrains.UI.RichText
open JetBrains.Util

module ForPostfixTemplate =
    let tryGetEnumerableTypeArg (contextExpr: IFSharpExpression) (fcsType: FSharpType) : FSharpType option =
        let seenTypes = HashSet()

        let rec loop (fcsType: FSharpType) =
            let fcsType = FSharpSymbolUtil.getAbbreviatedType fcsType

            let exprType = fcsType.MapType(contextExpr)
            if exprType.IsGenericIEnumerable() then
                Some fcsType.GenericArguments[0] else

            if not fcsType.HasTypeDefinition then None else

            let entity = fcsType.TypeDefinition
            if entity.IsArrayType then Some fcsType.GenericArguments[0] else

            if not (seenTypes.Add(entity.BasicQualifiedName)) then None else

            fcsType.BaseType
            |> Option.bind loop
            |> Option.orElseWith (fun _ ->
                fcsType.AllInterfaces
                |> Seq.tryPick loop
            )

        loop fcsType

    let getEnumeratedType (contextExpr: IFSharpExpression) (fcsType: FSharpType) : FSharpType option =
        if isNull fcsType then None else

        if fcsType.IsGenericParameter then
            fcsType.AllInterfaces |> Seq.tryPick (tryGetEnumerableTypeArg contextExpr)
        else
            tryGetEnumerableTypeArg contextExpr fcsType

    let getExpressionType (expr: IFSharpExpression) =
        let refExpr = expr.As<IReferenceExpr>()
        if isNull refExpr then Unchecked.defaultof<_> else

        let expr = refExpr.Qualifier
        if isNull expr then Unchecked.defaultof<_> else

        expr.TryGetFcsType(), expr.TryGetFcsDisplayContext()


[<PostfixTemplate("for", "Iterates over enumerable collection", "for _ in expr do ()")>]
type ForPostfixTemplate() =
    inherit FSharpPostfixTemplateBase()

    let isApplicableType (contextExpr: IFSharpExpression) (fcsType: FSharpType) =
        if isNull fcsType then false else

        let enumeratedType = ForPostfixTemplate.getEnumeratedType contextExpr fcsType
        isNotNull enumeratedType ||

        fcsType.IsGenericParameter && Seq.isEmpty fcsType.AllInterfaces

    let isApplicableDeclaredElement (refExpr: IReferenceExpr) =
        if isNull refExpr then false else

        let refPat = refExpr.Reference.Resolve().DeclaredElement.As<ILocalReferencePat>()
        if isNull refPat then false else

        let rec tryGetTopLevelPatternFromUntypedPattern (pat: IFSharpPattern) =
            let pat = pat.IgnoreParentParens()
            match TuplePatNavigator.GetByPattern(pat) with
            | null -> pat
            | pat -> tryGetTopLevelPatternFromUntypedPattern pat

        let pat = tryGetTopLevelPatternFromUntypedPattern refPat
        if isNull (BindingNavigator.GetByParameterPattern(pat)) then false else

        let references = List()
        let searchPattern = SearchPattern.FIND_USAGES ||| SearchPattern.FIND_RELATED_ELEMENTS
        let searchDomain = refPat.DeclaredElement.GetSearchDomain()

        refPat.GetPsiServices().AsyncFinder.Find([| refPat.DeclaredElement |], searchDomain, references.ConsumeReferences(),
                searchPattern, NullProgressIndicator.Create())

        match Seq.tryHead references with
        | None -> false
        | Some reference -> reference.GetTreeNode().GetTreeStartOffset() = refExpr.GetTreeStartOffset()

    let isApplicable (expr: IFSharpExpression) =
        if not (FSharpPostfixTemplates.canBecomeStatement expr) then false else

        let fcsType, displayContext = ForPostfixTemplate.getExpressionType expr
        isNotNull displayContext && fcsType |> isApplicableType expr ||

        let refExpr = expr.As<IReferenceExpr>()
        isApplicableDeclaredElement refExpr

    override x.CreateBehavior(info) = ForPostfixTemplateBehavior(info :?> ForPostfixTemplateInfo) :> _

    override x.CreateInfo(context) =
        let expr = context.Expression.As<IReferenceExpr>()

        let fcsType, displayContext = ForPostfixTemplate.getExpressionType expr
        let enumeratedType = ForPostfixTemplate.getEnumeratedType expr fcsType

        ForPostfixTemplateInfo(context, enumeratedType, displayContext) :> _

    override this.IsApplicable(node) =
        let expr = node.As<IFSharpExpression>()
        isApplicable expr


and ForPostfixTemplateInfo(expressionContext: PostfixExpressionContext, enumeratedType: FSharpType option,
        displayContext: FSharpDisplayContext) =
    inherit PostfixTemplateInfo("for", expressionContext)

    member val DisplayContext = displayContext
    member val EnumeratedType = enumeratedType


and ForPostfixTemplateBehavior(info: ForPostfixTemplateInfo) =
    inherit FSharpPostfixTemplateBehaviorBase(info)

    override x.ExpandPostfix(context) =
        let psiModule = context.PostfixContext.PsiModule
        let psiServices = psiModule.GetPsiServices()

        psiServices.Transactions.Execute(x.ExpandCommandName, fun _ ->
            let node = context.Expression :?> IFSharpTreeNode
            use writeCookie = WriteLockCookie.Create(node.IsPhysical())
            use disableFormatter = new DisableCodeFormatter()

            let expr = x.GetExpression(context)
            FSharpPostfixTemplates.convertToBlockLikeExpr expr context
            let forEachExpr = expr.CreateElementFactory().CreateForEachExpr(expr)
            ModificationUtil.ReplaceChild(expr, forEachExpr) :> ITreeNode)

    override x.AfterComplete(textControl, node, _) =
        let id = "ForPostfixTemplateBehavior.AfterComplete"

        let solution = node.GetSolution()
        let psiServices = node.GetPsiServices()

        let forEachExpr = node :?> IForEachExpr

        let names =
            let namesCollection = FSharpNamingService.createEmptyNamesCollection forEachExpr
            match info.EnumeratedType with
            | None -> ()
            | Some fcsType ->
                let exprType = fcsType.MapType(forEachExpr)
                if isNotNull exprType then
                    FSharpNamingService.addNamesForType exprType namesCollection |> ignore
            let namingService = LanguageManager.Instance.GetService<FSharpNamingService>(forEachExpr.Language)
            namingService.AddExtraNames(namesCollection, forEachExpr.Pattern)
            FSharpNamingService.prepareNamesCollection EmptySet.Instance forEachExpr namesCollection

        let names = List.ofSeq names @ ["_"]

        psiServices.Transactions.Execute(id, fun _ ->
            let name = if names.Length > 1 then names[0] else "x"
            let pat = forEachExpr.CreateElementFactory().CreatePattern(name, false)
            forEachExpr.SetPattern(pat) |> ignore
        ) |> ignore

        let dummy () =
            use transactionCookie = PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(psiServices, id)
            use writeCookie = WriteLockCookie.Create(node.IsPhysical())
            use disableFormatter = new DisableCodeFormatter()

            let hotspotsRegistry = HotspotsRegistry(psiServices)
            hotspotsRegistry.Register(forEachExpr.Pattern, NameSuggestionsExpression(names))

            let endOffset = forEachExpr.DoExpression.GetDocumentStartOffset()
            ModificationUtil.DeleteChild(forEachExpr.DoExpression)

            BulbActionUtils.ExecuteHotspotSession(hotspotsRegistry, endOffset).Invoke(textControl)

        match info.EnumeratedType with
        | None -> dummy ()
        | Some fcsType ->

        match FSharpDeconstruction.tryGetDeconstruction forEachExpr fcsType with
        | None -> dummy ()
        | Some(deconstruction) ->

        textControl.Caret.MoveTo(forEachExpr.Pattern.GetDocumentEndOffset(), CaretVisualPlacement.DontScrollIfVisible)

        let lifetime = textControl.Lifetime
        solution.Locks.ExecuteOrQueueReadLockEx(lifetime, id, fun _ ->
            let popupMenu = node.GetSolution().GetComponent<WorkflowPopupMenu>()

            let occurrences =
                let valueText = FSharpIntroduceVariable.getOccurrenceText info.DisplayContext fcsType "' value"
                [| WorkflowPopupMenuOccurrence(RichText(deconstruction.Text), null, deconstruction)
                   WorkflowPopupMenuOccurrence(valueText, null, null, null) |]

            let selectedOccurrence =
                popupMenu.ShowPopup(lifetime, occurrences, CustomHighlightingKind.Other, textControl, null)
            if isNull selectedOccurrence then () else

            let deconstruction = selectedOccurrence.Entities.FirstOrDefault()
            if isNull deconstruction then dummy () else

            use writeCookie = WriteLockCookie.Create(node.IsPhysical())
            use disableFormatter = new DisableCodeFormatter()
            use transactionCookie = PsiTransactionCookie.CreateAutoCommitCookieWithCachesUpdate(psiServices, id)
            use cookie = CompilationContextCookie.GetOrCreate(node.GetPsiModule().GetContextFromModule())

            match FSharpDeconstructionImpl.deconstructImpl false deconstruction forEachExpr.Pattern with
            | Some(hotspotsRegistry, _) ->
                let endOffset = forEachExpr.DoExpression.GetDocumentStartOffset()
                ModificationUtil.DeleteChild(forEachExpr.DoExpression)

                BulbActionUtils.ExecuteHotspotSession(hotspotsRegistry, endOffset).Invoke(textControl)
            | _ -> failwith id
        )
