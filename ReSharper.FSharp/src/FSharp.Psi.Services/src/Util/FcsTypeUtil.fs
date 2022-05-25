module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil

open FSharp.Compiler.Symbols

let getFunctionReturnType parametersCount (fcsType: FSharpType) =
    let rec skipInnerTypes remaining (fcsType: FSharpType) =
        if remaining = 0 then fcsType
        else
            skipInnerTypes (remaining - 1) fcsType.GenericArguments[1]

    let returnType = skipInnerTypes parametersCount fcsType
    returnType

let getFunctionParameterTypes parametersCount (fcsType: FSharpType) =
    let result = Array.zeroCreate parametersCount
    let mutable fullType = fcsType

    for i = 0 to parametersCount - 1 do
        result[i] <- fullType.GenericArguments[0]
        fullType <- fullType.GenericArguments[1]

    result

// methods take "this" arg as first parameter so skip it
let getMethodParameterTypes parametersCount (fcsType: FSharpType) =
    let result = Array.zeroCreate parametersCount
    let mutable fullType = fcsType.GenericArguments[1]

    for i = 0 to parametersCount - 1 do
        result[i] <- fullType.GenericArguments[0]
        fullType <- fullType.GenericArguments[1]

    result

let getFunctionParameterAt parameterIndex (fcsType: FSharpType) =
    let rec getParameter index (fcsType: FSharpType) =
        if index = 0 then fcsType.GenericArguments[0]
        else
            getParameter (index - 1) fcsType.GenericArguments[1]

    getParameter parameterIndex fcsType

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
