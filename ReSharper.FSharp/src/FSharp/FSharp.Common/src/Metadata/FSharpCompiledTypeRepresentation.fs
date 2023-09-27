namespace JetBrains.ReSharper.Plugins.FSharp.Metadata

open System.Text
open JetBrains.Util.Extension

[<RequireQualifiedAccess>]
type FSharpCompiledTypeRepresentation =
    | Module of hasModuleSuffix: bool
    | Union of cases: string[]
    | Other

    member this.HasRepresentation =
        match this with
        | Other -> false
        | _ -> true


type FSharpMetadataEntity =
    { Index: int 
      LogicalName: string
      CompiledName: string option
      TypeParameterCount: int
      mutable EntityKind: EntityKind
      mutable CompilationPath: (string * EntityKind)[] option
      mutable Representation: FSharpCompiledTypeRepresentation }

module FSharpMetadataEntity =
    let create index logicalName compiledName typeParameterCount compilationPath =
        { Index = index
          LogicalName = logicalName
          CompiledName = compiledName
          TypeParameterCount = typeParameterCount
          EntityKind = Unchecked.defaultof<_>
          CompilationPath = compilationPath
          Representation = Unchecked.defaultof<_> }

    let getEntityQualifiedName (entity: FSharpMetadataEntity) =
        let stringBuilder = StringBuilder()
        for name, entityKind in entity.CompilationPath |> Option.defaultValue [||] do
            stringBuilder.Append(name) |> ignore
            stringBuilder.Append(if entityKind = EntityKind.Namespace then "." else "+") |> ignore
  
        stringBuilder.Append(entity.CompiledName |> Option.defaultValue entity.LogicalName) |> ignore
        stringBuilder.ToString()

    let getCompiledModuleDeclaredName (entity: FSharpMetadataEntity) =
        let logicalName = entity.LogicalName.SubstringBefore("`")

        match entity.EntityKind with
        | EntityKind.ModuleWithSuffix -> AlternativeNames(logicalName.SubstringBeforeLast("Module"), logicalName)
        | _ -> SingleName(logicalName)
