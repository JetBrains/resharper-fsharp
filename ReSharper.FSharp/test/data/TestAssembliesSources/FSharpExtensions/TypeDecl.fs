namespace global

[<CompiledName "CompiledName">]
type SourceName =
    { F: int }

open System.Diagnostics.CodeAnalysis

type InjectionsOwner() =
    [<StringSyntax("regex")>]
    member _.InjectionProp with set (_: string) = ()
