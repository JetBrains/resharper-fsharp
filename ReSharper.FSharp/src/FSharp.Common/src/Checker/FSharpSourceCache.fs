namespace JetBrains.ReSharper.Plugins.FSharp.Common.Checker

open System
open System.Collections.Generic
open System.Text
open JetBrains.DataFlow
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ProjectModel
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.Caches
open JetBrains.ReSharper.Psi.Modules
open JetBrains.Util

type FSharpSource =
    {
        Source: byte[]
        Timestamp: DateTime
    }

[<SolutionComponent>]
type FSharpSourceCache(lifetime: Lifetime, logger: ILogger) =
    let files = Dictionary<FileSystemPath, FSharpSource>()
    do lifetime.AddAction(fun _ -> files.Clear()) |> ignore

    let canHandle (sourceFile: IPsiSourceFile) =
        sourceFile.PrimaryPsiLanguage.Is<FSharpLanguage>() &&
        sourceFile.PsiModule :? IProjectPsiModule &&
        sourceFile.Properties.ProvidesCodeModel

    let update (sourceFile: IPsiSourceFile) path timestamp =
        let source = Encoding.UTF8.GetBytes(sourceFile.Document.GetText())
        files.[path] <- { Source = source; Timestamp = timestamp }

    member x.GetSource(path: FileSystemPath) =
        lock files (fun _ ->
            let source = ref Unchecked.defaultof<FSharpSource>
            match files.TryGetValue(path, source), !source with
            | true, source -> Some source
            | _ -> None
        )

    interface IPsiSourceFileInvalidatingCache with
        member x.Build(sourceFile, _) =
            if canHandle sourceFile then
                lock files (fun _ ->
                    let path = sourceFile.GetLocation()
                    update sourceFile path (System.IO.File.GetLastWriteTime(path.FullPath)))
            null

        member x.OnDocumentChange(sourceFile, _) =
            if canHandle sourceFile then
                lock files (fun _ -> update sourceFile (sourceFile.GetLocation()) DateTime.Now)

        member x.OnPsiChange(_, _) = ()
        member x.Invalidate(_) = ()
        member x.HasDirtyFiles = false
        member x.MarkAsDirty(_) = ()
        member x.MergeLoaded(_) = ()
        member x.SyncUpdate(_) =()
        member x.Drop(_) = ()
        member x.UpToDate(_) = true
        member x.Load(_, _) = null
        member x.Save(_, _) = ()
        member x.Dump(_, _) = ()
        member x.Merge(_, _) = ()
