[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpExpressionUtil

open JetBrains.Diagnostics
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.ExtensionsAPI.Tree
open JetBrains.ReSharper.Psi.Tree

let isPredefinedFunctionRef name (expr: IFSharpExpression) =
    let refExpr = expr.IgnoreInnerParens().As<IReferenceExpr>()
    if isNull refExpr then false else

    Assertion.Assert(predefinedFunctionTypes.ContainsKey(name), "Predefined function is not added to map: {0}", name)

    let exprReference = refExpr.Reference
    if exprReference.GetName() <> name then false else

    let declaredElement = exprReference.Resolve().DeclaredElement.As<IFunction>()
    if isNull declaredElement then false else

    let containingType = declaredElement.GetContainingType()
    isNotNull containingType && containingType.GetClrName() = predefinedFunctionTypes.[name]

let isPredefinedInfixOpApp name (binaryAppExpr: IBinaryAppExpr) =
    if isNull binaryAppExpr then false else
    isPredefinedFunctionRef name binaryAppExpr.Operator

let isPredefinedFunctionApp name (expr: #IFSharpExpression) (arg: outref<IFSharpExpression>) =
    match expr :> IFSharpExpression with
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


let getPossibleFunctionAppReferenceExpr (expr: IFSharpExpression) =
    match expr with
    | :? IPrefixAppExpr as prefixApp -> prefixApp.FunctionExpression
    | :? IBinaryAppExpr as binaryApp ->
        let refExpr = binaryApp.FunctionExpression.As<IReferenceExpr>()
        if isNull refExpr then null else

        match refExpr.ShortName with
        | "|>" -> binaryApp.RightArgument
        | "<|" -> binaryApp.LeftArgument
        | _ -> null
    | _ -> null

let getPossibleFunctionAppName (expr: IFSharpExpression) =
    let expr = getPossibleFunctionAppReferenceExpr expr
    match expr.IgnoreInnerParens() with
    | :? IReferenceExpr as refExpr -> refExpr.ShortName
    | _ -> SharedImplUtil.MISSING_DECLARATION_NAME


let rec createLogicallyNegatedExpression (expr: IFSharpExpression): IFSharpExpression =
    if isNull expr then null else

    let expr = expr.IgnoreInnerParens()
    let lineEnding = expr.GetLineEnding()
    let factory = expr.CreateElementFactory()

    let mutable arg = Unchecked.defaultof<_>
    if isPredefinedFunctionApp "not" expr &arg && isNotNull arg then
        arg.IgnoreInnerParens().Copy() else

    let binaryApp = expr.As<IBinaryAppExpr>()

    let replaceBinaryApp nameTo negateArgs: IFSharpExpression =
        let arg1 = binaryApp.LeftArgument
        let arg2 = binaryApp.RightArgument

        let arg1 = if negateArgs then createLogicallyNegatedExpression arg1 else arg1
        let arg2 = if negateArgs then createLogicallyNegatedExpression arg2 else arg2

        let newBinaryApp = factory.CreateBinaryAppExpr(nameTo, arg1, arg2) :?> IBinaryAppExpr
        if binaryApp.LeftArgument.EndLine <> binaryApp.RightArgument.StartLine then
            moveToNewLine lineEnding binaryApp.LeftArgument.Indent newBinaryApp.RightArgument

        newBinaryApp :> _

    if isPredefinedInfixOpApp "||" binaryApp then
        replaceBinaryApp "&&" true else

    if isPredefinedInfixOpApp "&&" binaryApp then
        replaceBinaryApp "||" true else

    if isPredefinedInfixOpApp "<>" binaryApp then
        replaceBinaryApp "=" false else

    if isPredefinedInfixOpApp "=" binaryApp then
        replaceBinaryApp "<>" false else

    let literalExpr = expr.As<ILiteralExpr>()
    let literalTokenType = if isNotNull literalExpr then getTokenType literalExpr.Literal else null

    if literalTokenType == FSharpTokenType.FALSE then
        factory.CreateExpr("true") else

    if literalTokenType == FSharpTokenType.TRUE then
        factory.CreateExpr("false") else

    factory.CreateAppExpr("not", expr) :> _

let setBindingExpression (expr: IFSharpExpression) contextIndent (letBindings: #ILetBindings) =
    let newExpr = letBindings.Bindings.[0].SetExpression(expr.Copy())
    if not expr.IsSingleLine then
        let indentSize = expr.GetIndentSize()
        ModificationUtil.AddChildBefore(newExpr, NewLine(expr.GetLineEnding())) |> ignore
        ModificationUtil.AddChildBefore(newExpr, Whitespace(contextIndent + indentSize)) |> ignore
        shiftNode indentSize newExpr
