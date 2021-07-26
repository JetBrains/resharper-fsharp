namespace rec JetBrains.ReSharper.Plugins.FSharp.Shim.FileSystem

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
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Resources.Shell

type FSharpSource =
    { Source: byte[]
      Timestamp: DateTime }

    member x.ToRdFSharpSource() =
        RdFSharpSource(Encoding.UTF8.GetString(x.Source), x.Timestamp)

[<SolutionComponent>]
type FSharpSourceCache(lifetime: Lifetime, solution: ISolution, changeManager, documentManager: DocumentManager,
        solutionDocumentChangeProvider: SolutionDocumentChangeProvider, fileExtensions: IProjectFileExtensions,
        logger: ILogger) =
    inherit FileSystemShimChangeProvider(Lifetime.Define(lifetime).Lifetime, changeManager)

    let files = ConcurrentDictionary<VirtualFileSystemPath, FSharpSource>()

    let getText (document: IDocument) =
        Encoding.UTF8.GetBytes(document.GetText())

    let tryAddSource (path: VirtualFileSystemPath) =
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
            logger.Trace("Add: tryAddSource: {0}", path)
            files.[path] <- source.Value) |> ignore

        source

    let isApplicable (path: VirtualFileSystemPath) =
        // todo: support FCS fake paths like `startup`, prevent going to FS to check existence, etc.
        not path.IsEmpty && path.IsAbsolute && fileExtensions.GetFileType(path).Is<FSharpProjectFileType>()

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
        | true, source -> new MemoryStream(source.Source) :> _
        | _ ->

        logger.Trace("Miss: FileStreamReadShim miss: {0}", path)
        base.ReadFile(path, useMemoryMappedFile, shouldShadowCopy)

    override x.GetLastWriteTime(path) =
        if not (isApplicable path) then base.GetLastWriteTime(path) else

        match x.TryGetSource(path) with
        | true, source -> source.Timestamp
        | _ ->

        logger.Trace("Miss: GetLastWriteTime: {0}", path)
        base.GetLastWriteTime(path)

    override x.ExistsFile(path) =
        if not (isApplicable path) then base.ExistsFile(path) else
        match files.TryGetValue(path) with
        | true, _ -> true
        | _ ->

        match tryAddSource path with
        | Some _ -> true
        | _ ->

        logger.Trace("Miss: Exists: {0}", path)
        base.ExistsFile(path)

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

                    if change.ContainsChangeType(ProjectModelChangeType.REMOVED) then
                        files.remove(change.OldLocation)

                    elif change.ContainsChangeType(ProjectModelChangeType.EXTERNAL_CHANGE) then
                        match change.ProjectItem.As<IProjectFile>() with
                        | null -> ()
                        | projectFile when not (projectFile.LanguageType.Is<FSharpProjectFileType>()) -> ()
                        | projectFile ->

                        match projectFile.GetDocument() with
                        | null -> ()
                        | document ->

                        let path = projectFile.Location
                        let text = getText document

                        let mutable fsSource = Unchecked.defaultof<_>
                        if files.TryGetValue(path, &fsSource) && text = fsSource.Source then () else

                        logger.Trace("Add: Project Model change: {0}", path)
                        files.[path] <- { Source = text; Timestamp = DateTime.UtcNow } }

        visitor.VisitDelta(change)

    override x.Execute(changeEventArgs: ChangeEventArgs) =
        let changeMap = changeEventArgs.ChangeMap
        x.ProcessDocumentChange(changeMap.GetChange<DocumentChange>(solutionDocumentChangeProvider))
        x.ProcessProjectModelChange(changeMap.GetChange<ProjectModelChange>(solution))

    member x.GetRdFSharpSource(fileName: string): RdFSharpSource =
        let path = VirtualFileSystemPath.TryParse(fileName, InteractionContext.SolutionContext)
        let mutable fsSource = Unchecked.defaultof<FSharpSource>
        if x.TryGetSource(path, &fsSource) then fsSource.ToRdFSharpSource() else null
