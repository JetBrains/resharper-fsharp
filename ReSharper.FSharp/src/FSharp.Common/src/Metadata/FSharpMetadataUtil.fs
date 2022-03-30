module JetBrains.ReSharper.Plugins.FSharp.Metadata.FSharpMetadataUtil

open JetBrains.Util.Extension

[<CompiledName("GetCompiledModuleDeclaredName")>]
let getCompiledModuleDeclaredName (entityKind: EntityKind) (logicalName: string) =
    match entityKind with
    | EntityKind.ModuleWithSuffix -> AlternativeNames(logicalName.SubstringBeforeLast("Module"), logicalName)
    | _ -> SingleName(logicalName)