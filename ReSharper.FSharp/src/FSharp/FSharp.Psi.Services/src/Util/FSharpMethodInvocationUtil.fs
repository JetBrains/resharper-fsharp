module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpMethodInvocationUtil

open System
open System.Linq
open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Tree
open JetBrains.Util

let isNamedArgReference (expr: IFSharpExpression) =
    let refExpr = FSharpArgumentsUtil.TryGetNamedArgRefExpr(expr)
    if isNull refExpr then false else

    match refExpr.Reference.GetFcsSymbol() with
    | :? FSharpParameter -> true
    | :? FSharpField as fsField -> fsField.IsUnionCaseField
    | _ -> false

let tryGetNamedArg (expr: IFSharpExpression) =
    match FSharpArgumentsUtil.TryGetNamedArgRefExpr(expr) with
    | null -> null
    | refExpr -> refExpr.Reference.Resolve().DeclaredElement.As<IParameter>()

let private getPipeAppByPipedExpr (expr: IFSharpExpression): IBinaryAppExpr =
    let binaryApp = BinaryAppExprNavigator.GetByLeftArgument(expr)
    if isPipeOpApp binaryApp then binaryApp else

    let binaryApp = BinaryAppExprNavigator.GetByRightArgument(expr)
    if isBackwardPipeOpApp binaryApp then binaryApp else

    null

let private getArgsOwnerByArgGroup (argGroupExpr: IFSharpExpression): IFSharpArgumentsOwner =
    let argsOwner = FSharpArgumentOwnerNavigator.GetByArgumentExpression(argGroupExpr)
    if isNotNull argsOwner then argsOwner else

    let pipeApp = getPipeAppByPipedExpr argGroupExpr
    if isNotNull pipeApp then pipeApp else

    let tupleExpr = TupleExprNavigator.GetByExpression(argGroupExpr)
    let tuplePipeApp = if isNull tupleExpr then null else getPipeAppByPipedExpr (tupleExpr.IgnoreParentParens())
    if isTuplePipeOpApp tuplePipeApp then tuplePipeApp else

    null

let getArgsOwner (expr: IFSharpExpression) =
    let tupleExpr = TupleExprNavigator.GetByExpression(expr.IgnoreParentParens())
    let exprContext = if isNull tupleExpr then expr else tupleExpr :> _
    getArgsOwnerByArgGroup (exprContext.IgnoreParentParens())

let private getAppliedExprs (appExpr: IAppExpr) (invokedExpr: IFSharpExpression) =
    let isSpreadTupleValue (arg: IFSharpExpression) =
        let pipeApp = BinaryAppExprNavigator.GetByArgument(arg)
        isTuplePipeOpApp pipeApp

    getPrefixAppExprArgs invokedExpr
    |> Seq.takeWhile (fun arg -> appExpr.Contains(arg) && not (isSpreadTupleValue arg))

let private getAppParameterArgs (appExpr: IAppExpr) =
    let invokedRefExpr = getInvokedReferenceExpr appExpr
    if isNull invokedRefExpr then EmptyList<IArgument>.InstanceList else

    let appliedExprs = getAppliedExprs appExpr invokedRefExpr
    invokedRefExpr.CalculateParameterArguments(appliedExprs)

let getParameterArgs (argsOwner: IFSharpArgumentsOwner) =
    match argsOwner with
    | :? IAppExpr as appExpr -> getAppParameterArgs appExpr
    | :? INewExpr as newExpr -> newExpr.CalculateParameterArguments([newExpr.ArgumentExpression])
    | :? IAttribute as attribute -> attribute.CalculateParameterArguments([attribute.Expression])
    | _ -> EmptyList<IArgument>.InstanceList

let getReferenceName (fsArgsOwner: IFSharpArgumentsOwner) =
    let identifier =
        match fsArgsOwner with
        | :? IFSharpReferenceOwner as refOwner -> refOwner.NameIdentifier

        | :? IAppExpr as appExpr ->
            let invokedRefExpr = getInvokedReferenceExpr appExpr
            if isNull invokedRefExpr then null else invokedRefExpr.Identifier

        | _ -> null

    if isNull identifier then null else identifier.Name

let getReference (fsArgsOwner: IFSharpArgumentsOwner) =
    match fsArgsOwner with
    | :? IFSharpReferenceOwner as refOwner -> refOwner.Reference

    | :? IAppExpr as appExpr ->
        let invokedRefExpr = getInvokedReferenceExpr appExpr
        if isNull invokedRefExpr then null else

        let appliedExprsCount = getAppliedExprs appExpr invokedRefExpr |> Seq.length
        invokedRefExpr.GetInvokedFunctionReference(appliedExprsCount)

    | _ -> null

