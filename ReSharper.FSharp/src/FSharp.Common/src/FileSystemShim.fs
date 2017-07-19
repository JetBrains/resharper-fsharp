namespace JetBrains.ReSharper.Plugins.FSharp.Common

open JetBrains.DataFlow
open JetBrains.DocumentManagers
open JetBrains.ProjectModel
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Util
open Microsoft.FSharp.Compiler.AbstractIL.Internal.Library
open System.Collections.Generic
open System.IO
open System.Text

[<SolutionComponent>]
type FileSystemShim(lifetime: Lifetime, documentManager: DocumentManager) as this =
    static let fsExtensions = HashSet(Seq.ofList ["fs"; "fsi"; "fsx"; "ml"; "mli"; "fsscript"])
    let defaultFileSystem = Shim.FileSystem
    do
        Shim.FileSystem <- this
        lifetime.AddAction(fun _ -> Shim.FileSystem <- defaultFileSystem) |> ignore
    
    interface IFileSystem with
        member x.FileStreamReadShim(fileName) =
            match FileSystemPath.TryParse(fileName) with
            | path when not path.IsEmpty && fsExtensions.Contains(path.ExtensionNoDot) ->
                let document = documentManager.GetOrCreateDocument(path)
                let mutable text = null
                if ReadLockCookie.TryExecute(fun () -> text <- document.GetText()) then
                    new MemoryStream(Encoding.UTF8.GetBytes(text)) :> _
                else defaultFileSystem.FileStreamReadShim(fileName)
            | _ -> defaultFileSystem.FileStreamReadShim(fileName)
        
        member x.GetLastWriteTimeShim(fileName) = defaultFileSystem.GetLastWriteTimeShim(fileName) // todo
        
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
