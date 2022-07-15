namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.PostfixTemplates

open System.Collections.Generic
open FSharp.Compiler.Symbols
open JetBrains.Application.Environment
open JetBrains.Application.Environment.Helpers
open JetBrains.Application.Progress
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots
open JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates.Contexts
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Search
open JetBrains.ReSharper.Psi.Transactions
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Resources.Shell


[<PostfixTemplate("for", "Iterates over enumerable collection", "for _ in expr do ()")>]
type ForPostfixTemplate() =
    inherit FSharpPostfixTemplateBase()

    let isApplicableType (fcsType: FSharpType) (contextExpr: IFSharpExpression) =
        if isNull fcsType then false else

        let exprType = fcsType.MapType(contextExpr)
        if isNull exprType then false else

        if exprType :? IArrayType then true else

        let declaredType = exprType.As<IDeclaredType>()
        if isNull declaredType then false else

        let typeElement = declaredType.GetTypeElement()
        if isNull typeElement then false else

        let enumerableTypeElement = exprType.Module.GetPredefinedType().IEnumerable.GetTypeElement()

        let typeParameter = typeElement.As<ITypeParameter>()
        if isNotNull typeParameter then
            let superTypeElements = typeParameter.GetSuperTypeElements()
            superTypeElements |> Seq.exists (fun t -> t.IsDescendantOf(enumerableTypeElement))
        else
            typeElement.IsDescendantOf(enumerableTypeElement)

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
        let refExpr = expr.As<IReferenceExpr>()
        if isNull refExpr then false else

        let expr = refExpr.Qualifier
        if isNull expr then false else

        let expr = expr.TryGetOriginalNodeThroughSandBox()
        if isNull expr then false else

        let fcsType = expr.TryGetFcsType()
        isApplicableType fcsType expr ||

        let refExpr = expr.As<IReferenceExpr>()
        isApplicableDeclaredElement refExpr

    override x.CreateBehavior(info) = ForPostfixTemplateBehavior(info) :> _

    override x.TryCreateInfo(context) =
        let context = context.AllExpressions[0]
        let fsExpr = context.Expression.As<IFSharpExpression>()
        if not (isApplicable fsExpr) then null else

        ForPostfixTemplateInfo(context) :> _

    override this.IsEnabled(solution) =
        let configurations = solution.GetComponent<RunsProducts.ProductConfigurations>()
        configurations.IsInternalMode() || base.IsEnabled(solution)


and ForPostfixTemplateInfo(expressionContext: PostfixExpressionContext) =
    inherit PostfixTemplateInfo("for", expressionContext)


and ForPostfixTemplateBehavior(info) =
    inherit FSharpPostfixTemplateBehaviorBase(info)

    override x.ExpandPostfix(context) =
        let psiModule = context.PostfixContext.PsiModule
        let psiServices = psiModule.GetPsiServices()

        psiServices.Transactions.Execute(x.ExpandCommandName, fun _ ->
            let node = context.Expression :?> IFSharpTreeNode
            use writeCookie = WriteLockCookie.Create(node.IsPhysical())
            use disableFormatter = new DisableCodeFormatter()

            let refExpr = x.GetExpression(context)
            let forEachExpr = refExpr.CreateElementFactory().CreateForEachExpr(refExpr)
            ModificationUtil.ReplaceChild(refExpr, forEachExpr) :> ITreeNode)

    override x.AfterComplete(textControl, node, _) =
        let forEachExpr = node.As<IForEachExpr>()
        if isNull forEachExpr then () else

        let hotspotInfos =
            let templateField = TemplateField("Foo", SimpleHotspotExpression(null), 0)
            HotspotInfo(templateField, forEachExpr.Pattern.GetDocumentRange(), KeepExistingText = true)

        let hotspotSession =
            LiveTemplatesManager.Instance.CreateHotspotSessionAtopExistingText(
                info.ExecutionContext.Solution, forEachExpr.GetDocumentEndOffset(), textControl,
                LiveTemplatesManager.EscapeAction.LeaveTextAndCaret, hotspotInfos)

        hotspotSession.ExecuteAndForget()
