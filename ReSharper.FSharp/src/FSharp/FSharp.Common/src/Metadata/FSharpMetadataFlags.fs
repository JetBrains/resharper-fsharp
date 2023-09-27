namespace JetBrains.ReSharper.Plugins.FSharp.Metadata

open System

type EntityKind =
    | ModuleWithSuffix = 0
    | ModuleOrType = 1
    | Namespace = 2

[<Flags>]
type EntityFlags =
    | IsModuleOrNamespace = 0b000000000000001L
    | IsStruct            = 0b000000000100000L
    | ReservedBit         = 0b000000000010000L
