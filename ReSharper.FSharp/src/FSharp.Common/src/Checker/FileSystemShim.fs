namespace JetBrains.ReSharper.Plugins.FSharp.Common

open System.IO
open JetBrains.DataFlow
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Common.Checker
open JetBrains.Util
open Microsoft.FSharp.Compiler.AbstractIL.Internal.Library

[<SolutionComponent>]
type FileSystemShim(lifetime: Lifetime, sourceCache: FSharpSourceCache) as this =
    let defaultFileSystem = Shim.FileSystem
    do
        Shim.FileSystem <- this
        lifetime.AddAction(fun _ -> Shim.FileSystem <- defaultFileSystem) |> ignore

    let getSource (path: string) =
        let path = FileSystemPath.TryParse(path)
        match path.ExtensionNoDot.ToLowerInvariant() with
        | FSharpSourceExtension -> sourceCache.GetSource(path)
        | _ -> None

    interface IFileSystem with
        member x.FileStreamReadShim(path) =
            match getSource path with
            | Some source -> new MemoryStream(source.Source) :> _
            | _ -> defaultFileSystem.FileStreamReadShim(path)

        member x.GetLastWriteTimeShim(path) =
            match getSource path with
            | Some source -> source.Timestamp
            | _ -> defaultFileSystem.GetLastWriteTimeShim(path)

        member x.FileStreamWriteExistingShim(fileName) = defaultFileSystem.FileStreamWriteExistingShim(fileName)
        member x.FileStreamCreateShim(fileName) = defaultFileSystem.FileStreamCreateShim(fileName)
        member x.IsInvalidPathShim(fileName) = defaultFileSystem.IsInvalidPathShim(fileName)
        member x.ReadAllBytesShim(fileName) = defaultFileSystem.ReadAllBytesShim(fileName)
        member x.IsPathRootedShim(fileName) = defaultFileSystem.IsPathRootedShim(fileName)
        member x.AssemblyLoadFrom(fileName) = defaultFileSystem.AssemblyLoadFrom(fileName)
        member x.AssemblyLoad(assemblyName) = defaultFileSystem.AssemblyLoad(assemblyName)
        member x.GetFullPathShim(fileName) = defaultFileSystem.GetFullPathShim(fileName)
        member x.SafeExists(fileName) = defaultFileSystem.SafeExists(fileName)
        member x.FileDelete(fileName) = defaultFileSystem.FileDelete(fileName)
        member x.GetTempPathShim() = defaultFileSystem.GetTempPathShim()
