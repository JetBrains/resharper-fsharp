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


[<Extension; CompiledName("IsCompiledRecord")>]
let isCompiledRecord (property: IAttributesOwner) =
    getCompilationMappingFlag property = SourceConstructFlags.RecordType

[<Extension; CompiledName("IsCompiledFSharpField")>]
let isCompiledFSharpField (property: IProperty) =
    getCompilationMappingFlag property = SourceConstructFlags.Field

[<Extension; CompiledName("IsCompiledUnion")>]
let isCompiledUnion (property: IAttributesOwner) =
    getCompilationMappingFlag property = SourceConstructFlags.SumType

[<Extension; CompiledName("IsCompiledUnionCase")>]
let isCompiledUnionCase (property: IAttributesOwner) =
    getCompilationMappingFlag property = SourceConstructFlags.UnionCase

[<Extension; CompiledName("IsCompiledModule")>]
let isCompiledModule (property: IAttributesOwner) =
    getCompilationMappingFlag property = SourceConstructFlags.Module

[<Extension; CompiledName("IsCompiledException")>]
let isCompiledException (property: IAttributesOwner) =
    getCompilationMappingFlag property = SourceConstructFlags.Exception


[<Extension; CompiledName("GetAutoOpenAttributes")>]
let getAutoOpenAttributes (attributesSet: IAttributesSet) =
    attributesSet.GetAttributeInstances(autoOpenAttrTypeName, AttributesSource.Self)


[<Extension; CompiledName("HasRequireQualifiedAccessAttribute")>]
let hasRequireQualifiedAccessAttribute (attrsOwner: IAttributesOwner) =
    attrsOwner.HasAttributeInstance(requireQualifiedAccessAttrTypeName, false)
