namespace JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader

open System
open FSharp.Compiler.AbstractIL.ILBinaryReader
open JetBrains.Metadata.Reader.API
open JetBrains.ReSharper.Psi.Modules

type IProjectFcsModuleReader =
    inherit ILModuleReader

    abstract PsiModule: IPsiModule
    abstract Timestamp: DateTime

    abstract CreateAllTypeDefs: unit -> unit

    // todo: change to shortName, update short names dict too
    abstract InvalidateTypeDef: typeName: IClrTypeName -> unit
    abstract InvalidateReferencingTypes: shortName: string -> unit
    abstract InvalidateTypesReferencingFSharpModule: psiModule: IPsiModule -> unit

    /// Debug data, disabled by default
    abstract RealModuleReader: ILModuleReader option with get, set

[<RequireQualifiedAccess>]
type ReferencedAssembly =
    /// An output of a psi source project except for F# projects.
    | ProjectOutput of IProjectFcsModuleReader

    /// Not supported file or output assembly for F# project.
    | Ignored

type IFcsAssemblyReaderShim =
    abstract IsEnabled: bool
    abstract GetModuleReader: psiModule: IPsiModule -> ReferencedAssembly
    abstract InvalidateDirty: unit -> unit

    abstract GetTimestamp: psiModule: IPsiModule -> DateTime
