[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpExpressionUtil

open JetBrains.Diagnostics
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree

let isPredefinedFunctionRef name (expr: ISynExpr) =
    let refExpr = expr.IgnoreInnerParens().As<IReferenceExpr>()
    if isNull refExpr then false else

    Assertion.Assert(predefinedFunctionTypes.ContainsKey(name), "Predefined function is not added to map: {0}", name)

    let exprReference = refExpr.Reference
    if exprReference.GetName() <> name then false else

    let declaredElement = exprReference.Resolve().DeclaredElement.As<IFunction>()
    if isNull declaredElement then false else

    let containingType = declaredElement.GetContainingType()
    isNotNull containingType && containingType.GetClrName() = predefinedFunctionTypes.[name]

let inline isPredefinedInfixOpApp name (binaryAppExpr: IBinaryAppExpr) =
    if isNull binaryAppExpr then false else
    isPredefinedFunctionRef name binaryAppExpr.Operator

let inline isPredefinedFuctionApp name (expr: ISynExpr) (arg: outref<ISynExpr>) =
    match expr with
    | :? IPrefixAppExpr as prefixApp when
            isPredefinedFunctionRef name prefixApp.FunctionExpression ->
        arg <- prefixApp.ArgumentExpression
        true

    | :? IBinaryAppExpr as binaryApp when
            isPredefinedInfixOpApp "|>" binaryApp &&
            isPredefinedFunctionRef name binaryApp.RightArgument ->
        arg <- binaryApp.LeftArgument
        true

    | :? IBinaryAppExpr as binaryApp when
            isPredefinedInfixOpApp "<|" binaryApp &&
            isPredefinedFunctionRef name binaryApp.LeftArgument ->
        arg <- binaryApp.RightArgument
        true

    | _ -> false

let rec createLogicallyNegatedExpression (expr: ISynExpr): ISynExpr =
    if isNull expr then null else

    let expr = expr.IgnoreInnerParens()
    let factory = expr.CreateElementFactory()

    let mutable arg = Unchecked.defaultof<_>
    if isPredefinedFuctionApp "not" expr &arg && isNotNull arg then
        arg.IgnoreInnerParens().Copy() else

    let binaryApp = expr.As<IBinaryAppExpr>()
    if isPredefinedInfixOpApp "||" binaryApp then
        let arg1 = createLogicallyNegatedExpression binaryApp.LeftArgument
        let arg2 = createLogicallyNegatedExpression binaryApp.RightArgument
        factory.CreateBinaryAppExpr("&&", arg1, arg2) else

    if isPredefinedInfixOpApp "&&" binaryApp then
        let arg1 = createLogicallyNegatedExpression binaryApp.LeftArgument
        let arg2 = createLogicallyNegatedExpression binaryApp.RightArgument
        factory.CreateBinaryAppExpr("||", arg1, arg2) else

    let literalExpr = expr.As<ILiteralExpr>()
    let literalTokenType = if isNotNull literalExpr then getTokenType literalExpr.Literal else null

    if literalTokenType == FSharpTokenType.FALSE then
        factory.CreateExpr("true") else

    if literalTokenType == FSharpTokenType.TRUE then
        factory.CreateExpr("false") else

    factory.CreateAppExpr("not", expr) :> _
