namespace JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader

open System
open FSharp.Compiler.AbstractIL.ILBinaryReader
open JetBrains.ReSharper.Plugins.FSharp.Checker
open JetBrains.ReSharper.Psi.Modules

type IProjectFcsModuleReader =
    inherit ILModuleReader

    abstract Path: VirtualFileSystemPath
    abstract PsiModule: IPsiModule
    abstract Timestamp: DateTime

    /// Marks as possibly needing the timestamp update
    abstract MarkDirty: unit -> unit

    /// Removes outdated type defs and updates module timestamp if needed
    abstract UpdateTimestamp: unit -> unit

    /// Debug data, disabled by default
    abstract RealModuleReader: ILModuleReader option with get, set


type IFcsAssemblyReaderShim =
    abstract IsEnabled: bool
    abstract Logger: ILogger

    abstract TryGetModuleReader: projectKey: FcsProjectKey -> IProjectFcsModuleReader option

    abstract IsKnownModule: IPsiModule -> bool
    abstract IsKnownModule: VirtualFileSystemPath -> bool

    /// Marks module as dirty, so it could be invalidated before the next FCS request
    abstract MarkDirty: IPsiModule -> unit

    abstract PrepareForFcsRequest: fcsProject: FcsProject -> unit

    abstract TestDump: string
