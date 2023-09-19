[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpExpressionUtil

open FSharp.Compiler.Syntax
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
    isNotNull containingType && containingType.GetClrName() = predefinedFunctionTypes[name]

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

let isFunctionInApp (expr: #IFSharpExpression) (funExpr: outref<IAppExpr>) (arg: outref<IFSharpExpression>) =
    let prefixAppExpr = PrefixAppExprNavigator.GetByFunctionExpression(expr)
    if isNotNull prefixAppExpr then
        funExpr <- prefixAppExpr
        arg <- prefixAppExpr.ArgumentExpression
        true else

    let binaryAppExpr = BinaryAppExprNavigator.GetByRightArgument(expr)
    if isNotNull binaryAppExpr && isPredefinedInfixOpApp "|>" binaryAppExpr then
        funExpr <- binaryAppExpr
        arg <- binaryAppExpr.LeftArgument
        true else

    let binaryAppExpr = BinaryAppExprNavigator.GetByLeftArgument(expr)
    if isNotNull binaryAppExpr && isPredefinedInfixOpApp "<|" binaryAppExpr then
        funExpr <- binaryAppExpr
        arg <- binaryAppExpr.RightArgument
        true else

    false

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

let setBindingExpression (expr: IFSharpExpression) contextIndent (binding: IBinding) =
    let newExpr = binding.SetExpression(expr.Copy())
    if not expr.IsSingleLine then
        let nextSibling = binding.EqualsToken.NextSibling
        if getTokenType nextSibling == FSharpTokenType.WHITESPACE then
            ModificationUtil.DeleteChild(nextSibling)

        let indentSize = expr.GetIndentSize()
        ModificationUtil.AddChildBefore(newExpr, NewLine(expr.GetLineEnding())) |> ignore
        ModificationUtil.AddChildBefore(newExpr, Whitespace(contextIndent + indentSize)) |> ignore
        shiftNode indentSize newExpr

let tryGetEffectiveParentComputationExpression (expr: IFSharpExpression) =
    let rec loop isLetInExpr (expr: IFSharpExpression) =
        let computationExpr = ComputationExprNavigator.GetByExpression(expr)
        let appExpr = PrefixAppExprNavigator.GetByArgumentExpression(computationExpr)
        if isNotNull appExpr then computationExpr, isLetInExpr else

        let letOrUseExpr = LetOrUseExprNavigator.GetByInExpression(expr)
        if isNotNull letOrUseExpr then loop true letOrUseExpr else

        let seqExpr = SequentialExprNavigator.GetByExpression(expr)
        if isNotNull seqExpr then loop isLetInExpr seqExpr else

        let matchExpr = MatchExprNavigator.GetByClauseExpression(expr)
        if isNotNull matchExpr then loop isLetInExpr matchExpr else

        let ifExpr = IfExprNavigator.GetByBranchExpression(expr)
        if isNotNull ifExpr then loop isLetInExpr ifExpr else

        let whileExpr = WhileExprNavigator.GetByDoExpression(expr)
        if isNotNull whileExpr then loop isLetInExpr whileExpr else

        let forExpr = ForExprNavigator.GetByDoExpression(expr)
        if isNotNull forExpr then loop isLetInExpr forExpr else

        let tryExpr = TryLikeExprNavigator.GetByTryExpression(expr)
        if isNotNull tryExpr then loop isLetInExpr tryExpr else

        let prefixAppExpr = PrefixAppExprNavigator.GetByFunctionExpression(expr)
        if isNotNull prefixAppExpr then loop isLetInExpr prefixAppExpr else

        let binaryAppExpr = BinaryAppExprNavigator.GetByLeftArgument(expr)
        if isNotNull binaryAppExpr then loop isLetInExpr binaryAppExpr else

        let referenceExpr = ReferenceExprNavigator.GetByQualifier(expr)
        if isNotNull referenceExpr then loop isLetInExpr referenceExpr else

        null, false

    loop false expr

let isInsideComputationExpressionForCustomOperation (expr: IFSharpExpression) =
    let rec loop (expr: IFSharpExpression) =
        let computationExpr = ComputationExprNavigator.GetByExpression(expr)
        let appExpr = PrefixAppExprNavigator.GetByArgumentExpression(computationExpr)
        if isNotNull appExpr && isNotNull computationExpr then true else

        let letOrUseExpr = LetOrUseExprNavigator.GetByInExpression(expr)
        // use is not allowed before custom operation
        if isNotNull letOrUseExpr && not letOrUseExpr.IsUse then loop letOrUseExpr else

        let seqExpr = SequentialExprNavigator.GetByExpression(expr)
        if isNotNull seqExpr then loop seqExpr else

        let forExpr = ForLikeExprNavigator.GetByDoExpression(expr)
        if isNotNull forExpr then loop forExpr else

        let prefixAppExpr = PrefixAppExprNavigator.GetByExpression(expr)
        if isNotNull prefixAppExpr then loop prefixAppExpr else

        false

    loop expr

let isOperatorReferenceExpr (expr: IFSharpExpression) =
    let refExpr = expr.As<IReferenceExpr>()
    isNotNull refExpr &&

    let name = refExpr.ShortName
    name <> SharedImplUtil.MISSING_DECLARATION_NAME &&
    PrettyNaming.IsOperatorDisplayName name
