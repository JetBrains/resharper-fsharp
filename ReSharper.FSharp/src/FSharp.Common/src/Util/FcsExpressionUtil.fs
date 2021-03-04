[<Extension>]
module JetBrains.ReSharper.Plugins.FSharp.Util.FcsExpressionUtil

open System
open FSharp.Compiler.Syntax

[<Extension; CompiledName("IsSimpleValueExpression")>]
let rec isSimpleValueExpression (synExpr: SynExpr) =
    match synExpr with
    | SynExpr.Paren(expr = expr) -> isSimpleValueExpression expr
    | SynExpr.Const _ | SynExpr.Quote _
    | SynExpr.ArrayOrList _ | SynExpr.ArrayOrListOfSeqExpr _
    | SynExpr.ObjExpr _ | SynExpr.New _
    | SynExpr.Record _ | SynExpr.AnonRecd _
    | SynExpr.Do _ | SynExpr.Lazy _ | SynExpr.TypeTest _ 
    | SynExpr.For _ | SynExpr.ForEach _ | SynExpr.While _ -> true
    | _ -> false

[<Extension; CompiledName("IsLiteralExpression")>]
let rec isConstExpression (synExpr: SynExpr) =
    match synExpr with
    | SynExpr.Const(synConst, _) ->
        match synConst with
        | SynConst.Unit | SynConst.Bytes _ | SynConst.Measure _ -> false
        | _ -> true
    | _ -> false

[<CompiledName("IsLiteralExpressionFunc")>]
let isConstExpressionFunc = Func<_,_>(isConstExpression)

[<CompiledName("IsSimpleValueExpressionFunc")>]
let isSimpleValueExpressionFunc = Func<_,_>(isSimpleValueExpression)
