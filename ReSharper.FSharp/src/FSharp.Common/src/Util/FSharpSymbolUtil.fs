[<AutoOpen>]
module JetBrains.ReSharper.Plugins.FSharp.Common.Util.FSharpSymbolUtil

open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.SourceCodeServices.Extensions

[<CompiledName("GetReturnType")>]
let getReturnType (symbol : FSharpSymbol) =
    match symbol with
    | :? FSharpMemberOrFunctionOrValue as mfv -> Some mfv.ReturnParameter.Type
    | :? FSharpField as field -> Some field.FieldType
    | :? FSharpParameter as param -> Some param.Type
    | _ -> None

[<CompiledName("TryGetFullCompiledName")>]    
let tryGetFullCompiledName (entity : FSharpEntity) =
    entity.TryGetFullCompiledName()