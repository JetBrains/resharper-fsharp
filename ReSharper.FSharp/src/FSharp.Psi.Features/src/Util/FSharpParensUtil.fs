[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpParensUtil

open System
open FSharp.Compiler
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree

let deindentsBody (expr: ISynExpr) =
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

let (|Prefix|_|) (other: string) (str: string) =
    if str.StartsWith(other, StringComparison.Ordinal) then someUnit else None

let precedence (expr: ISynExpr): int =
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
        | Prefix "!=" | Prefix "<" | Prefix ">" | Prefix "|" | Prefix "&" | "$" | "=" -> 3
        | Prefix "^" -> 4
        | Prefix "::" -> 5
        | Prefix "+" | Prefix "-" -> 6
        | Prefix "*" | Prefix "/" | Prefix "%" -> 7
        | Prefix "**" -> 8
        | _ -> 0

    | :? IPrefixAppExpr -> 9
    | _ -> 0


let isHighPrecedenceApp (appExpr: IPrefixAppExpr) =
    if isNull appExpr then false else

    let funExpr = appExpr.FunctionExpression
    let argExpr = appExpr.ArgumentExpression
    if isNull funExpr || isNull argExpr then false else

    let funEndOffset = funExpr.GetTreeEndOffset()
    let argStartOffset = argExpr.GetTreeStartOffset()
    funEndOffset = argStartOffset

let private canBeTopLevelArgInHighPrecedenceApp (expr: ISynExpr) =
    expr :? IArrayOrListExpr || expr :? IArrayOrListOfSeqExpr ||
    expr :? IObjExpr || expr :? IRecordExpr

let rec private isHighPrecedenceAppRequired (appExpr: IPrefixAppExpr) =
    let argExpr = appExpr.ArgumentExpression.IgnoreInnerParens()
    if canBeTopLevelArgInHighPrecedenceApp argExpr then false else

    if isNotNull (QualifiedExprNavigator.GetByQualifier(appExpr)) then true else

    false

let rec needsParens (expr: ISynExpr) =
    if isNull expr then false else

    let context = expr.IgnoreParentParens()
    if context.Parent :? IChameleonExpression then false else

    let appExpr = PrefixAppExprNavigator.GetByExpression(context)
    if isHighPrecedenceApp appExpr && isHighPrecedenceAppRequired appExpr then true else

    match expr with
    | :? IQualifiedExpr as qualifiedExpr ->
        needsParens qualifiedExpr.Qualifier

    | :? IParenExpr | :? IQuoteExpr
    | :? IConstExpr | :? INullExpr
    | :? IRecordExpr | :? IAnonRecdExpr
    | :? IArrayOrListExpr | :? IArrayOrListOfSeqExpr
    | :? IObjExpr | :? IComputationLikeExpr
    | :? IAddressOfExpr -> false

    | :? IBinaryAppExpr as binaryAppExpr when
            // todo: app expr (and check prefix too)
            let outerApp = AppExprNavigator.GetByArgument(context)
            isNotNull outerApp && precedence outerApp < precedence binaryAppExpr ->
        false

    | :? IBinaryAppExpr as binaryAppExpr when
            let outerApp = AppExprNavigator.GetByRightArgument(context)
            isNotNull outerApp && precedence outerApp <= precedence binaryAppExpr ->
        true

    | :? IAppExpr when
            // todo: for each
            isNotNull (BinaryAppExprNavigator.GetByArgument(context)) ||
            isNotNull (ConditionOwnerExprNavigator.GetByExpr(context)) ->
        false

    | :? IIfThenElseExpr when
            isNotNull (IfThenElseExprNavigator.GetByElseExpr(context)) ->
        false

    | _ ->

    let binaryApp = BinaryAppExprNavigator.GetByLeftArgument(context)
    if isNull binaryApp then true else

    if deindentsBody expr then true else

    let operator = binaryApp.Operator
    if isNotNull operator && context.Indent = operator.Indent then false else

    let rightArgument = binaryApp.RightArgument
    if isNotNull rightArgument && context.Indent = rightArgument.Indent then false else

    true


let addParens (expr: ISynExpr) =
    let exprCopy = expr.Copy()
    let factory = expr.CreateElementFactory()

    let parenExpr = factory.CreateParenExpr()
    let parenExpr = ModificationUtil.ReplaceChild(expr, parenExpr)
    let expr = parenExpr.SetInnerExpression(exprCopy)

    shiftExpr 1 expr
    expr


let addParensIfNeeded (expr: ISynExpr) =
    if not (needsParens expr) then expr else
    addParens expr
