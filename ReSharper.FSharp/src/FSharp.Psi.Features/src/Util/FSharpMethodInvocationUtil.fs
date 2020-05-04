module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpMethodInvocationUtil

open FSharp.Compiler.SourceCodeServices
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree


let resolveNamedArg (binaryAppExpr: IBinaryAppExpr) =
    if binaryAppExpr.Operator.Reference.GetName() <> "=" then null else

    match binaryAppExpr.LeftArgument with
    | :? IReferenceExpr as refExpr -> refExpr.Reference.Resolve().DeclaredElement.As<IParameter>()
    | _ -> null


let tryGetNamedArg (expr: IFSharpExpression) =
    match expr with
    | :? IBinaryAppExpr as binaryAppExpr -> resolveNamedArg binaryAppExpr
    | _ -> null


let getMatchingParameter (expr: IFSharpExpression) =
    let argsOwner =
        let tupleExpr = TupleExprNavigator.GetByExpression(expr.IgnoreParentParens())
        let exprContext = if isNull tupleExpr then expr else tupleExpr :> _
        FSharpArgumentOwnerNavigator.GetByArgumentExpression(exprContext.IgnoreParentParens())

    if isNull argsOwner then null else

    let binaryAppExpr = expr.As<IBinaryAppExpr>()
    // todo: recover from named args failures
    if isNotNull binaryAppExpr then resolveNamedArg binaryAppExpr else

    let symbolReference = argsOwner.Reference
    if isNull symbolReference then null else

    match box (symbolReference.GetFSharpSymbol()) with
    | :? FSharpMemberOrFunctionOrValue as mfv ->
        let args = argsOwner.ParameterArguments

        match args |> Seq.tryFindIndex (fun argExpr -> expr.Equals(argExpr)) with
        | None -> null
        | Some paramIndex ->

        let paramOwner = symbolReference.Resolve().DeclaredElement.As<IParametersOwner>()
        if isNull paramOwner then null else

        let invokingExtensionMethod = mfv.IsExtensionMember && Some mfv.ApparentEnclosingEntity <> mfv.DeclaringEntity
        let offset = if invokingExtensionMethod then 1 else 0
        let param = paramOwner.Parameters.[paramIndex + offset]

        // Skip unnamed parameters
        if param.ShortName = SharedImplUtil.MISSING_DECLARATION_NAME then null else param

    | _ -> null

[<Language(typeof<FSharpLanguage>)>]
type FSharpMethodInvocationUtil() =
    interface IFSharpMethodInvocationUtil with
        member x.GetMatchingParameter(expr) = getMatchingParameter expr
        member x.GetNamedArg(expr) = tryGetNamedArg expr
