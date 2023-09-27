module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FSharpExpectedTypesUtil

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Util

let rec extractPartialSubstitution (declType: FSharpType) (argType: FSharpType) =
    if declType.IsGenericParameter then [declType.GenericParameter, argType] else

    // todo: functions, tuples
    if not declType.HasTypeDefinition || not argType.HasTypeDefinition then [] else

    let declTypeDefinition = declType.TypeDefinition
    let argTypeDefinition = argType.TypeDefinition

    // todo: inheritors: e.g. list vs seq
    // todo: different abbreviations
    if not (declTypeDefinition.Equals(argTypeDefinition))  then [] else

    // todo: tuple
    let declTypeArgs = declType.GenericArguments
    let argTypeArgs = argType.GenericArguments

    let concat = 
        (List.ofSeq declTypeArgs, List.ofSeq argTypeArgs)
        ||> List.map2 extractPartialSubstitution
        |> List.concat

    concat

let getFunTypeArgs (fcsType: FSharpType) =
    let rec loop acc (fcsType: FSharpType) =
        if not fcsType.IsFunctionType then List.rev acc else

        let acc = fcsType.GenericArguments[0] :: acc
        loop acc fcsType.GenericArguments[1]
    loop [] fcsType

let tryGetExpectedFcsType (expr: IFSharpExpression): (FSharpType * FSharpDisplayContext) option =
    match FSharpMethodInvocationUtil.getFunExprAndPosition expr with
    | None -> None
    | Some(funExpr, (groupIndex, paramIndex), prefixAppExpr) ->

    // Disable for non-last arg for now, like in `f arg1 {caret} arg2`. 
    let outerPrefixAppExpr = PrefixAppExprNavigator.GetByFunctionExpression(prefixAppExpr.IgnoreParentParens())
    if isNotNull outerPrefixAppExpr then None else

    let funExpr = funExpr.As<IReferenceExpr>()
    if isNull funExpr then None else

    let funFcsSymbolUse = funExpr.Reference.GetSymbolUse()
    if isNull funFcsSymbolUse then None else

    let funMfv = funFcsSymbolUse.Symbol.As<FSharpMemberOrFunctionOrValue>()
    if isNull funMfv then None else

    let funFcsType = funMfv.FullType
    if not funFcsType.IsFunctionType then None else

    let mfvParameterGroups = funMfv.CurriedParameterGroups

    let partialSubstitution =
        let binaryAppExpr = BinaryAppExprNavigator.GetByRightArgument(prefixAppExpr)
        if isNull binaryAppExpr then [] else

        // todo: check isPredefined
        // todo: generalize for other operators
        match binaryAppExpr.ShortName with
        | "|>" ->
            let arg = binaryAppExpr.LeftArgument
            if isNull arg then [] else

            let parameterGroups = mfvParameterGroups
            if parameterGroups.Count < 2 then [] else

            let lastParamGroup = parameterGroups[parameterGroups.Count - 1]
            if lastParamGroup.Count <> 1 then [] else // todo: tuples?

            let fcsParamType = lastParamGroup[0].Type

            let leftArgType = arg.TryGetFcsType()
            if isNull leftArgType then [] else

            extractPartialSubstitution fcsParamType leftArgType

        | "||>" ->
            let arg = binaryAppExpr.LeftArgument
            if isNull arg then [] else

            let leftArgType = arg.TryGetFcsType()
            if isNull leftArgType || not leftArgType.IsTupleType then [] else

            let tupleTypeArgs = leftArgType.GenericArguments
            if tupleTypeArgs.Count <> 2 then [] else

            let tupleTypeArgs = List.ofSeq tupleTypeArgs

            let mfvCurriedParameterGroups = funMfv.CurriedParameterGroups

            let parameterGroups = mfvCurriedParameterGroups
            let parameterGroupCount = mfvCurriedParameterGroups.Count
            if parameterGroupCount < 3 then [] else

            let fcsParamGroups = 
                [ parameterGroups[parameterGroupCount - 2]
                  parameterGroups[parameterGroupCount - 1] ]

            (fcsParamGroups, tupleTypeArgs)
            ||> List.zip
            |> List.filter (fun (paramGroup, _) -> paramGroup.Count = 1) // todo: tuples
            |> List.map (fun (paramGroup, argType) -> paramGroup[0].Type, argType)
            |> List.unzip
            ||> List.map2 extractPartialSubstitution
            |> List.concat

        | _ -> []

    let fcsFunParamTypes = getFunTypeArgs funFcsType

    List.tryItem groupIndex fcsFunParamTypes
    |> Option.bind (fun paramGroupType ->
        if paramGroupType.IsTupleType && not paramGroupType.IsStructTupleType then
            let genericArguments = paramGroupType.GenericArguments
            Seq.tryItem paramIndex genericArguments
        elif paramIndex = 0 then
            Some(paramGroupType)
        else
            None)
    |> Option.map (fun t -> t.Instantiate(partialSubstitution), funFcsSymbolUse.DisplayContext)
