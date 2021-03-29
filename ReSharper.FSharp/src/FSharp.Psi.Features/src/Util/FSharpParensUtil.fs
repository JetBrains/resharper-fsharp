[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpParensUtil

open System
open FSharp.Compiler.Syntax
open JetBrains.Application.Settings
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
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


let contextRequiresParens (context: IFSharpExpression) =
    isNotNull (ObjExprNavigator.GetByArgExpression(context)) ||
    isNotNull (NewExprNavigator.GetByArgumentExpression(context)) ||
    isNotNull (RecordExprNavigator.GetByInheritCtorArgExpression(context)) ||
    isNotNull (TypeInheritNavigator.GetByCtorArgExpression(context))


let isHighPrecedenceApp (appExpr: IPrefixAppExpr) =
    if isNull appExpr then false else

    let funExpr = appExpr.FunctionExpression
    let argExpr = appExpr.ArgumentExpression

    // todo: attribute arg :(
    isNotNull funExpr && isNotNull argExpr && funExpr.NextSibling == argExpr


let (|Prefix|_|) (other: string) (str: string) =
    if str.StartsWith(other, StringComparison.Ordinal) then someUnit else None

let operatorName (binaryApp: IBinaryAppExpr) =
    let refExpr = binaryApp.Operator
    if isNull refExpr then SharedImplUtil.MISSING_DECLARATION_NAME else

    // todo: fix op tokens in references
    let name = refExpr.GetText()
    PrettyNaming.DecompileOpName(name)

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
    | :? IActivePatternExpr
    | :? ITraitCallExpr -> 0

    | :? ILetOrUseExpr -> 1

    | :? IForLikeExpr
    | :? IIfExpr
    | :? IMatchClauseListOwnerExpr
    | :? ITryLikeExpr
    | :? IWhileExpr -> 3

    // todo: type test, cast, typed
    | :? ITypedLikeExpr -> 4
    | :? ILambdaExpr -> 5
    | :? ISequentialExpr -> 6
    | :? ITupleExpr -> 7

    | :? IBinaryAppExpr as binaryAppExpr ->
        // todo: remove this hack and align common precedence
        match operatorName binaryAppExpr with
        | "|>" -> 2
        | _ -> 8

    | :? IDoLikeExpr -> 9
    | :? INewExpr -> 10

    | :? IPrefixAppExpr as prefixApp ->
        if isHighPrecedenceApp prefixApp then 12 else 11

    | :? IFSharpExpression -> 13

    | _ -> 0

let startsBlock (context: IFSharpExpression) =
//    isNotNull (BinaryAppExprNavigator.GetByRightArgument(context)) || // todo: not really a block here :(
    isNotNull (SetExprNavigator.GetByRightExpression(context))

let getContextPrecedence (context: IFSharpExpression) =
    if isNotNull (QualifiedExprNavigator.GetByQualifier(context)) then 12 else

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
    isNotNull (WhenExprClauseNavigator.GetByExpression(context)) ||
    isNotNull (YieldOrReturnExprNavigator.GetByExpression(context))

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
        | :? ITupleExpr -> isNotNull (PrefixAppExprNavigator.GetByArgumentExpression(context))
        | _ -> false
    | _ -> false

let literalsRequiringParens =
    NodeTypeSet(FSharpTokenType.INT32, FSharpTokenType.IEEE32, FSharpTokenType.IEEE64)

let rec needsParens (context: IFSharpExpression) (expr: IFSharpExpression) =
    if escapesTupleAppArg context expr then true else
    if expr :? IParenExpr then false else

    let expr = expr.IgnoreInnerParens()
    if isNull expr|| contextRequiresParens context then true else

    let ParentPrefixAppExpr = PrefixAppExprNavigator.GetByArgumentExpression(context)
    if isHighPrecedenceApp ParentPrefixAppExpr && isNotNull (QualifiedExprNavigator.GetByQualifier(ParentPrefixAppExpr)) then true else

    // todo: calc once?
    let allowHighPrecedenceAppParens () = 
        let settingsStore = context.GetSettingsStoreWithEditorConfig()
        settingsStore.GetValue(fun (key: FSharpFormatSettingsKey) -> key.AllowHighPrecedenceAppParens)

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

        needsParens context lastClauseExpr ||
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

        isNotNull (AppExprNavigator.GetByArgument(context)) && getFirstQualifier refExpr :? IAppExpr ||

        // todo: tests
        isNull typeArgumentList && isNull qualifier && PrettyNaming.IsOperatorName (refExpr.GetText()) ||

        checkPrecedence context expr

    | :? ITypedLikeExpr ->
        expr :? ITypedExpr && isNotNull (BinaryAppExprNavigator.GetByLeftArgument(context)) ||

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

    | :? IPrefixAppExpr ->
        isNotNull (PrefixAppExprNavigator.GetByArgumentExpression(getQualifiedExpr context)) ||
        checkPrecedence context expr

    | :? ISequentialExpr as seqExpr ->
        deindentsBody seqExpr.Indent seqExpr ||

        let strictContextExpr = getPossibleStrictContextExpr context
        isNotNull (ConditionOwnerExprNavigator.GetByConditionExpr(strictContextExpr)) ||
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
        literalsRequiringParens.[tokenType] && isNotNull (QualifiedExprNavigator.GetByQualifier(context))

    | _ ->

    let binaryApp = BinaryAppExprNavigator.GetByLeftArgument(context)
    if isNull binaryApp then checkPrecedence context expr else

    if lastBlockHasSameIndent expr then true else

    let operator = binaryApp.Operator
    if isNotNull operator && context.Indent = operator.Indent then false else

    let rightArgument = binaryApp.RightArgument
    if isNotNull rightArgument && context.Indent = rightArgument.Indent then false else

    precedence binaryApp.LeftArgument < precedence binaryApp


let addParens (expr: IFSharpExpression) =
    let exprCopy = expr.Copy()
    let factory = expr.CreateElementFactory()

    let parenExpr = factory.CreateParenExpr()
    let parenExpr = ModificationUtil.ReplaceChild(expr, parenExpr)
    let expr = parenExpr.SetInnerExpression(exprCopy)

    shiftNode 1 expr
    expr


let addParensIfNeeded (expr: IFSharpExpression) =
    let context = expr.IgnoreParentParens()
    if context != expr || not (needsParens context expr) then expr else
    addParens expr
