﻿module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil

open FSharp.Compiler.Symbols

let rec skipFunctionParameters (fcsType: FSharpType) paramsToSkipCount =
    if paramsToSkipCount = 0 || not fcsType.IsFunctionType then fcsType else

    let returnType = fcsType.GenericArguments[1]
    skipFunctionParameters returnType (paramsToSkipCount - 1)

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


let private emptyDisplayContext =
    FSharpDisplayContext.Empty.WithShortTypeNames(true)


type FSharpType with
    member this.Format() =
        this.Format(emptyDisplayContext)
