namespace JetBrains.ReSharper.Plugins.FSharp.Common

open System
open System.Collections.Concurrent
open System.IO
open JetBrains
open JetBrains.Application.FileSystemTracker
open JetBrains.Application.changes
open JetBrains.DataFlow
open JetBrains.ProjectModel

type AssemblyTimestamp =
    | Exists of timestamp: DateTime
    | NotExists

[<SolutionComponent>]
type AssemblyTimestampCache(lifetime: Lifetime, fileSystemTracker: IFileSystemTracker) as this =
    let assemblies = ConcurrentDictionary()

    member x.Exists (path: Util.FileSystemPath) =
        match assemblies.TryGetValue(path) with
        | true, Exists _ -> true
        | true, _ -> false
        | _ ->
            let fullPath = path.FullPath
            let exists = File.Exists(fullPath)
            assemblies.[path] <-
                if exists then Exists (File.GetLastWriteTimeUtc(fullPath))
                else NotExists
            fileSystemTracker.AdviseFileChanges(lifetime, path, Action<_>(this.Update))
            exists
        |> Some

    member x.GetTimestamp (path: Util.FileSystemPath) =
        match assemblies.TryGetValue(path) with
        | true, Exists stamp -> Some stamp
        | _ -> None

    member private x.Update(delta: FileSystemChangeDelta) =
        let path = delta.OldPath
        match delta.ChangeType with
        | FileSystemChangeType.ADDED
        | FileSystemChangeType.CHANGED ->
            assemblies.[path] <- Exists (File.GetLastWriteTimeUtc(path.FullPath))
        | FileSystemChangeType.DELETED
        | FileSystemChangeType.RENAMED ->
            assemblies.[path] <- NotExists
        | _ -> ()
