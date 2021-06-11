namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.PostfixTemplates

open System
open JetBrains.Diagnostics
open JetBrains.ReSharper.Feature.Services.CodeCompletion.PostfixTemplates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates.Contexts
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util
open JetBrains.Util.Extension

[<AllowNullLiteral>]
type FSharpPostfixTemplateContext(node: ITreeNode, executionContext: PostfixTemplateExecutionContext) =
    inherit PostfixTemplateContext(node, executionContext)

    override this.Language = FSharpLanguage.Instance :> _

    override this.GetAllExpressionContexts() =
        [| FSharpPostfixExpressionContext(this, node) :> PostfixExpressionContext |] :> _


and FSharpPostfixExpressionContext(postfixContext, expression) =
    inherit PostfixExpressionContext(postfixContext, expression)


[<Language(typeof<FSharpLanguage>)>]
type FSharpPostfixTemplateContextFactory() =
    interface IPostfixTemplateContextFactory with
        member this.GetReparseStrings() = EmptyArray.Instance

        member this.TryCreate(node, executionContext) =
            if isNull node then null else

            let solution = executionContext.Solution
            if not (solution.IsFSharpExperimentalFeatureEnabled(ExperimentalFeature.PostfixTemplates)) then null else

            FSharpPostfixTemplateContext(node, executionContext) :> _


[<Language(typeof<FSharpLanguage>)>]
type FSharpPostfixTemplatesProvider(templatesManager, sessionExecutor, usageStatistics) =
    inherit PostfixTemplatesItemProviderBase<FSharpCodeCompletionContext, FSharpPostfixTemplateContext>(
        templatesManager, sessionExecutor, usageStatistics)

    let isApplicableToken (token: ITreeNode) =
        let tokenType = getTokenType token
        tokenType == FSharpTokenType.DOT ||
        tokenType == FSharpTokenType.IEEE64 ||
        tokenType == FSharpTokenType.DECIMAL ||
        tokenType == FSharpTokenType.RESERVED_LITERAL_FORMATS ||
        tokenType == FSharpTokenType.IDENTIFIER && getTokenType token.PrevSibling == FSharpTokenType.DOT

    override this.TryCreatePostfixContext(fsCompletionContext) =
        let context = fsCompletionContext.BasicContext
        let solution = context.Solution
        if not (solution.IsFSharpExperimentalFeatureEnabled(ExperimentalFeature.PostfixTemplates)) then null else

        let token = fsCompletionContext.TokenBeforeCaret
        if isNull token || not (token.Parent :? IFSharpExpression && isApplicableToken token) then null else

        let settings = context.ContextBoundSettingsStore
        let executionContext = PostfixTemplateExecutionContext(solution, context.TextControl, settings, "__")
        FSharpPostfixTemplateContext(token, executionContext)


[<AbstractClass>]
type FSharpPostfixTemplateBehaviorBase(info) =
    inherit PostfixTemplateBehavior(info)

    let rec getContainingArgExpr (expr: IFSharpExpression) =
        match PrefixAppExprNavigator.GetByArgumentExpression(expr) with
        | null ->
            match BinaryAppExprNavigator.GetByRightArgument(expr) with
            | null -> expr
            | binaryAppExpr -> binaryAppExpr :> _
        | appExpr -> getContainingArgExpr appExpr

    let getContainingTypeExpression (typeName: IReferenceName) =
        match NamedTypeUsageNavigator.GetByReferenceName(typeName.As()) with
        | null -> null
        | namedTypeUsage ->

        let castExpr = CastExprNavigator.GetByTypeUsage(namedTypeUsage)
        if isNotNull castExpr then
            getContainingArgExpr castExpr else

        let typedExpr = TypedExprNavigator.GetByType(namedTypeUsage)
        if isNotNull typedExpr then
            getContainingArgExpr typedExpr else

        null


    let getParentExpression (token: IFSharpTreeNode): IFSharpExpression =
        match token with
        | TokenType FSharpTokenType.RESERVED_LITERAL_FORMATS _ ->
            match token.Parent.As<IConstExpr>() with
            | null -> null
            | parent ->

            let literalText = token.GetText().SubstringBeforeLast(".", StringComparison.Ordinal)
            let constExpr = token.CreateElementFactory().CreateConstExpr(literalText)
            let newChild = ModificationUtil.ReplaceChild(parent.FirstChild, constExpr.FirstChild)
            getContainingArgExpr (newChild.Parent.As())

        | :? IFSharpIdentifier as identifier ->
            let refExpr = ReferenceExprNavigator.GetByIdentifier(identifier)
            if isNotNull refExpr && isNull (ReferenceExprNavigator.GetByQualifier(refExpr)) then
                let qualifier = refExpr.Qualifier.NotNull()
                ModificationUtil.ReplaceChild(refExpr, qualifier.Copy()) else

            let referenceName = ReferenceNameNavigator.GetByIdentifier(identifier)
            if isNotNull referenceName && isNull (ReferenceNameNavigator.GetByQualifier(referenceName)) then
                let qualifier = referenceName.Qualifier.NotNull()
                let newReferenceName = ModificationUtil.ReplaceChild(referenceName, qualifier.Copy())
                getContainingTypeExpression newReferenceName else

            null

        | _ -> null

    member this.GetExpression(context: PostfixExpressionContext) =
        let token = context.Expression :?> IFSharpTreeNode
        let parent = getParentExpression token
        getContainingArgExpr parent
