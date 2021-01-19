[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpParensUtil

open System
open FSharp.Compiler
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree

let deindentsBody (expr: IFSharpExpression) =
    match expr with
    | :? IMatchClauseListOwner as matchExpr ->
        if expr.IsSingleLine then false else

        let clause = matchExpr.Clauses.LastOrDefault()
        if isNull clause then false else

        let clauseExpr = clause.Expression
        isNotNull clauseExpr && clauseExpr.Indent = expr.Indent

    | :? IIfExpr as ifExpr ->
        if expr.IsSingleLine then false else

        let elseExpr = ifExpr.ElseExpr
        isNotNull elseExpr && elseExpr.Indent = expr.Indent

    | _ -> false

let contextExprRequiresParens (expr: IFSharpExpression) =
    isNotNull (TypeInheritNavigator.GetByCtorArgExpression(expr)) ||
    isNotNull (ObjExprNavigator.GetByArgExpression(expr)) ||
    isNotNull (NewExprNavigator.GetByArgumentExpression(expr))

let isTopLevelContextExpr (expr: IFSharpExpression) =
    if expr.Parent :? IChameleonExpression && isNull (AttributeNavigator.GetByExpression(expr)) then true else

    if isNotNull (ParenExprNavigator.GetByInnerExpression(expr)) then
        true else

    if isNotNull (LetOrUseExprNavigator.GetByBinding(LocalBindingNavigator.GetByExpression(expr))) then
        true else

    if isNotNull (LetOrUseExprNavigator.GetByInExpression(expr)) then
        true else

    if isNotNull (MatchClauseNavigator.GetByExpression(expr)) ||
            isNotNull (MatchClauseNavigator.GetByWhenExpression(WhenExprClauseNavigator.GetByExpression(expr))) then
        true else

    if isNotNull (WhileExprNavigator.GetByExpression(expr)) then
        true else

    false

let (|Prefix|_|) (other: string) (str: string) =
    if str.StartsWith(other, StringComparison.Ordinal) then someUnit else None

let precedence (expr: IFSharpExpression): int =
    match expr with
    | :? IBinaryAppExpr as binaryApp ->
        let refExpr = binaryApp.Operator
        if isNull refExpr then 0 else

        // todo: fix op tokens in references
        let name = PrettyNaming.DecompileOpName (refExpr.GetText())
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

    | :? ICastExpr -> 3
    | :? ITypeTestExpr -> 7
    | :? IPrefixAppExpr | :? IDoLikeExpr -> 11
    | :? IParenExpr -> 12

    | _ -> 0


let isHighPrecedenceApp (appExpr: IPrefixAppExpr) =
    if isNull appExpr then false else

    let funExpr = appExpr.FunctionExpression
    let argExpr = appExpr.ArgumentExpression
    if isNull funExpr || isNull argExpr then false else

    let funEndOffset = funExpr.GetTreeEndOffset()
    let argStartOffset = argExpr.GetTreeStartOffset()
    funEndOffset = argStartOffset

let private canBeTopLevelArgInHighPrecedenceApp (expr: IFSharpExpression) =
    // todo: check `ignore{| Field = 1 + 1 |}.Field` vs `ignore[].Head` 
    expr :? IArrayOrListExpr || expr :? IObjExpr || expr :? IRecordLikeExpr

let rec private isHighPrecedenceAppRequired (appExpr: IPrefixAppExpr) =
    let argExpr = appExpr.ArgumentExpression.IgnoreInnerParens()
    if canBeTopLevelArgInHighPrecedenceApp argExpr then false else

    if isNotNull (QualifiedExprNavigator.GetByQualifier(appExpr)) then true else

    false

let rec needsParens (context: IFSharpExpression) (expr: IFSharpExpression) =
    if isNull expr then false else

    let context = if isNotNull context then context else expr.IgnoreParentParens()

    if contextExprRequiresParens context then true else
    if isTopLevelContextExpr context then false else

    let appExpr = PrefixAppExprNavigator.GetByExpression(context)
    if isHighPrecedenceApp appExpr && isHighPrecedenceAppRequired appExpr then true else

    match expr with
    | :? IReferenceExpr as refExpr when
            let attr = AttributeNavigator.GetByExpression(refExpr.IgnoreParentParens())
            isNotNull attr && (isNotNull refExpr.TypeArgumentList || isNotNull attr.Target) ->
        true

    | :? IQualifiedExpr as qualifiedExpr ->
        needsParens context qualifiedExpr.Qualifier

    | :? IParenExpr | :? IQuoteExpr
    | :? IConstExpr | :? INullExpr
    | :? IRecordLikeExpr | :? IArrayOrListExpr | :? IComputationExpr
    | :? IObjExpr | :? IAddressOfExpr -> false

    | :? IBinaryAppExpr as binaryAppExpr when
            isNotNull (AppExprNavigator.GetByArgument(context)) ->
        let outerApp = AppExprNavigator.GetByArgument(context)
        precedence outerApp > precedence binaryAppExpr ||

        let outerApp = AppExprNavigator.GetByRightArgument(context)
        precedence outerApp = precedence binaryAppExpr

    | :? IAppExpr when
            // todo: for each
            isNotNull (BinaryAppExprNavigator.GetByArgument(context)) ->
        false

    | :? IIfThenElseExpr when
            isNotNull (IfThenElseExprNavigator.GetByElseExpr(context)) ->
        false

    | :? IAppExpr | :? ITypedLikeExpr | :? IDoLikeExpr when
            isNotNull (ConditionOwnerExprNavigator.GetByExpr(context)) ->
        false

    | _ ->

    let binaryApp = BinaryAppExprNavigator.GetByLeftArgument(context)
    if isNull binaryApp then true else

    if deindentsBody expr then true else

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
    if not (needsParens expr expr) then expr else
    addParens expr
