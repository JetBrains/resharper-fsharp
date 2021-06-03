module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpMethodInvocationUtil

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI

let tryGetNamedArgRefExpr (expr: IFSharpExpression) =
    let binaryAppExpr = expr.As<IBinaryAppExpr>()
    if isNull binaryAppExpr then null else

    let operatorExpr = binaryAppExpr.Operator
    if isNull operatorExpr || operatorExpr.Reference.GetName() <> "=" then null else

    binaryAppExpr.LeftArgument.As<IReferenceExpr>()


let isNamedArgReference (expr: IFSharpExpression) =
    let refExpr = tryGetNamedArgRefExpr expr
    if isNull refExpr then false else

    match refExpr.Reference.GetFSharpSymbol() with
    | :? FSharpParameter -> true
    | :? FSharpField as fsField -> fsField.IsUnionCaseField
    | _ -> false


let tryGetNamedArg (expr: IFSharpExpression) =
    match tryGetNamedArgRefExpr expr with
    | null -> null
    | refExpr -> refExpr.Reference.Resolve().DeclaredElement.As<IParameter>()

let getArgsOwner (expr: IFSharpExpression) =
    let tupleExpr = TupleExprNavigator.GetByExpression(expr.IgnoreParentParens())
    let exprContext = if isNull tupleExpr then expr else tupleExpr :> _
    FSharpArgumentOwnerNavigator.GetByArgumentExpression(exprContext.IgnoreParentParens())

let getReference (fsArgsOwner: IFSharpArgumentsOwner) =
    match fsArgsOwner with
    | :? IFSharpReferenceOwner as refOwner -> refOwner.Reference
    | :? IPrefixAppExpr as prefixAppExpr -> prefixAppExpr.InvokedFunctionReference
    | _ -> null

let getMatchingParameter (expr: IFSharpExpression) =
    let argsOwner = getArgsOwner expr
    if isNull argsOwner then null else

    let namedArgRefExpr = tryGetNamedArgRefExpr expr

    let namedParam =
        match namedArgRefExpr with
        | null -> null
        | namedRef -> namedRef.Reference.Resolve().DeclaredElement.As<IParameter>()

    if isNotNull namedParam then namedParam else

    let symbolReference = getReference argsOwner
    if isNull symbolReference then null else

    let mfv = symbolReference.GetFSharpSymbol().As<FSharpMemberOrFunctionOrValue>()
    if isNull mfv then null else

    let paramOwner = symbolReference.Resolve().DeclaredElement.As<IParametersOwner>()
    if isNull paramOwner then null else

    let param =
        let parameters = paramOwner.Parameters
        if isNotNull namedArgRefExpr then
            // If this is a named argument, but FCS couldn't match it, try matching ourselves by name
            paramOwner.Parameters
            |> Seq.tryFind (fun param -> param.ShortName = namedArgRefExpr.Reference.GetName())
        else
            match Seq.tryFindIndex expr.Equals argsOwner.ParameterArguments with
            | None -> None
            | Some paramIndex ->

            let invokingExtensionMethod =
                mfv.IsExtensionMember && Some mfv.ApparentEnclosingEntity <> mfv.DeclaringEntity

            let offset = if invokingExtensionMethod then 1 else 0
            let paramIndex = paramIndex + offset
            if paramIndex < parameters.Count then
                Some(parameters.[paramIndex])
            else
                None

    // Skip unnamed parameters
    match param with
    | Some param when param.ShortName <> SharedImplUtil.MISSING_DECLARATION_NAME -> param
    | _ -> null

[<Language(typeof<FSharpLanguage>)>]
type FSharpMethodInvocationUtil() =
    interface IFSharpMethodInvocationUtil with
        member x.GetMatchingParameter(expr) = getMatchingParameter expr
        member x.GetNamedArg(expr) = tryGetNamedArg expr
