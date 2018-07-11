namespace rec JetBrains.ReSharper.Plugins.FSharp.Common.Shim.FileSystem

open System
open System.IO
open System.Collections.Concurrent
open System.Runtime.InteropServices
open System.Text
open JetBrains.Application.changes
open JetBrains.DataFlow
open JetBrains.DocumentManagers
open JetBrains.DocumentManagers.impl
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Common.Util.CommonUtil
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.ReSharper.Resources.Shell
open Microsoft.FSharp.Compiler.AbstractIL.Internal.Library

type FSharpSource =
    { Source: byte[]
      Timestamp: DateTime }


[<SolutionComponent>]
type FSharpSourceCache
        (lifetime: Lifetime, changeManager, documentManager: DocumentManager, extensions: IProjectFileExtensions) =
    inherit FileSystemShimChangeProvider
        (Lifetimes.Define(lifetime).Lifetime, Shim.FileSystem, changeManager, documentManager.ChangeProvider)

    let files = ConcurrentDictionary<FileSystemPath, FSharpSource>()

    let getText (document: IDocument) =
        Encoding.UTF8.GetBytes(document.GetText())

    let tryAddSource (path: FileSystemPath) =
        let mutable source = None
        ReadLockCookie.TryExecute(fun _ ->
            match files.TryGetValue(path) with
            | true, value -> source <- Some value
            | _ ->

            match documentManager.GetOrCreateDocument(path) with
            | null -> ()
            | document ->
                let timestamp = File.GetLastWriteTimeUtc(path.FullPath)
                source <- Some { Source = getText document; Timestamp = timestamp }
                files.[path] <- source.Value) |> ignore
        source

    member x.TryGetSource(path: FileSystemPath, [<Out>] source: byref<FSharpSource>) =
        if not (extensions.GetFileType(path).Is<FSharpProjectFileType>()) then false else

        match files.TryGetValue(path) with
        | true, value -> source <- value; true
        | _ ->

        match tryAddSource path with
        | Some v -> source <- v; true
        | _ -> false

    override x.FileStreamReadShim(fileName) =
        let path = FileSystemPath.TryParse(fileName)
        if path.IsEmpty then base.FileStreamReadShim(fileName) else

        match x.TryGetSource(path) with
        | true, source -> new MemoryStream(source.Source) :> Stream
        | _ -> base.FileStreamReadShim(fileName)

    override x.GetLastWriteTime(path) =
        match x.TryGetSource(path) with
        | true, source -> source.Timestamp
        | _ -> base.GetLastWriteTime(path)

    override x.Exists(path) =
        match files.TryGetValue(path) with
        | true, value -> true
        | _ -> base.Exists(path)

    override x.Execute(changeMap) =
        let change = changeMap.GetChange<ProjectFileDocumentCopyChange>(documentManager.ChangeProvider)
        if isNotNull change then
            let file = change.ProjectFile
            if file.LanguageType.Is<FSharpProjectFileType>() then
                 files.[file.Location] <- { Source = getText change.Document; Timestamp = DateTime.UtcNow }
        null
