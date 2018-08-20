namespace rec JetBrains.ReSharper.Plugins.FSharp.Common.Naming

open JetBrains.ReSharper.Psi.ExtensionsAPI

type CompiledNameKind =
    | Attribute
    | ImplicitModule
    | ExplicitModule

type FSharpName =
    /// Declared name as seen by F# source code and in compiled assemblies.
    | DeclaredName of name: string

    /// Generated compiled name used in union case or exception fields like Item1 or Data0 when source name is missing.
    | GeneratedName of name: string

    /// Declared name as seen by F# source code and compiled name as seen in compiled assembly.
    | MultipleNames of sourceName: string * compiledName: string * kind: CompiledNameKind

    member x.SourceName =
        match x with
        | DeclaredName name
        | MultipleNames (name, _, _) -> name
        | _ -> SharedImplUtil.MISSING_DECLARATION_NAME

    member x.CompiledName =
        match x with
        | DeclaredName name
        | MultipleNames (_, name, _)
        | GeneratedName name -> name

    static member Create(name: string) =
        match name with
        | null -> FSharpName.missingName
        | name -> DeclaredName name

[<RequireQualifiedAccess>]
module FSharpName =
    [<CompiledName("MissingName")>]
    let missingName = DeclaredName SharedImplUtil.MISSING_DECLARATION_NAME

