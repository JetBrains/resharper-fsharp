namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.PostfixTemplates

open System.Collections.Generic
open System.Linq
open FSharp.Compiler.Symbols
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
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Modules
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

    let getExpressionType (wholeExpr: IFSharpExpression) (initialExpr: IFSharpExpression) =
        let getRefExprQualifierType (refExpr: IReferenceExpr) =
            let expr = refExpr.Qualifier
            if isNull expr then Unchecked.defaultof<_>, Unchecked.defaultof<_> else

            expr.TryGetFcsType(), expr.TryGetFcsDisplayContext()

        let wholeRefExpr = wholeExpr.As<IReferenceExpr>()
        if isNotNull wholeRefExpr then
            getRefExprQualifierType wholeRefExpr else

        let initialRefExpr = initialExpr.As<IReferenceExpr>()
        if isNull initialRefExpr then Unchecked.defaultof<_>, Unchecked.defaultof<_> else

        let originalNode = initialRefExpr.TryGetOriginalNodeThroughSandBox(wholeExpr)
        if isNotNull originalNode then
            originalNode.TryGetFcsType(), originalNode.TryGetFcsDisplayContext() else

        Unchecked.defaultof<_>, Unchecked.defaultof<_>


[<PostfixTemplate("for", "Iterates over enumerable collection", "for _ in expr do ()")>]
type ForPostfixTemplate() =
    inherit FSharpPostfixTemplateBase()

    let isApplicableType (contextExpr: IFSharpExpression) (fcsType: FSharpType) =
        if isNull fcsType then false else

        let enumeratedType = ForPostfixTemplate.getEnumeratedType contextExpr fcsType
        isNotNull enumeratedType ||

        fcsType.IsGenericParameter && Seq.isEmpty fcsType.AllInterfaces

    let isApplicable (expr: IFSharpExpression) =
        if not (FSharpPostfixTemplates.canBecomeStatement expr) then false else

        let wholeExpr =
            expr
            |> FSharpPostfixTemplates.getContainingAppExprFromLastArg false
            |> FSharpPostfixTemplates.getContainingTupleExprFromLastItem

        let fcsType, displayContext = ForPostfixTemplate.getExpressionType wholeExpr expr
        isNotNull displayContext && isApplicableType wholeExpr fcsType

    override x.CreateBehavior(info) = ForPostfixTemplateBehavior(info :?> ForPostfixTemplateInfo) :> _

    override x.CreateInfo(context) =
        let expr = context.Expression.As<IReferenceExpr>()

        let wholeExpr =
            expr
            |> FSharpPostfixTemplates.getContainingAppExprFromLastArg false
            |> FSharpPostfixTemplates.getContainingTupleExprFromLastItem

        let fcsType, displayContext = ForPostfixTemplate.getExpressionType wholeExpr expr
        let enumeratedType = ForPostfixTemplate.getEnumeratedType wholeExpr fcsType

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
            let expr =
                expr
                |> FSharpPostfixTemplates.getContainingAppExprFromLastArg false
                |> FSharpPostfixTemplates.getContainingTupleExprFromLastItem

            FSharpPostfixTemplates.convertToBlockLikeExpr expr context
            let forEachExpr = expr.CreateElementFactory().CreateForEachExpr(expr)
            ModificationUtil.ReplaceChild(expr, forEachExpr) :> ITreeNode)

    override x.AfterComplete(textControl, node, _) =
        let id = "ForPostfixTemplateBehavior.AfterComplete"

        let solution = node.GetSolution()
        let psiServices = node.GetPsiServices()

        let forEachExpr = node :?> IForEachExpr

        let fcsType =
            forEachExpr.InExpression.TryGetFcsType()
            |> ForPostfixTemplate.getEnumeratedType forEachExpr

        let names =
            let namesCollection = FSharpNamingService.createEmptyNamesCollection forEachExpr
            match fcsType with
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

        match fcsType with
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
