namespace JetBrains.ReSharper.Plugins.FSharp.Common

open System
open System.Collections.Concurrent
open System.IO
open System.Text
open System.Threading
open JetBrains
open JetBrains.Application.FileSystemTracker
open JetBrains.Application.changes
open JetBrains.DataFlow
open JetBrains.DocumentManagers
open JetBrains.DocumentManagers.impl
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Common.Checker
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.ReSharper.Resources.Shell
open Microsoft.FSharp.Compiler.AbstractIL.Internal.Library

[<SolutionComponent>]
type FileSystemShim(lifetime: Lifetime, sources: FSharpSourceCache, assemblies: AssemblyTimestampCache) as this =
    let defaultFileSystem = Shim.FileSystem
    do
        Shim.FileSystem <- this
        lifetime.AddAction(fun _ -> Shim.FileSystem <- defaultFileSystem) |> ignore

    let (|FSharpSourcePath|AssemblyPath|OtherPath|) (path: Util.FileSystemPath) =
        match path.ExtensionNoDot.ToLowerInvariant() with
        | "fs" | "fsi" | "ml" | "mli" | "fsx" | "fsscript" -> FSharpSourcePath
        | "dll" | "exe" -> AssemblyPath
        | _ -> OtherPath

    let applyForPath path sourceFunction assemblyFunction defaultFunction =
        let path = Util.FileSystemPath.TryParse(path)
        match path with
        | FSharpSourcePath -> sourceFunction path
        | AssemblyPath -> assemblyFunction path
        | _ -> None
        |> Option.defaultWith defaultFunction

    interface IFileSystem with
        member x.FileStreamReadShim(path) =
            applyForPath path
                sources.GetSourceStream
                (fun _ -> None)
                (fun _ -> defaultFileSystem.FileStreamReadShim(path))

        member x.GetLastWriteTimeShim(path) =
            applyForPath path
                sources.GetTimestamp
                assemblies.GetTimestamp
                (fun _ -> defaultFileSystem.GetLastWriteTimeShim(path))

        member x.SafeExists(path) =
            applyForPath path
                sources.Exists
                assemblies.Exists
                (fun _ -> defaultFileSystem.SafeExists(path))

        member x.FileStreamWriteExistingShim(fileName) = defaultFileSystem.FileStreamWriteExistingShim(fileName)
        member x.IsStableFileHeuristic(fileName) = defaultFileSystem.IsStableFileHeuristic(fileName)
        member x.FileStreamCreateShim(fileName) = defaultFileSystem.FileStreamCreateShim(fileName)
        member x.IsInvalidPathShim(fileName) = defaultFileSystem.IsInvalidPathShim(fileName)
        member x.ReadAllBytesShim(fileName) = defaultFileSystem.ReadAllBytesShim(fileName)
        member x.IsPathRootedShim(fileName) = defaultFileSystem.IsPathRootedShim(fileName)
        member x.AssemblyLoadFrom(fileName) = defaultFileSystem.AssemblyLoadFrom(fileName)
        member x.AssemblyLoad(assemblyName) = defaultFileSystem.AssemblyLoad(assemblyName)
        member x.GetFullPathShim(fileName) = defaultFileSystem.GetFullPathShim(fileName)
        member x.FileDelete(fileName) = defaultFileSystem.FileDelete(fileName)
        member x.GetTempPathShim() = defaultFileSystem.GetTempPathShim()
