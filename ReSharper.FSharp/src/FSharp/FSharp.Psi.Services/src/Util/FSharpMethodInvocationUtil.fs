module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpMethodInvocationUtil

open System.Linq
open FSharp.Compiler.Symbols
open JetBrains.Diagnostics
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util
open JetBrains.ReSharper.Psi

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

let getArgsOwner (expr: IFSharpExpression) =
    let tupleExpr = TupleExprNavigator.GetByExpression(expr.IgnoreParentParens())
    let exprContext = if isNull tupleExpr then expr else tupleExpr :> _
    FSharpArgumentOwnerNavigator.GetByArgumentExpression(exprContext.IgnoreParentParens())

let getReferenceName (fsArgsOwner: IFSharpArgumentsOwner) =
    let identifier =
        match fsArgsOwner with
        | :? IFSharpReferenceOwner as refOwner -> refOwner.FSharpIdentifier

        | :? IPrefixAppExpr as prefixAppExpr ->
            let invokedRefExpr = prefixAppExpr.InvokedReferenceExpression
            if isNull invokedRefExpr then null else invokedRefExpr.Identifier

        | _ -> null

    if isNull identifier then null else identifier.Name

let getReference (fsArgsOwner: IFSharpArgumentsOwner) =
    match fsArgsOwner with
    | :? IFSharpReferenceOwner as refOwner -> refOwner.Reference
    | :? IPrefixAppExpr as prefixAppExpr -> prefixAppExpr.InvokedFunctionReference
    | _ -> null

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

        let paramIndex =
            let invokingExtensionMethod =
                match fcsSymbol with
                | :? FSharpMemberOrFunctionOrValue as mfv ->
                    mfv.IsExtensionMember && mfv.ApparentEnclosingEntity <> mfv.DeclaringEntity
                | _ -> false

            let offset = if invokingExtensionMethod then 1 else 0
            argIndex + offset

        paramOwner.Parameters.ElementAtOrDefault(paramIndex)

let getFunExprAndPosition (expr: IFSharpExpression) =
    let expr = expr.NotNull().IgnoreParentParens()
    let tupleExpr = TupleExprNavigator.GetByExpression(expr)
    let arg, positionInGroup =
        if isNull tupleExpr then expr, 0 else tupleExpr.IgnoreParentParens(), tupleExpr.Expressions.IndexOf(expr)

    let prefixAppExpr = PrefixAppExprNavigator.GetByArgumentExpression(arg)
    if isNull prefixAppExpr then None else

    let rec loop curriedGroupPosition (appExpr: IPrefixAppExpr) =
        match appExpr.FunctionExpression.IgnoreInnerParens() with
        | :? IPrefixAppExpr as nestedPrefixAppExpr -> loop (curriedGroupPosition + 1) nestedPrefixAppExpr
        | funExpr -> Some(funExpr.IgnoreParentParens(), (curriedGroupPosition, positionInGroup), prefixAppExpr)

    loop 0 prefixAppExpr

[<Language(typeof<FSharpLanguage>)>]
type FSharpMethodInvocationUtil() =
    interface IFSharpMethodInvocationUtil with
        member x.GetMatchingParameter(expr) = getMatchingParameter expr
        member x.GetNamedArg(expr) = tryGetNamedArg expr