let getExtensionMethodParameterOffset (fcsSymbol: FSharpSymbol) =
    match fcsSymbol with
    | :? FSharpMemberOrFunctionOrValue as mfv when
            mfv.IsExtensionMember && mfv.ApparentEnclosingEntity <> mfv.DeclaringEntity -> 1
    | _ -> 0

let getMatchingParameter (expr: IFSharpExpression) =
    let argsOwner = getArgsOwner expr
    if isNull argsOwner then null else

    let symbolReference = getReference argsOwner
    if isNull symbolReference then null else

    let fcsSymbol = symbolReference.GetFcsSymbol()
    if not (fcsSymbol :? FSharpMemberOrFunctionOrValue) && not (fcsSymbol :? FSharpUnionCase) then null else

    let namedArgRefExpr =
        match fcsSymbol with
        | :? FSharpMemberOrFunctionOrValue as mfv when
            mfv.IsMember && FSharpArgumentsUtil.IsTopLevelArg(expr) ->
            FSharpArgumentsUtil.TryGetNamedArgRefExpr(expr)
        | _ -> null

    let namedParam =
        match namedArgRefExpr with
        | null -> null
        | namedRef -> namedRef.Reference.Resolve().DeclaredElement.As<IParameter>()

    if isNotNull namedParam then namedParam else

    let paramOwner = symbolReference.Resolve().DeclaredElement.As<IParametersOwner>()
    if isNull paramOwner then null else

    if isNotNull namedArgRefExpr then
        // If this is a named argument, but FCS couldn't match it, try matching ourselves by name
        paramOwner.Parameters
        |> Seq.tryFind (fun param -> param.ShortName = namedArgRefExpr.Reference.GetName())
        |> Option.defaultValue null
    else
        match Seq.tryFindIndex expr.Equals argsOwner.ParameterArguments with
        | None -> null
        | Some argIndex ->

        let paramIndex = argIndex + getExtensionMethodParameterOffset fcsSymbol
        paramOwner.Parameters.ElementAtOrDefault(paramIndex)

let private tryGetArgIndex (expr: IFSharpExpression) =
    let namedArgExpr = BinaryAppExprNavigator.GetByRightArgument(expr)
    let isNamedArg = isNamedArgReference namedArgExpr
    let namedArgName = if isNamedArg then FSharpArgumentsUtil.TryGetNamedArgName(namedArgExpr) else null

    let argExpr: IFSharpExpression = if isNamedArg then namedArgExpr else expr.IgnoreParentParens()
    let tupleExpr = TupleExprNavigator.GetByExpression(argExpr)
    let argGroupExpr: IFSharpExpression = if isNull tupleExpr then argExpr else tupleExpr

    let argGroupExpr: IFSharpExpression =
        if isNamedArg then ParenExprNavigator.GetByInnerExpression(argGroupExpr) else argGroupExpr.IgnoreParentParens()

    let indexInGroup =
        if isNamedArg || isNull tupleExpr then Nullable() else Nullable(tupleExpr.Expressions.IndexOf(argExpr))

    let argsOwner = getArgsOwnerByArgGroup argGroupExpr
    match argsOwner with
    | null -> None

    | :? IAppExpr as appExpr ->
        let invokedExpr = getInvokedExpr appExpr
        let appliedExprs = getAppliedExprs appExpr invokedExpr |> Array.ofSeq

        let groupIndex = Array.IndexOf(appliedExprs, argGroupExpr)
        if groupIndex >= 0 then Some(argsOwner, FSharpParameterIndex(groupIndex, indexInGroup, namedArgName)) else

        let tuplePipeGroupIndex = Array.IndexOf(appliedExprs, argExpr)
        if tuplePipeGroupIndex >= 0 then Some(argsOwner, FSharpParameterIndex(tuplePipeGroupIndex)) else

        None

    | _ -> Some(argsOwner, FSharpParameterIndex(0, indexInGroup, namedArgName))

let tryGetParameterIndex (node: IFSharpTypeOwnerNode) =
    match node with
    | :? IFSharpExpression as expr -> tryGetArgIndex expr

    | :? IFSharpPattern as pat ->
        let mutable argsOwner = null
        let parameterIndex = FSharpArgumentsOwnerUtil.TryGetFSharpParameterIndex(pat, &argsOwner)
        if parameterIndex.HasValue then Some(argsOwner, parameterIndex.Value) else None

    | _ -> None

[<Language(typeof<FSharpLanguage>)>]
type FSharpMethodInvocationUtil() =
    interface IFSharpMethodInvocationUtil with
        member x.GetMatchingParameter(expr) = getMatchingParameter expr
        member x.GetNamedArg(expr) = tryGetNamedArg expr
        member x.GetParameterArguments(argsOwner) = getParameterArgs argsOwner
