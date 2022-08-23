module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypeUtil

open System
open FSharp.Compiler.Symbols
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

let isOption (fcsType: FSharpType) =
    fcsType.StrippedType.QualifiedBaseName = FSharpPredefinedType.fsOptionTypeName.FullName

let isValueOption (fcsType: FSharpType) =
    fcsType.StrippedType.QualifiedBaseName = FSharpPredefinedType.fsValueOptionTypeName.FullName

let isChoice (fcsType: FSharpType) =
    fcsType.StrippedType.QualifiedBaseName.StartsWith("Microsoft.FSharp.Core.FSharpChoice`", StringComparison.Ordinal)
