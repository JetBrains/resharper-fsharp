namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.PostfixTemplates

open JetBrains.Application.Settings
open JetBrains.Diagnostics
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.CodeCompletion.PostfixTemplates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates
open JetBrains.ReSharper.Feature.Services.PostfixTemplates.Contexts
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCompletion
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Refactorings
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

module FSharpPostfixTemplates =
    type FormatterImplHelper = JetBrains.ReSharper.Psi.Impl.CodeStyle.FormatterImplHelper

    let isSingleLine (expr: IFSharpExpression) =
        let formatter = expr.Language.LanguageServiceNotNull().CodeFormatter
        not (FormatterImplHelper.HasLineFeedsTo(expr.FirstChild, expr.LastChild, formatter))

    let isApplicableTypeUsage (typeUsage: ITypeUsage) =
        let typeUsage = skipIntermediateParentsOfSameType<ITypeUsage> typeUsage
        let typedExpr = TypedLikeExprNavigator.GetByTypeUsage(typeUsage)
        isNotNull typedExpr

    let rec getContainingAppExprFromLastArg allowNewLines (expr: IFSharpExpression) =
        match PrefixAppExprNavigator.GetByArgumentExpression(expr) with
        | null ->
            match BinaryAppExprNavigator.GetByRightArgument(expr) with
            | null -> expr
            | binaryAppExpr when not allowNewLines ->
                let leftArg = binaryAppExpr.LeftArgument
                let rightArg = binaryAppExpr.RightArgument

                let formatter = expr.Language.LanguageServiceNotNull().CodeFormatter
                if isNotNull leftArg && isNotNull rightArg &&
                        not (FormatterImplHelper.HasLineFeedsTo(leftArg, rightArg, formatter)) then
                    binaryAppExpr :> _
                else
                    expr

            | binaryAppExpr -> binaryAppExpr

        | appExpr -> getContainingAppExprFromLastArg allowNewLines appExpr

    let getContainingTypeExpression (typeName: ITypeReferenceName) =
        let namedTypeUsage = NamedTypeUsageNavigator.GetByReferenceName(typeName)
        let tupleTypeUsage = TupleTypeUsageNavigator.GetByItem(namedTypeUsage)

        let typeUsage: ITypeUsage =
            if isNotNull tupleTypeUsage && tupleTypeUsage.Items.Last() == namedTypeUsage then
                tupleTypeUsage :> _
            else
                namedTypeUsage :> _

        let functionTypeUsage = FunctionTypeUsageNavigator.GetByReturnTypeUsage(typeUsage)
        let typeUsage: ITypeUsage = if isNotNull functionTypeUsage then functionTypeUsage :> _ else typeUsage

        let typedExpr = TypedLikeExprNavigator.GetByTypeUsage(typeUsage)
        if isNotNull typedExpr then
            getContainingAppExprFromLastArg true typedExpr else

        null

    let rec getContainingTupleExprFromLastItem (expr: IFSharpExpression) =
        let tupleExpr = TupleExprNavigator.GetByExpression(expr)
        if isNotNull tupleExpr && tupleExpr.Expressions.LastOrDefault() == expr then
            getContainingTupleExprFromLastItem tupleExpr
        else
            expr

    let canBecomeStatement (initialExpr: IFSharpExpression) : bool =
        let expr = 
            initialExpr
            |> (getContainingAppExprFromLastArg false)
            |> getContainingTupleExprFromLastItem

        if isNull expr then false else

        if not (FSharpIntroduceVariable.CanIntroduceVar(expr, true) &&
                FSharpIntroduceVariable.CanIntroduceVar(initialExpr, true)) then false else

        let formatter = expr.Language.LanguageServiceNotNull().CodeFormatter

        let isLastMeaningfulNodeOnLine (node: ITreeNode) =
            FormatterImplHelper.HasNewLineAfter(node, formatter) ||
            isNull (node.GetNextToken())

        let isFirstMeaningfulNodeOnLine node =
            FormatterImplHelper.HasNewLineBefore(node, formatter)

        let isBlockLike (getParent: IFSharpExpression -> #ITreeNode) parentIsApplicable =
            let parent = getParent expr
            parentIsApplicable parent

        let isParentApplicable (parent: ITreeNode) =
            isNotNull parent &&
            isLastMeaningfulNodeOnLine parent

        let isBinaryExprApplicable (parent: ITreeNode) =
            isNotNull parent &&
            isLastMeaningfulNodeOnLine expr &&
            isFirstMeaningfulNodeOnLine expr

        let isLambdaExprApplicable (lambdaExpr: ILambdaExpr) =
            isNotNull lambdaExpr &&
            isLastMeaningfulNodeOnLine lambdaExpr ||

            let parenExpr = ParenExprNavigator.GetByInnerExpression(lambdaExpr)
            isNotNull parenExpr && isLastMeaningfulNodeOnLine parenExpr

        let isLetExprApplicable (letExpr: ILetOrUseExpr) =
            isNotNull letExpr &&
            isLastMeaningfulNodeOnLine expr &&
            letExpr.Indent <= expr.Indent 

        let isSequentialExprApplicable (seqExpr: ISequentialExpr) =
            isNotNull seqExpr &&
            isLastMeaningfulNodeOnLine expr &&
            isFirstMeaningfulNodeOnLine expr

        let isExprStmtApplicable (exprStmt: IDoLikeStatement) =
            isNotNull exprStmt &&
            isLastMeaningfulNodeOnLine exprStmt &&
            Seq.isEmpty exprStmt.AttributesEnumerable

        isBlockLike ArrayOrListExprNavigator.GetByExpression isParentApplicable ||
        isBlockLike ComputationExprNavigator.GetByExpression isParentApplicable ||
        isBlockLike BindingNavigator.GetByExpression isParentApplicable ||
        isBlockLike BinaryAppExprNavigator.GetByArgument isBinaryExprApplicable ||
        isBlockLike DoLikeExprNavigator.GetByExpression isParentApplicable ||
        isBlockLike DoLikeStatementNavigator.GetByExpression isExprStmtApplicable ||
        isBlockLike ForEachExprNavigator.GetByDoExpression isParentApplicable ||
        isBlockLike IfExprNavigator.GetByBranchExpression isParentApplicable ||
        isBlockLike LambdaExprNavigator.GetByExpression isLambdaExprApplicable ||
        isBlockLike LetOrUseExprNavigator.GetByInExpression isLetExprApplicable ||
        isBlockLike MatchClauseNavigator.GetByExpression isParentApplicable ||
        isBlockLike QuoteExprNavigator.GetByQuotedExpression isParentApplicable ||
        isBlockLike TryFinallyExprNavigator.GetByFinallyExpression isParentApplicable ||
        isBlockLike TryLikeExprNavigator.GetByTryExpression isParentApplicable ||
        isBlockLike SequentialExprNavigator.GetByExpression isSequentialExprApplicable ||
        isBlockLike SetExprNavigator.GetByRightExpression isParentApplicable ||
        isBlockLike WhileExprNavigator.GetByDoExpression isParentApplicable

    let rec removeTemplateAndGetParentExpression (token: IFSharpTreeNode): IFSharpExpression =
        match token with
        | :? IReferenceExpr as refExpr ->
            let qualifier = refExpr.Qualifier.NotNull()
            ModificationUtil.ReplaceChild(refExpr, qualifier.Copy())

        | :? ITypeReferenceName as referenceName ->
            let qualifier = referenceName.Qualifier.As<ITypeReferenceName>().NotNull()
            let newReferenceName = ModificationUtil.ReplaceChild(referenceName, qualifier.Copy())
            getContainingTypeExpression newReferenceName

        | _ -> null

    let convertToBlockLikeExpr expr (context: PostfixExpressionContext) =
        let expr = getContainingTupleExprFromLastItem expr

        let contextExpr: ITreeNode = 
            match ChameleonExpressionNavigator.GetByExpression(expr) with
            | null -> expr
            | chameleon -> chameleon

        if not (isFirstMeaningfulNodeOnLine contextExpr) then
            let lineEnding = expr.GetLineEnding()

            let contextIndent = 
                let matchClause = MatchClauseNavigator.GetByExpression(expr)
                let tryFinallyExpr = TryWithExprNavigator.GetByClause(matchClause)
                if isNotNull tryFinallyExpr && matchClause.StartLine = tryFinallyExpr.WithKeyword.StartLine then
                    tryFinallyExpr.WithKeyword.Indent else

                let lambdaExpr = LambdaExprNavigator.GetByExpression(expr)
                if isNotNull lambdaExpr then
                    let formatter = lambdaExpr.Language.LanguageServiceNotNull().CodeFormatter
                    let indent = FormatterImplHelper.CalcLineIndent(lambdaExpr, formatter).Length

                    let parenExpr = ParenExprNavigator.GetByInnerExpression(lambdaExpr)
                    if isNotNull parenExpr && isNotNull parenExpr.RightParen then
                        let moveRparenToNewLine =
                            context.PostfixContext.ExecutionContext.SettingsStore
                                .GetValue(fun (key: FSharpFormatSettingsKey) -> key.MultiLineLambdaClosingNewline)

                        if moveRparenToNewLine then
                            addNodesBefore parenExpr.RightParen [
                                NewLine(lineEnding)
                                Whitespace(indent)
                            ] |> ignore

                    indent
                else
                    contextExpr.Parent.Indent

            addNodesBefore contextExpr [
                NewLine(lineEnding)
                Whitespace(contextIndent + expr.GetIndentSize())
            ] |> ignore


[<AllowNullLiteral>]
type FSharpPostfixTemplateContext(node: ITreeNode, executionContext: PostfixTemplateExecutionContext) =
    inherit PostfixTemplateContext(node, executionContext)

    // todo: override IsSemanticallyMakeSense to disable on namespaces/modules?

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
            let node = FSharpReparsedCodeCompletionContext.FixReferenceOwnerUnderTransaction(node)
            let fsIdentifier = node.As<IFSharpIdentifier>()

            let referenceExpr = ReferenceExprNavigator.GetByIdentifier(fsIdentifier)
            if isNotNull referenceExpr then FSharpPostfixTemplateContext(referenceExpr, executionContext) :> _ else

            let referenceName = TypeReferenceNameNavigator.GetByIdentifier(fsIdentifier)
            if isNotNull referenceName then FSharpPostfixTemplateContext(referenceName, executionContext) :> _ else

            null


[<Language(typeof<FSharpLanguage>)>]
type FSharpPostfixTemplatesProvider(templatesManager, sessionExecutor, usageStatistics) =
    inherit PostfixTemplatesItemProviderBase<FSharpCodeCompletionContext, FSharpPostfixTemplateContext>(
        templatesManager, sessionExecutor, usageStatistics)

    override this.TryCreatePostfixContext(fsCompletionContext) =
        if fsCompletionContext.NodeInFile.IsFSharpSigFile() then null else

        let reparsedContext = fsCompletionContext.ReparsedContext
        let reference = reparsedContext.Reference.As<FSharpSymbolReference>()
        if isNull reference || not reference.IsQualified then null else

        let node = reference.GetTreeNode().NotNull()
        let context = fsCompletionContext.BasicContext
        let settings = context.ContextBoundSettingsStore
        let executionContext = PostfixTemplateExecutionContext(context.Solution, context.TextControl, settings, "__")
        FSharpPostfixTemplateContext(node, executionContext)


[<AbstractClass>]
type FSharpPostfixTemplateBehaviorBase(info) =
    inherit PostfixTemplateBehavior(info)

    member this.GetExpression(context: PostfixExpressionContext) =
        let node = context.Expression :?> IFSharpTreeNode
        FSharpPostfixTemplates.removeTemplateAndGetParentExpression node


[<AbstractClass>]
type FSharpPostfixTemplateBase() =
    member this.Language = FSharpLanguage.Instance

    abstract IsApplicable: ITreeNode -> bool
    abstract CreateBehavior: PostfixTemplateInfo -> PostfixTemplateBehavior
    abstract CreateInfo: PostfixExpressionContext -> PostfixTemplateInfo

    abstract IsEnabled: ISolution -> bool
    default this.IsEnabled(solution) =
        solution.IsFSharpExperimentalFeatureEnabled(ExperimentalFeature.PostfixTemplates)

    interface IPostfixTemplate with
        member this.Language = this.Language :> _
        member this.CreateBehavior(info) = this.CreateBehavior(info)

        member this.TryCreateInfo(templateContext) =
            if not (this.IsEnabled(templateContext.PsiModule.GetSolution())) then null else

            let exprContext = templateContext.AllExpressions[0]
            let node = exprContext.Expression
            if isNotNull node && this.IsApplicable(node) then
                this.CreateInfo(exprContext)
            else
                null
