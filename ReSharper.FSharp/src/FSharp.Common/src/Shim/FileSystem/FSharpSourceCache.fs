namespace JetBrains.ReSharper.Plugins.FSharp.Shim.FileSystem

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
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Resources.Shell

type FSharpSource =
    | Exists of Source: byte[] * Timestamp: DateTime
    | NotExists

    member x.ToRdFSharpSource() =
        match x with
        | Exists(source, timestamp) -> RdFSharpSource(Encoding.UTF8.GetString(source), timestamp)
        | _ -> RdFSharpSource("NotExists", DateTime.MinValue)

[<SolutionComponent>]
type FSharpSourceCache(lifetime: Lifetime, solution: ISolution, changeManager, documentManager: DocumentManager,
        solutionDocumentChangeProvider: SolutionDocumentChangeProvider, fileExtensions: IProjectFileExtensions,
        logger: ILogger) =
    inherit FileSystemShimChangeProvider(Lifetime.Define(lifetime).Lifetime, changeManager)

    let [<Literal>] RemoveFileChangeType =
        ProjectModelChangeType.REMOVED ||| ProjectModelChangeType.MOVED_OUT

    let [<Literal>] UpdateFileChangeType =
        ProjectModelChangeType.EXTERNAL_CHANGE ||| ProjectModelChangeType.ADDED ||| ProjectModelChangeType.MOVED_IN

    let files = ConcurrentDictionary<VirtualFileSystemPath, FSharpSource>()

    let getText (document: IDocument) =
        Encoding.UTF8.GetBytes(document.GetText())

    let tryAddSource (path: VirtualFileSystemPath) =
        let mutable source = None
        ReadLockCookie.TryExecute(fun _ ->
            match files.TryGetValue(path) with
            | true, value -> source <- Some value
            | _ ->

            let document = documentManager.GetOrCreateDocument(path)
            if isNull document then () else

            logger.Trace("Add: tryAddSource: {0}", path)
            source <-
                if path.ExistsFile then
                    Some(Exists(getText document, File.GetLastWriteTimeUtc(path.FullPath)))
                else
                    Some(NotExists)

            files[path] <- source.Value
        ) |> ignore

        source

    let isApplicable (path: VirtualFileSystemPath) =
        // todo: support FCS fake paths like `startup`, prevent going to FS to check existence, etc.
        not path.IsEmpty && path.IsAbsolute && fileExtensions.GetFileType(path).Is<FSharpProjectFileType>()

    let applyChange (projectFile: IProjectFile) (document: IDocument) changeSource =
        let path = projectFile.Location
        if not (isApplicable path) then () else

        let text = getText document

        let mutable fsSource = Unchecked.defaultof<_>
        match files.TryGetValue(path, &fsSource), fsSource with
        | true, Exists(source, _) when source = text -> ()
        | _ ->

        logger.Trace("Add: {0} change: {1}", changeSource, path)
        files[path] <- Exists(text, DateTime.UtcNow)

    member x.TryGetSource(path: VirtualFileSystemPath, [<Out>] source: byref<FSharpSource>) =
        match files.TryGetValue(path) with
        | true, value -> source <- value; true
        | _ ->

        match tryAddSource path with
        | Some v -> source <- v; true
        | _ -> false

    override this.ReadFile(path, useMemoryMappedFile, shouldShadowCopy) =
        if not (isApplicable path) then base.ReadFile(path, useMemoryMappedFile, shouldShadowCopy) else

        match this.TryGetSource(path) with
        | true, Exists(source, _) -> new MemoryStream(source) :> _
        | true, NotExists -> failwithf $"Reading not existing file: {path}"
        | _ ->

        logger.Trace("Miss: FileStreamReadShim miss: {0}", path)
        base.ReadFile(path, useMemoryMappedFile, shouldShadowCopy)

    override x.GetLastWriteTime(path) =
        if not (isApplicable path) then base.GetLastWriteTime(path) else

        match x.TryGetSource(path) with
        | true, Exists(_, timestamp) -> timestamp
        | true, NotExists -> FileNotFoundException($"GetLastWriteTime: NotExists: {path}") |> raise
        | _ ->

        logger.Trace("GetLastWriteTime: miss: {0}", path)
        base.GetLastWriteTime(path)

    override x.ExistsFile(path) =
        if not (isApplicable path) then base.ExistsFile(path) else

        match files.TryGetValue(path) with
        | true, Exists _ -> true
        | true, NotExists -> false
        | _ ->

        match tryAddSource path with
        | Some (Exists _) -> true
        | Some NotExists -> false
        | _ ->

        logger.Trace("Miss: Exists: {0}", path)
        base.ExistsFile(path)

    member x.ProcessDocumentChange(change: DocumentChange) =
        let projectFile =
            match change with
            | :? ProjectFileDocumentChange as change -> change.ProjectFile
            | :? ProjectFileDocumentCopyChange as change -> change.ProjectFile
            | _ -> null

        if isNotNull projectFile && projectFile.LanguageType.Is<FSharpProjectFileType>() then
             applyChange projectFile change.Document "Document"

    member x.ProcessProjectModelChange(change: ProjectModelChange) =
        if isNull change then () else

        let visitor =
            { new RecursiveProjectModelChangeDeltaVisitor() with
                override v.VisitItemDelta(change) =
                    base.VisitItemDelta(change)

                    if change.ContainsChangeType(RemoveFileChangeType) then
                         files.TryRemove(change.OldLocation) |> ignore

                    if change.ContainsChangeType(UpdateFileChangeType) then
                        let projectFile = change.ProjectItem.As<IProjectFile>()
                        if isValid projectFile && projectFile.LanguageType.Is<FSharpProjectFileType>() then
                            let document = projectFile.GetDocument()
                            if isNotNull document then
                                applyChange projectFile document "Project model" }

        visitor.VisitDelta(change)

    override x.Execute(changeEventArgs: ChangeEventArgs) =
        let changeMap = changeEventArgs.ChangeMap
        x.ProcessDocumentChange(changeMap.GetChange<DocumentChange>(solutionDocumentChangeProvider))
        x.ProcessProjectModelChange(changeMap.GetChange<ProjectModelChange>(solution))

    member x.GetRdFSharpSource(fileName: string): RdFSharpSource =
        let path = VirtualFileSystemPath.TryParse(fileName, InteractionContext.SolutionContext)
        let mutable fsSource = Unchecked.defaultof<FSharpSource>
        if x.TryGetSource(path, &fsSource) then fsSource.ToRdFSharpSource() else null
