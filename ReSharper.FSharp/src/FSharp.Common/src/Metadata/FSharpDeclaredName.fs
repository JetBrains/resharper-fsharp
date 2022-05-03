namespace JetBrains.ReSharper.Plugins.FSharp.Metadata

type FSharpDeclaredName =
    | SingleName of sourceName: string
    | AlternativeNames of sourceName: string * compiledName: string

    member this.SourceName =
        match this with
        | SingleName(name)
        | AlternativeNames(sourceName = name) -> name

    member this.CompiledName =
        match this with
        | SingleName(name)
        | AlternativeNames(compiledName = name) -> name

    /// IAlternativeNameOwner.AlternativeName should return null when there's effectively no alternative name.
    member this.AlternativeName =
        match this with
        | AlternativeNames(sourceName, compiledName) when sourceName <> compiledName -> sourceName
        | _ -> null
