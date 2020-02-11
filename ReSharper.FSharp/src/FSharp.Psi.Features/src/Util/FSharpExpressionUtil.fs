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

let isPredefinedFuctionApp name (appExpr: IAppExpr) =
    Assertion.Assert(predefinedFunctionTypes.ContainsKey(name), "Predefined function is not added: {0}", name)

    if isNull appExpr || isNull appExpr.ArgumentExpression then false else

    let refExpr = appExpr.FunctionExpression.As<IReferenceExpr>()
    if isNull refExpr then false else

    let exprReference = refExpr.Reference
    if exprReference.GetName() <> name then false else

    let declaredElement = exprReference.Resolve().DeclaredElement.As<IFunction>()
    if isNull declaredElement then false else

    let containingType = declaredElement.GetContainingType()
    isNotNull containingType && containingType.GetClrName() = predefinedFunctionTypes.[name]


let rec createLogicallyNegatedExpression (expr: ISynExpr): ISynExpr =
    if isNull expr then null else

    let factory = expr.CreateElementFactory()

    let parenExpr = expr.As<IParenExpr>()
    if isNotNull parenExpr then
        let negatedExpression = createLogicallyNegatedExpression parenExpr.InnerExpression
        factory.CreateParenExpr(negatedExpression.IgnoreInnerParens()) :> _ else

    let appExpr = expr.As<IPrefixAppExpr>()
    if isNotNull appExpr && isPredefinedFuctionApp "not" appExpr then
        // todo: check if parens are needed
        appExpr.ArgumentExpression.Copy() else

    let infixApp = if isNotNull appExpr then appExpr.FunctionExpression.As<IInfixAppExpr>() else null
    if isNotNull infixApp && isPredefinedFuctionApp "||" infixApp then
        let arg1 = createLogicallyNegatedExpression infixApp.ArgumentExpression
        let arg2 = createLogicallyNegatedExpression appExpr.ArgumentExpression
        factory.CreateBinaryAppExpr("&&", arg1, arg2) else

    if isNotNull infixApp && isPredefinedFuctionApp "&&" infixApp then
        let arg1 = createLogicallyNegatedExpression infixApp.ArgumentExpression
        let arg2 = createLogicallyNegatedExpression appExpr.ArgumentExpression
        factory.CreateBinaryAppExpr("||", arg1, arg2) else

    let literalExpr = expr.As<ILiteralExpr>()
    let literalTokenType = if isNotNull literalExpr then getTokenType literalExpr.Literal else null

    if literalTokenType == FSharpTokenType.FALSE then
        factory.CreateExpr("true") else

    if literalTokenType == FSharpTokenType.TRUE then
        factory.CreateExpr("false") else

    factory.CreateAppExpr("not", expr) :> _
