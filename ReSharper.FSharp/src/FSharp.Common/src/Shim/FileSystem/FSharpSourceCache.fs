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
open JetBrains.Rider.Model
open Microsoft.FSharp.Compiler.AbstractIL.Internal.Library

type FSharpSource =
    { Source: byte[]
      Timestamp: DateTime }

    member x.ToRdFSharpSource() =
        RdFSharpSource(Encoding.UTF8.GetString(x.Source), x.Timestamp)

[<SolutionComponent>]
type FSharpSourceCache
        (lifetime: Lifetime, solution: ISolution, changeManager, documentManager: DocumentManager,
         extensions: IProjectFileExtensions, solutionDocumentChangeProvider: SolutionDocumentChangeProvider) =
    inherit FileSystemShimChangeProvider(Lifetimes.Define(lifetime).Lifetime, Shim.FileSystem, changeManager)

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

    member x.ProcessDocumentChange(change: DocumentChange) =
        let projectFile =
            match change with
            | :? ProjectFileDocumentChange as change -> change.ProjectFile
            | :? ProjectFileDocumentCopyChange as change -> change.ProjectFile
            | _ -> null

        match projectFile with
        | null -> ()
        | file ->

        if file.LanguageType.Is<FSharpProjectFileType>() then
             files.[file.Location] <- { Source = getText change.Document; Timestamp = DateTime.UtcNow }

    member x.ProcessProjectModelChange(change: ProjectModelChange) =
        if isNull change then () else
        let visitor =
            { new RecursiveProjectModelChangeDeltaVisitor() with
                override v.VisitItemDelta(change) =
                    base.VisitItemDelta(change)
                    if not (change.ContainsChangeType(ProjectModelChangeType.EXTERNAL_CHANGE)) then () else

                    match change.ProjectItem with
                    | :? IProjectFile as file when file.LanguageType.Is<FSharpProjectFileType>() ->
                        match file.GetDocument() with
                        | null -> ()
                        | document ->

                        let path = file.Location
                        let text = getText document

                        let mutable fsSource = Unchecked.defaultof<_>
                        if files.TryGetValue(path, &fsSource) && text = fsSource.Source then () else

                        files.[path] <- { Source = text; Timestamp = DateTime.UtcNow }
                    | _ -> () }

        visitor.VisitDelta(change)

    override x.Execute(changeEventArgs: ChangeEventArgs) =
        let changeMap = changeEventArgs.ChangeMap
        x.ProcessDocumentChange(changeMap.GetChange<DocumentChange>(solutionDocumentChangeProvider))
        x.ProcessProjectModelChange(changeMap.GetChange<ProjectModelChange>(solution))

    member x.GetRdFSharpSource(fileName: string): RdFSharpSource =
        let path = FileSystemPath.TryParse(fileName)
        let mutable fsSource = Unchecked.defaultof<FSharpSource>
        if x.TryGetSource(path, &fsSource) then fsSource.ToRdFSharpSource() else null
