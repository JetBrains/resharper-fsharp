module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Util.FcsTypesUtil

open FSharp.Compiler.SourceCodeServices

let getFunctionTypeArgs fcsType =
    let rec loop (fcsType: FSharpType) acc =
        let args = fcsType.GenericArguments
        let acc = args.[0] :: acc

        let argType = args.[1]
        if argType.IsFunctionType then
            loop argType acc
        else
            argType :: acc

    loop fcsType [] |> List.rev
