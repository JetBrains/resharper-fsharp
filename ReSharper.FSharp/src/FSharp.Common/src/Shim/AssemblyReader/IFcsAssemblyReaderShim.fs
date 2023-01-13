namespace JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader

open System
open FSharp.Compiler.AbstractIL.ILBinaryReader
open JetBrains.ReSharper.Psi.Modules

type IProjectFcsModuleReader =
    inherit ILModuleReader

    abstract Path: VirtualFileSystemPath
    abstract PsiModule: IPsiModule
    abstract Timestamp: DateTime

    /// Marks as possibly needing the timestamp update
    abstract MarkDirty: unit -> unit

    /// Removes created type defs with changed type parts
    abstract InvalidateTypeDefs: shortName: string -> unit

    /// Removes all created type defs due to invalidation requested externally, e.g. when references assemblies change
    abstract InvalidateAllTypeDefs: unit -> unit

    /// Removes outdated type defs and updates module timestamp if needed
    abstract UpdateTimestamp: unit -> unit

    /// Debug data, disabled by default
    abstract RealModuleReader: ILModuleReader option with get, set


[<RequireQualifiedAccess>]
type ReferencedAssembly =
    /// An output of a psi source project except for F# projects.
    | ProjectOutput of IProjectFcsModuleReader * ILModuleReader option

    /// Not supported file or output assembly for F# project.
    | Ignored of path: VirtualFileSystemPath

    member this.Path =
        match this with
        | ProjectOutput(reader, _) -> reader.Path
        | Ignored path -> path


module ReferencedAssembly =
    let invalid = ReferencedAssembly.Ignored(VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext))


type IFcsAssemblyReaderShim =
    abstract IsEnabled: bool
    abstract HasDirtyTypes: bool
    abstract Logger: ILogger

    abstract GetModuleReader: psiModule: IPsiModule -> ReferencedAssembly

    abstract IsKnownModule: IPsiModule -> bool
    abstract IsKnownModule: VirtualFileSystemPath -> bool

    /// Removes reader for the module if present, another reader is going to be created for it
    abstract InvalidateModule: psiModule: IPsiModule -> unit

    abstract InvalidateAll: reason: string -> unit

    abstract MarkTypesDirty: IPsiModule -> unit

    /// Clears dirty type defs, updating reader timestamps if needed
    abstract InvalidateDirty: unit -> unit

    /// Clears dirty type defs, updating reader timestamps if needed
    abstract InvalidateDirty: psiModule: IPsiModule -> unit

    abstract RemoveModule: psiModule: IPsiModule -> unit

    abstract TestDump: string
    abstract RecordInvalidations: bool with set
