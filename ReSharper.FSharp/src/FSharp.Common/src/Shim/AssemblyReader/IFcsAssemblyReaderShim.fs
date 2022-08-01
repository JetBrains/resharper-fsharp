namespace JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader

open System
open FSharp.Compiler.AbstractIL.ILBinaryReader
open JetBrains.Metadata.Reader.API
open JetBrains.ReSharper.Psi.Modules

type IProjectFcsModuleReader =
    inherit ILModuleReader

    abstract Path: VirtualFileSystemPath
    abstract PsiModule: IPsiModule
    abstract Timestamp: DateTime

    abstract Invalidate: unit -> unit
    abstract CreateAllTypeDefs: unit -> unit

    // todo: change to shortName, update short names dict too
    abstract InvalidateTypeDef: typeName: IClrTypeName -> unit
    abstract InvalidateReferencingTypes: shortName: string -> unit
    abstract InvalidateTypesReferencingFSharpModule: psiModule: IPsiModule -> unit

    abstract UpdateTimestamp: unit -> unit

    /// Debug data, disabled by default
    abstract RealModuleReader: ILModuleReader option with get, set


[<RequireQualifiedAccess>]
type ReferencedAssembly =
    /// An output of a psi source project except for F# projects.
    | ProjectOutput of IProjectFcsModuleReader

    /// Not supported file or output assembly for F# project.
    | Ignored of path: VirtualFileSystemPath

    member this.Path =
        match this with
        | ProjectOutput reader -> reader.Path
        | Ignored path -> path


module ReferencedAssembly =
    let invalid = ReferencedAssembly.Ignored(VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext))


type IFcsAssemblyReaderShim =
    abstract IsEnabled: bool
    abstract GetModuleReader: psiModule: IPsiModule -> ReferencedAssembly

    abstract IsKnownModule: IPsiModule -> bool
    abstract IsKnownModule: VirtualFileSystemPath -> bool

    /// Removes reader for the module if present, another reader is going to be created for it
    abstract InvalidateModule: psiModule: IPsiModule -> unit

    abstract InvalidateAll: unit -> unit

    /// Clears dirty type defs, updating reader timestamps if needed
    abstract InvalidateDirty: unit -> unit

    /// Record referenced project chains, later used for invalidation
    abstract RecordDependencies: psiModule: IPsiModule -> unit

    abstract RemoveModule: psiModule: IPsiModule -> unit

    abstract TestDump: string
