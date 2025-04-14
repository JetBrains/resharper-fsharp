namespace JetBrains.ReSharper.Plugins.FSharp.Metadata

open System
open System.Text
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.Util.Extension

type FSharpMetadataTypeReference =
    | Local of index: int
    | NonLocal of compilationUnitName: string * typeNames: string[]

    member this.ShortName =
        match this with
        | Local _ -> None
        | NonLocal(_, typeNames) ->
            typeNames
            |> Array.tryLast
            |> Option.map _.SubstringBeforeLast("`", StringComparison.Ordinal)

[<RequireQualifiedAccess>]
type FSharpMetadataType =
    | TypeRef of typeRef: FSharpMetadataTypeReference
    | App of typeRef: FSharpMetadataTypeReference * inst: FSharpMetadataType[]
    | Function
    | Other

type FSharpMetadataMemberInfo =
    { ApparentEnclosingEntity: FSharpMetadataTypeReference }

type FSharpMetadataValue =
    { LogicalName: string
      CompiledName: string option
      IsExtensionMember: bool
      ApparentEnclosingTypeReference: FSharpMetadataTypeReference
      IsPublic: bool // todo: replace with the accessibility
      IsLiteral: bool // todo: replace with the literal value
      IsFunction: bool // todo: replace with the type }
    }

[<RequireQualifiedAccess>]
type FSharpMetadataModuleNameKind =
    | Normal
    | HasModuleSuffix
    | Anon

[<RequireQualifiedAccess>]
type FSharpCompiledTypeRepresentation =
    | Module of nameKind: FSharpMetadataModuleNameKind * values: FSharpMetadataValue[]
    | Union of cases: string[]
    | Other
    | Erased

    member this.HasRepresentation =
        match this with
        | Other | Erased -> false
        | _ -> true


type FSharpMetadataEntity =
    { Index: int 
      LogicalName: string
      CompiledName: string option
      TypeParameterCount: int
      AccessRights: AccessRights
      mutable EntityKind: EntityKind
      mutable CompilationPath: (string * EntityKind)[]
      mutable Representation: FSharpCompiledTypeRepresentation }

module FSharpMetadataEntity =
    let create index logicalName compiledName typeParameterCount compilationPath accessRights =
        { Index = index
          LogicalName = logicalName
          CompiledName = compiledName
          TypeParameterCount = typeParameterCount
          AccessRights = accessRights
          EntityKind = Unchecked.defaultof<_>
          CompilationPath = compilationPath
          Representation = Unchecked.defaultof<_> }

    let getEntityQualifiedName (entity: FSharpMetadataEntity) =
        let stringBuilder = StringBuilder()
        for name, entityKind in entity.CompilationPath do
            stringBuilder.Append(name) |> ignore
            stringBuilder.Append(if entityKind = EntityKind.Namespace then "." else "+") |> ignore
  
        stringBuilder.Append(entity.CompiledName |> Option.defaultValue entity.LogicalName) |> ignore
        stringBuilder.ToString()

    let getCompiledModuleDeclaredName (entity: FSharpMetadataEntity) =
        if isNull entity then SingleName(SharedImplUtil.MISSING_DECLARATION_NAME) else

        let logicalName = entity.LogicalName.SubstringBefore("`")

        match entity.EntityKind, entity.CompiledName with
        | EntityKind.ModuleWithSuffix, _ -> AlternativeNames(logicalName.SubstringBeforeLast("Module"), logicalName)
        | _, Some compiledName -> AlternativeNames(logicalName, compiledName)
        | _ -> SingleName(logicalName)

    let getRepresentation (entity: FSharpMetadataEntity) =
        if isNull entity then FSharpCompiledTypeRepresentation.Erased else entity.Representation
