[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpParensUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree

let isHighPrecedenceApp (appExpr: IAppExpr) =
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

let rec private isHighPrecedenceAppRequired (appExpr: IAppExpr) =
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
    | _ -> true


let addParens (expr: ISynExpr) =
    let exprCopy = expr.Copy()
    let factory = expr.CreateElementFactory()

    let parenExpr = factory.CreateParenExpr()
    let parenExpr = ModificationUtil.ReplaceChild(expr, parenExpr)
    let expr = parenExpr.SetInnerExpression(exprCopy)

    if not expr.IsSingleLine then
        shiftExpr 1 expr
    expr


let addParensIfNeeded (expr: ISynExpr) =
    if not (needsParens expr) then expr else
    addParens expr
