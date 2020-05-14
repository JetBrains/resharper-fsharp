module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpMethodInvocationUtil

open FSharp.Compiler.SourceCodeServices
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree


let tryGetNamedRef (expr: IFSharpExpression) =
    let binaryAppExpr = expr.As<IBinaryAppExpr>()
    if isNull binaryAppExpr then None else

    if binaryAppExpr.Operator.Reference.GetName() <> "=" then None else

    match binaryAppExpr.LeftArgument with
    | :? IReferenceExpr as refExpr -> Some refExpr
    | _ -> None


let tryGetNamedArg (expr: IFSharpExpression) =
    match tryGetNamedRef expr with
    | None -> null
    | Some refExpr -> refExpr.Reference.Resolve().DeclaredElement.As<IParameter>()


let getArgumentsOwner (expr: IFSharpExpression) =
    let tupleExpr = TupleExprNavigator.GetByExpression(expr.IgnoreParentParens())
    let exprContext = if isNull tupleExpr then expr else tupleExpr :> _
    FSharpArgumentOwnerNavigator.GetByArgumentExpression(exprContext.IgnoreParentParens())


let getMatchingParameter (expr: IFSharpExpression) =
    let argsOwner = getArgumentsOwner expr
    if isNull argsOwner then null else

    let namedRefOpt = tryGetNamedRef expr
    let namedParam =
        match namedRefOpt with
        | None -> null
        | Some namedRef -> namedRef.Reference.Resolve().DeclaredElement.As<IParameter>()
    if isNotNull namedParam then namedParam else

    let symbolReference = argsOwner.Reference
    if isNull symbolReference then null else

    let mfv =
        symbolReference.TryGetFSharpSymbol()
        |> Option.bind (function
            | :? FSharpMemberOrFunctionOrValue as mfv -> Some mfv
            | _ -> None)

    match mfv with
    | None -> null
    | Some mfv ->

    let paramOwner = symbolReference.Resolve().DeclaredElement.As<IParametersOwner>()
    if isNull paramOwner then null else

    let param =
        match namedRefOpt with
        | Some namedRef ->
            // If this is a named argument, but FCS couldn't match it, try matching ourselves by name
            paramOwner.Parameters
            |> Seq.tryFind (fun param -> param.ShortName = namedRef.Reference.GetName())
        | None ->

        let args = argsOwner.ParameterArguments

        match args |> Seq.tryFindIndex (fun argExpr -> expr.Equals(argExpr)) with
        | None -> None
        | Some paramIndex ->

        let invokingExtensionMethod = mfv.IsExtensionMember && Some mfv.ApparentEnclosingEntity <> mfv.DeclaringEntity
        let offset = if invokingExtensionMethod then 1 else 0
        Some paramOwner.Parameters.[paramIndex + offset]

    // Skip unnamed parameters
    match param with
    | Some param when param.ShortName <> SharedImplUtil.MISSING_DECLARATION_NAME -> param
    | _ -> null

[<Language(typeof<FSharpLanguage>)>]
type FSharpMethodInvocationUtil() =
    interface IFSharpMethodInvocationUtil with
        member x.GetArgumentsOwner(expr) = getArgumentsOwner expr
        member x.GetMatchingParameter(expr) = getMatchingParameter expr
        member x.GetNamedArg(expr) = tryGetNamedArg expr
