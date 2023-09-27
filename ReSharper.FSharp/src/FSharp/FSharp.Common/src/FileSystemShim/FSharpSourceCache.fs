namespace JetBrains.ReSharper.Plugins.FSharp.Common.Checker

open System
open System.IO
open System.Collections.Concurrent
open System.Text
open JetBrains
open JetBrains.Application.changes
open JetBrains.DocumentManagers
open JetBrains.DocumentManagers.impl
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Common.Util.CommonUtil
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Modules
open JetBrains.ReSharper.Resources.Shell

type FSharpSource =
    { Source: byte[]
      Timestamp: DateTime }

[<SolutionComponent>]
type FSharpSourceCache(lifetime, changeManager: ChangeManager, documentManager: DocumentManager) as this =
    let files = ConcurrentDictionary()
    do
        changeManager.RegisterChangeProvider(lifetime, this)
        changeManager.AddDependency(lifetime, this, documentManager.ChangeProvider)

    let getText (document: IDocument) = Encoding.UTF8.GetBytes(document.GetText()) 

    let tryAddSource (path: Util.FileSystemPath) =
        let mutable source = None
        ReadLockCookie.TryExecute(fun _ ->
            match files.TryGetValue(path) with
            | true, value -> source <- Some value
            | _ ->
                documentManager.GetOrCreateDocument(path)
                |> Option.ofObj
                |> Option.iter (fun document ->
                    let timestamp = File.GetLastWriteTimeUtc(path.FullPath)
                    source <- Some { Source = getText document; Timestamp = timestamp }
                    files.[path] <- source.Value)) |> ignore
        source

    member x.GetTimestamp(path) =
        match files.TryGetValue(path) with
        | true, value -> Some value.Timestamp
        | _ -> tryAddSource path |> Option.map (fun s -> s.Timestamp)

    member x.GetSourceStream(path) =
        let getStream source = new MemoryStream(source.Source) :> Stream
        match files.TryGetValue(path) with
        | true, value -> Some (getStream value)
        | _ -> tryAddSource path |> Option.map getStream

    member x.Exists(path) =
        match files.TryGetValue(path) with
        | true, _ -> Some true
        | _ -> None

    interface IChangeProvider with
        member x.Execute(changeMap) =
            let change = changeMap.GetChange<ProjectFileDocumentCopyChange>(documentManager.ChangeProvider)
            if isNotNull change then
                let file = change.ProjectFile
                match file.LanguageType with
                | :? FSharpProjectFileType
                | :? FSharpScriptProjectFileType ->
                     files.[file.Location] <- { Source = getText change.Document; Timestamp = DateTime.UtcNow }
                | _ -> ()
            null
