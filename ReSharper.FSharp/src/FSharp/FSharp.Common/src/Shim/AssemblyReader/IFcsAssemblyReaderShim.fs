namespace JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader

open System
open FSharp.Compiler.AbstractIL.ILBinaryReader
open JetBrains.DataFlow
open JetBrains.ProjectModel
open JetBrains.ReSharper.Psi.Modules
open JetBrains.Util.Dotnet.TargetFrameworkIds

type FcsProjectKey =
    { Project: IProject
      TargetFrameworkId: TargetFrameworkId }

    static member Create(psiModule: IPsiModule) =
        { Project = psiModule.ContainingProjectModule :?> _
          TargetFrameworkId = psiModule.TargetFrameworkId }

    static member Create(project, targetFrameworkId) =
        { Project = project
          TargetFrameworkId = targetFrameworkId }

type IProjectFcsModuleReader =
    inherit ILModuleReader

    abstract Path: VirtualFileSystemPath
    abstract PsiModule: IPsiModule
    abstract Timestamp: DateTime

    abstract MarkDirty: typeShortName: string -> unit

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


type IFcsAssemblyReaderShim =
    abstract IsEnabled: bool
    abstract HasDirtyModules: bool
    abstract Logger: ILogger

    abstract ProjectInvalidated: ISignal<FcsProjectKey>

    abstract TryGetModuleReader: projectKey: FcsProjectKey -> IProjectFcsModuleReader option

    abstract IsKnownModule: IPsiModule -> bool
    abstract IsKnownModule: VirtualFileSystemPath -> bool

    /// Marks module as dirty, so it could be invalidated before the next FCS request
    abstract MarkDirty: IPsiModule -> unit

    /// Clears dirty type defs, updating reader timestamps if needed
    abstract InvalidateDirty: unit -> unit

    /// Clears dirty type defs, updating reader timestamps if needed
    /// todo: remove psi module, replace with project key
    abstract InvalidateDirty: psiModule: IPsiModule -> unit

    abstract TestDump: string
