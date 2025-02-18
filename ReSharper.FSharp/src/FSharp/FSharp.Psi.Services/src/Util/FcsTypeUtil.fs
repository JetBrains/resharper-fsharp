module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil

open FSharp.Compiler.Symbols
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Util

let getFunctionTypeArgs includeReturnType fcsType =
    let rec loop (fcsType: FSharpType) acc =
        if not fcsType.IsFunctionType then [] else 

        let args = fcsType.GenericArguments
        let acc = args[0] :: acc

        let argType = args[1]
        if argType.IsFunctionType then
            loop argType acc
        else
            if includeReturnType then
                argType :: acc
            else
                acc

    loop fcsType [] |> List.rev

let tryGetOuterOptionalParameterAndItsType (pattern: IReferencePat) fcsType =
    let optionalValPat = OptionalValPatNavigator.GetByPattern(pattern)
    if isNotNull optionalValPat && isOption fcsType then (optionalValPat : IFSharpPattern), fcsType.GenericArguments[0]
    else pattern, fcsType
