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

    let refExpr = binaryAppExpr.LeftArgument.As<IReferenceExpr>()
    if isNull refExpr then null else

    refExpr.Reference.Resolve().DeclaredElement.As<IParameter>()


let tryGetNamedArg (expr: IFSharpExpression) =
    let binaryAppExpr = expr.As<IBinaryAppExpr>()
    if isNull binaryAppExpr then null else
    resolveNamedArg binaryAppExpr


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

        // todo: this should be cached, and kept on the FSharpArgumentsOwner
        // todo: this is basically doing the same as PrefixAppExpr.Arguments, can they be merged?
        let args =
            argsOwner.AppliedExpressions
            |> Seq.map (fun expr -> expr.IgnoreInnerParens())
            |> Seq.zip mfv.CurriedParameterGroups
            |> Seq.collect (fun (paramGroup, argExpr) ->
                match paramGroup.Count with
                | 0 -> Seq.empty
                | 1 -> Seq.singleton (argExpr :?> IArgument)
                | count ->
                    match argExpr with
                    | :? ITupleExpr as tupleExpr ->
                        Seq.init paramGroup.Count (fun i ->
                            if i < tupleExpr.Expressions.Count then
                                tupleExpr.Expressions.[i] :?> IArgument
                            else
                                null
                        )
                    | _ ->
                        Seq.init count (fun _ -> null)
            )

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
