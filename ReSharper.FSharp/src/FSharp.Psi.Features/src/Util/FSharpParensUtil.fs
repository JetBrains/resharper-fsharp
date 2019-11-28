[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpParensUtil

open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree

let needsParens (expr: ISynExpr) =
    match expr with
    | :? IReferenceExpr | :? IIndexerExpr 
    | :? IParenExpr | :? IQuoteExpr
    | :? IConstExpr | :? INullExpr
    | :? IRecordExpr | :? IAnonRecdExpr
    | :? IArrayOrListExpr | :? IArrayOrListOfSeqExpr
    | :? IObjExpr | :? ICompExpr
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
