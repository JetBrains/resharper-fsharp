[<AutoOpen; Extension>]
module JetBrains.ReSharper.Plugins.FSharp.Util.FSharpCompilerAttributesUtil

open JetBrains.ReSharper.Psi

[<Extension; CompiledName("GetCompilationMappingAttrInstanceFlag")>]
let getCompilationMappingAttrInstanceFlag (attrInstance: IAttributeInstance) =
    match Seq.tryHead (attrInstance.PositionParameters()) with
    | None -> SourceConstructFlags.None
    | Some parameter -> parameter.ConstantValue.Value :?> SourceConstructFlags

[<Extension; CompiledName("GetCompilationMappingFlag")>]
let getCompilationMappingFlag (attrsOwner: IAttributesOwner) =
    attrsOwner.GetAttributeInstances(compilationMappingAttrTypeName, false)
    |> Seq.tryHead
    |> Option.map getCompilationMappingAttrInstanceFlag
    |> Option.defaultValue SourceConstructFlags.None

[<Extension; CompiledName("IsFSharpField")>]
let isFSharpField (property: IProperty) =
    getCompilationMappingFlag property = SourceConstructFlags.Field

[<Extension; CompiledName("IsUnionCase")>]
let isUnionCase (property: IAttributesOwner) =
    getCompilationMappingFlag property = SourceConstructFlags.UnionCase
