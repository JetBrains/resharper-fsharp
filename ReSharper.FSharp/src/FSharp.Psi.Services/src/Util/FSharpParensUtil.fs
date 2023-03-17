[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpParensUtil

open System
open FSharp.Compiler.Syntax
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree

let lastBlockHasSameIndent (expr: IFSharpExpression) =
    match expr with
    | :? IMatchClauseListOwnerExpr as clausesOwnerExpr ->
        if expr.IsSingleLine then false else

        let clause = clausesOwnerExpr.Clauses.LastOrDefault()
        if isNull clause then false else

        let clauseExpr = clause.Expression
        isNotNull clauseExpr && clauseExpr.Indent = expr.Indent

    | :? IIfExpr as ifExpr ->
        if expr.IsSingleLine then false else

        let elseExpr = ifExpr.ElseExpr
        isNotNull elseExpr && elseExpr.Indent = expr.Indent

    | _ -> false


let rec deindentsBody blockIndent (expr: IFSharpExpression) =
    isNotNull expr && expr.Indent < blockIndent ||

    match expr with
    | :? ISequentialExpr as seqExpr ->
        seqExpr.ExpressionsEnumerable |> Seq.exists (deindentsBody blockIndent)

    | :? IMatchClauseListOwnerExpr as clausesOwnerExpr ->
        clausesOwnerExpr.ClauseExpressionsEnumerable |> Seq.exists (deindentsBody blockIndent)

    | :? ILetOrUseExpr as letExpr ->
        deindentsBody blockIndent letExpr.InExpression

    | :? IBinaryAppExpr as binaryAppExpr ->
        let op = binaryAppExpr.Operator
        let leftArg = binaryAppExpr.LeftArgument

        isNotNull op && isNotNull leftArg && op.Indent + op.GetTextLength() + 1 < leftArg.Indent ||

        deindentsBody blockIndent leftArg ||
        deindentsBody blockIndent binaryAppExpr.RightArgument

    | :? ILambdaExpr as lambdaExpr ->
        deindentsBody blockIndent lambdaExpr.Expression

    | :? IParenExpr as parenExpr ->
        deindentsBody blockIndent parenExpr.InnerExpression

    | _ -> false


let contextRequiresParens (expr: IFSharpExpression) (context: IFSharpExpression) =
    isNotNull (ObjExprNavigator.GetByArgExpression(context)) ||
    isNotNull (NewExprNavigator.GetByArgumentExpression(context)) ||
    isNotNull (RecordExprNavigator.GetByInheritCtorArgExpression(context)) ||
    isNotNull (TypeInheritNavigator.GetByCtorArgExpression(context)) ||

    let dynamicExpr = DynamicExprNavigator.GetByArgumentExpression(context)
    let refExpr = expr.As<IReferenceExpr>()
    isNotNull dynamicExpr && (isNull refExpr || not refExpr.IsSimpleName)


let (|Prefix|_|) (other: string) (str: string) =
    if str.StartsWith(other, StringComparison.Ordinal) then someUnit else None

let operatorName (binaryApp: IBinaryAppExpr) =
    let refExpr = binaryApp.Operator
    if isNull refExpr then SharedImplUtil.MISSING_DECLARATION_NAME else

    // todo: fix op tokens in references
    let name = refExpr.GetText()
    PrettyNaming.ConvertValLogicalNameToDisplayNameCore(name)

let operatorPrecedence (binaryApp: IBinaryAppExpr) =
    let name = operatorName binaryApp
    if name.Length = 0 then 0 else

    match name with
    | "|" | "||" -> 1
    | "&" | "&&" -> 2
    | Prefix "!=" | Prefix "<" | Prefix ">" | Prefix "|" | Prefix "&" | "$" | "=" -> 4
    | Prefix "^" -> 5
    | Prefix "::" -> 6
    | Prefix "+" | Prefix "-" -> 8
    | Prefix "*" | Prefix "/" | Prefix "%" -> 9
    | Prefix "**" -> 10
    | _ -> 0

let precedence (expr: ITreeNode) =
    match expr with
    | :? ILibraryOnlyExpr
    | :? ITraitCallExpr -> 0

    | :? IReferenceExpr as refExpr when (refExpr.Identifier :? IActivePatternId) -> 0

    | :? ILetOrUseExpr -> 1
    | :? IYieldOrReturnExpr -> 2

    | :? IForLikeExpr
    | :? IIfExpr
    | :? IMatchClauseListOwnerExpr
    | :? ITryLikeExpr
    | :? IWhileExpr -> 4

    // todo: type test, cast, typed
    | :? ITypedLikeExpr -> 5
    | :? ILambdaExpr -> 6

    | :? ISequentialExpr
    | :? ISetExpr -> 7

    | :? ITupleExpr -> 8

    | :? IBinaryAppExpr as binaryAppExpr ->
        // todo: remove this hack and align common precedence
        match operatorName binaryAppExpr with
        | "|>" -> 3
        | _ -> 9

    | :? IDoLikeExpr -> 10
    | :? INewExpr -> 11

    | :? IAddressOfExpr -> 12

    | :? IPrefixAppExpr as prefixApp ->
        if isOperatorReferenceExpr prefixApp.FunctionExpression then 12 else
        if prefixApp.IsHighPrecedence then 13 else 12

    | :? IFSharpExpression -> 14

    | _ -> 0

let startsBlock (context: IFSharpExpression) =
//    isNotNull (BinaryAppExprNavigator.GetByRightArgument(context)) || // todo: not really a block here :(
    isNotNull (SetExprNavigator.GetByRightExpression(context))

let getContextPrecedence (context: IFSharpExpression) =
    if context :? IInterpolatedStringExpr then 0 else

    if isNotNull (QualifiedExprNavigator.GetByQualifier(context)) then 13 else

    if startsBlock context then 0 else precedence context.Parent

let checkPrecedence (context: IFSharpExpression) node =
    let nodePrecedence = precedence node
    let contextPrecedence = getContextPrecedence context
    nodePrecedence < contextPrecedence


let rec getPossibleStrictContextExpr (context: IFSharpExpression): IFSharpExpression =
    let binaryAppExpr = BinaryAppExprNavigator.GetByArgument(context)
    if isNotNull binaryAppExpr then getPossibleStrictContextExpr binaryAppExpr else

    let letExpr = LetOrUseExprNavigator.GetByInExpression(context)
    if isNotNull letExpr then getPossibleStrictContextExpr letExpr else

    context

let strictContextRequiresDeclExpr (context: IFSharpExpression) =
    isNotNull (WhenExprClauseNavigator.GetByExpression(context))

let contextRequiresDeclExpr (context: IFSharpExpression) =
    let strictContextExpr = getPossibleStrictContextExpr context
    strictContextRequiresDeclExpr strictContextExpr

let rec getLongestBinaryAppParentViaRightArg (context: IFSharpExpression): IFSharpExpression =
    match BinaryAppExprNavigator.GetByRightArgument(context) with
    | null -> context
    | binaryAppExpr -> getLongestBinaryAppParentViaRightArg binaryAppExpr

let rec getQualifiedExpr (expr: IFSharpExpression) =
    match QualifiedExprNavigator.GetByQualifier(expr.IgnoreParentParens()) with
    | null -> expr.IgnoreParentParens()
    | expr -> getQualifiedExpr expr

let rec getFirstQualifier (expr: IQualifiedExpr) =
    match expr.Qualifier with
    | null -> expr :> IFSharpExpression
    | :? IQualifiedExpr as qualifier -> getFirstQualifier qualifier
    | qualifier -> qualifier


//let private canBeTopLevelArgInHighPrecedenceApp (expr: IFSharpExpression) =
//    // todo: check `ignore{| Field = 1 + 1 |}.Field` vs `ignore[].Head`
//    expr :? IArrayOrListExpr || expr :? IObjExpr || expr :? IRecordLikeExpr

let isHighPrecedenceAppArg context =
    let appExpr = PrefixAppExprNavigator.GetByArgumentExpression(context)
    if isNotNull appExpr then
        let funExpr = appExpr.FunctionExpression
        isNotNull funExpr && funExpr.NextSibling == context else

    // todo: add test with spaces
    let chameleonExpr = ChameleonExpressionNavigator.GetByExpression(context)
    let attribute = AttributeNavigator.GetByArgExpression(chameleonExpr)
    if isNotNull attribute then
        let referenceName = attribute.ReferenceName
        isNotNull referenceName && referenceName.NextSibling == chameleonExpr else

    false

let rec needsParensInDeclExprContext (expr: IFSharpExpression) =
    match expr with
    | null -> false

    | :? IConditionOwnerExpr
    | :? IForLikeExpr
    | :? IMatchClauseListOwnerExpr
    | :? ISequentialExpr
    | :? ITupleExpr
    | :? ITypedLikeExpr ->
        true

    | :? IBinaryAppExpr as binaryAppExpr ->
        needsParensInDeclExprContext binaryAppExpr.RightArgument ||
        needsParensInDeclExprContext binaryAppExpr.LeftArgument

    | :? ILetOrUseExpr as letExpr ->
        needsParensInDeclExprContext letExpr.InExpression

    | _ -> false

let escapesTupleAppArg (context: IFSharpExpression) (expr: IFSharpExpression) =
    match expr with
    | :? IParenExpr as parenExpr ->
        match parenExpr.InnerExpression with
        | :? ITupleExpr -> isNotNull (AppLikeExprNavigator.GetByArgumentExpression(context))
        | _ -> false
    | _ -> false

let escapesRefExprAtNamedArgPosition (context: IFSharpExpression) (expr: IFSharpExpression) =
    let refExpr = expr.As<IReferenceExpr>()
    isNotNull refExpr && refExpr.IsSimpleName &&

    let binaryAppExpr = BinaryAppExprNavigator.GetByLeftArgument(context)
    isNotNull binaryAppExpr && FSharpMethodInvocationUtil.isTopLevelArg binaryAppExpr

let escapesAppAtNamedArgPosition (parenExpr: IParenExpr) =
    match parenExpr.InnerExpression with
    | :? IParenExpr as innerParenExpr ->
        match innerParenExpr.InnerExpression with
        | :? IBinaryAppExpr as binaryAppExpr ->
            FSharpMethodInvocationUtil.hasNamedArgStructure binaryAppExpr &&
            isNotNull (FSharpArgumentOwnerNavigator.GetByArgumentExpression(parenExpr.IgnoreParentParens()))
        | _ -> false
    | :? IBinaryAppExpr as binaryAppExpr ->
        FSharpMethodInvocationUtil.hasNamedArgStructure binaryAppExpr &&
        FSharpMethodInvocationUtil.isTopLevelArg parenExpr
    | _ -> false

let literalsRequiringParens =
    NodeTypeSet(FSharpTokenType.INT32, FSharpTokenType.IEEE32, FSharpTokenType.IEEE64)

let rec needsParensImpl (allowHighPrecedenceAppParens: unit -> bool) (context: IFSharpExpression) (expr: IFSharpExpression) =
    if escapesTupleAppArg context expr then true else
    if expr :? IParenOrBeginEndExpr then false else

    let expr = expr.IgnoreInnerParens()
    if isNull expr|| contextRequiresParens expr context then true else

    let parentPrefixAppExpr = PrefixAppExprNavigator.GetByArgumentExpression(context)
    if isNotNull parentPrefixAppExpr && parentPrefixAppExpr.IsHighPrecedence &&
            isNotNull (QualifiedExprNavigator.GetByQualifier(parentPrefixAppExpr)) then true else

    if isHighPrecedenceAppArg context && allowHighPrecedenceAppParens () then true else

    match expr with
    | :? IIfThenElseExpr ->
        isNotNull (IfThenElseExprNavigator.GetByThenExpr(context)) ||
        isNotNull (ConditionOwnerExprNavigator.GetByConditionExpr(context)) ||
        isNotNull (ForEachExprNavigator.GetByInExpression(context)) ||
        isNotNull (BinaryAppExprNavigator.GetByLeftArgument(context)) ||
        isNotNull (PrefixAppExprNavigator.GetByFunctionExpression(context)) ||
        isNotNull (TypedLikeExprNavigator.GetByExpression(context)) ||

        contextRequiresDeclExpr context ||

        let tupleExpr = TupleExprNavigator.GetByExpression(context)
        isNotNull tupleExpr && tupleExpr.Expressions.LastOrDefault() != context ||

        checkPrecedence context expr

    | :? IMatchClauseListOwnerExpr as matchExpr ->
        contextRequiresDeclExpr context ||
        checkPrecedence context expr ||

        let lastClause = matchExpr.ClausesEnumerable.LastOrDefault()
        let lastClauseExpr = if isNull lastClause then null else lastClause.Expression
        if isNull lastClauseExpr then false else // todo: or true?

        needsParensImpl allowHighPrecedenceAppParens context lastClauseExpr ||
        lastClauseExpr.Indent = matchExpr.Indent ||

        let binaryAppExpr = BinaryAppExprNavigator.GetByLeftArgument(context)
        let opExpr = if isNull binaryAppExpr then null else binaryAppExpr.Operator

        isNotNull opExpr && opExpr.Indent <> matchExpr.Indent ||

        false

    | :? ITupleExpr ->
        isNotNull (AttributeNavigator.GetByExpression(context)) ||
        isNotNull (TupleExprNavigator.GetByExpression(context)) ||

        // todo: enable for {(struct (1, 2))}
        isNotNull (InterpolatedStringExprNavigator.GetByInsert(context)) ||

        contextRequiresDeclExpr context ||

        checkPrecedence context expr

    | :? IReferenceExpr as refExpr ->
        let qualifier = refExpr.Qualifier
        let typeArgumentList = refExpr.TypeArgumentList

        let attribute = AttributeNavigator.GetByExpression(context)
        isNotNull attribute && (isNotNull attribute.Target || isNotNull typeArgumentList || isNotNull qualifier) ||

        isNotNull (PrefixAppExprNavigator.GetByArgumentExpression(context)) && getFirstQualifier refExpr :? IAppExpr ||

        checkPrecedence context expr

    | :? IIndexerExpr as indexerExpr ->
        isNotNull (PrefixAppExprNavigator.GetByArgumentExpression(context)) && getFirstQualifier indexerExpr :? IAppExpr ||

        checkPrecedence context expr
    
    | :? ITypedLikeExpr ->
        expr :? ITypedExpr && isNotNull (BinaryAppExprNavigator.GetByLeftArgument(context)) ||
        expr :? ITypedExpr && isNotNull (YieldOrReturnExprNavigator.GetByExpression(context)) ||

        contextRequiresDeclExpr context ||
        checkPrecedence context expr

    | :? IBinaryAppExpr as binaryAppExpr ->
        let precedence = operatorPrecedence binaryAppExpr

        // todo: check assoc

        let parentViaLeftArg = BinaryAppExprNavigator.GetByLeftArgument(context)
        isNotNull parentViaLeftArg && operatorPrecedence parentViaLeftArg > precedence ||

        let parentViaRightArg = BinaryAppExprNavigator.GetByRightArgument(context)
        isNotNull parentViaRightArg && operatorPrecedence parentViaRightArg >= precedence ||

        // todo: only check this when expr is limited by some block
        // RedundantParenExprTest.``Seq - Binary - Deindent 01``
        let op = binaryAppExpr.Operator
        let leftArg = binaryAppExpr.LeftArgument
        isNotNull op && isNotNull leftArg && op.Indent + op.GetTextLength() + 1 < leftArg.Indent ||

        let strictContextExpr = getPossibleStrictContextExpr context
        strictContextRequiresDeclExpr strictContextExpr && needsParensInDeclExprContext binaryAppExpr ||

        checkPrecedence context expr

    | :? IPrefixAppExpr as appExpr ->
        isNotNull (PrefixAppExprNavigator.GetByArgumentExpression(getQualifiedExpr context)) ||

        let isQualifier = isNotNull (QualifiedExprNavigator.GetByQualifier(context))
        isQualifier && isIndexerLikeAppExpr appExpr || 

        checkPrecedence context expr

    | :? ISequentialExpr as seqExpr ->
        deindentsBody seqExpr.Indent seqExpr ||

        let strictContextExpr = getPossibleStrictContextExpr context
        isNotNull (ConditionOwnerExprNavigator.GetByConditionExpr(strictContextExpr)) ||
        isNotNull (YieldOrReturnExprNavigator.GetByExpression(strictContextExpr)) ||
        strictContextRequiresDeclExpr strictContextExpr ||

        checkPrecedence context expr

    | :? ILetOrUseExpr ->
        let strictContextExpr = getPossibleStrictContextExpr context
        strictContextRequiresDeclExpr strictContextExpr && needsParensInDeclExprContext expr ||
        isNotNull (IfThenElseExprNavigator.GetByConditionExpr(strictContextExpr)) ||

        checkPrecedence context expr

    | :? ILambdaExpr ->
        isNotNull (BinaryAppExprNavigator.GetByLeftArgument(context)) ||
        isNotNull (PrefixAppExprNavigator.GetByFunctionExpression(context)) ||
        isNotNull (TypedLikeExprNavigator.GetByExpression(context)) ||

        checkPrecedence context expr

    | :? ILiteralExpr as literalExpr ->
        // todo: check digits after dot: `1.0.Prop` is allowed, and `1.Prop` is not.
        let tokenType = getTokenType literalExpr.Literal
        literalsRequiringParens[tokenType] && isNotNull (QualifiedExprNavigator.GetByQualifier(context))

    | _ ->

    let binaryApp = BinaryAppExprNavigator.GetByLeftArgument(context)
    if isNull binaryApp then checkPrecedence context expr else

    if lastBlockHasSameIndent expr then true else

    let operator = binaryApp.Operator
    if isNotNull operator && context.Indent = operator.Indent then false else

    let rightArgument = binaryApp.RightArgument
    if isNotNull rightArgument && context.Indent = rightArgument.Indent then false else

    precedence binaryApp.LeftArgument < precedence binaryApp

let allowHighPrecedenceAppParens (node: ITreeNode) =
    let settingsStore = node.GetSettingsStoreWithEditorConfig()
    SettingsUtil.getBoundValue<FSharpFormatSettingsKey, bool> settingsStore "AllowHighPrecedenceAppParens"

let needsParens (context: IFSharpExpression) (expr: IFSharpExpression) =
    let allowHighPrecedenceAppParens () = allowHighPrecedenceAppParens context
    needsParensImpl allowHighPrecedenceAppParens context expr

let addParens (expr: IFSharpExpression) =
    let exprCopy = expr.Copy()
    let factory = expr.CreateElementFactory()

    let parenExpr = factory.CreateParenExpr()
    let parenExpr = ModificationUtil.ReplaceChild(expr, parenExpr)
    let expr = parenExpr.SetInnerExpression(exprCopy)

    shiftNode 1 expr
    expr


let addParensIfNeeded (expr: IFSharpExpression) =
    let context = expr.IgnoreParentParens(includingBeginEndExpr = false)
    if context != expr || not (needsParens context expr) then expr else
    addParens expr
