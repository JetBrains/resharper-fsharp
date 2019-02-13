namespace rec JetBrains.ReSharper.Plugins.FSharp.Common.Shim.FileSystem

open System
open System.IO
open System.Collections.Concurrent
open System.Runtime.InteropServices
open System.Text
open JetBrains.Application.changes
open JetBrains.Diagnostics
open JetBrains.DocumentManagers
open JetBrains.DocumentManagers.impl
open JetBrains.DocumentModel
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Common.Util.CommonUtil
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase
open JetBrains.ReSharper.Resources.Shell
open JetBrains.Rider.Model

type FSharpSource =
    { Source: byte[]
      Timestamp: DateTime }

    member x.ToRdFSharpSource() =
        RdFSharpSource(Encoding.UTF8.GetString(x.Source), x.Timestamp)

[<SolutionComponent>]
type FSharpSourceCache
        (lifetime: Lifetime, solution: ISolution, changeManager, documentManager: DocumentManager,
         solutionDocumentChangeProvider: SolutionDocumentChangeProvider,
         fileExtensions: IProjectFileExtensions, logger: JetBrains.Util.ILogger) =
    inherit FileSystemShimChangeProvider(Lifetime.Define(lifetime).Lifetime, changeManager)

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
                logger.Verbose("tryAddSource, add: {0}", path)
                files.[path] <- source.Value) |> ignore
        source

    member x.IsApplicable(path: FileSystemPath) =
        fileExtensions.GetFileType(path).Is<FSharpProjectFileType>()

    member x.TryGetSource(path: FileSystemPath, [<Out>] source: byref<FSharpSource>) =
        match files.TryGetValue(path) with
        | true, value -> source <- value; true
        | _ ->

        match tryAddSource path with
        | Some v -> source <- v; true
        | _ -> false

    override x.FileStreamReadShim(fileName) =
        let path = FileSystemPath.TryParse(fileName)
        if path.IsEmpty || not (x.IsApplicable(path)) then base.FileStreamReadShim(fileName) else

        match x.TryGetSource(path) with
        | true, source -> new MemoryStream(source.Source) :> Stream
        | _ ->
            logger.Warn("FileStreamReadShim miss: {0}", path)
            base.FileStreamReadShim(fileName)

    override x.GetLastWriteTime(path) =
        if not (x.IsApplicable(path)) then base.GetLastWriteTime(path) else

        match x.TryGetSource(path) with
        | true, source -> source.Timestamp
        | _ ->
            logger.Warn("GetLastWriteTime miss: {0}", path)
            base.GetLastWriteTime(path)

    override x.Exists(path) =
        match files.TryGetValue(path) with
        | true, _ -> true
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

                        logger.Verbose("ProcessProjectModelChange, add: {0}", path)
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
