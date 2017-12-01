namespace JetBrains.ReSharper.Plugins.FSharp.Common.Checker

open System
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

type FSharpSource =
    {
        Source: byte[]
        Timestamp: DateTime
    }

[<SolutionComponent>]
type FSharpSourceCache(lifetime, changeManager: ChangeManager, documentManager: DocumentManager) as this =
    let files = ConcurrentDictionary()
    do
        changeManager.RegisterChangeProvider(lifetime, this)
        changeManager.AddDependency(lifetime, this, documentManager.ChangeProvider)

    let update (document: IDocument) path =
        let source = Encoding.UTF8.GetBytes(document.GetText())
        files.[path] <- { Source = source; Timestamp = DateTime.UtcNow }

    member x.GetSource(path: Util.FileSystemPath) =
        match files.TryGetValue(path) with
        | true, source -> Some source
        | _ -> None

    interface IChangeProvider with
        member x.Execute(changeMap) =
            let change = changeMap.GetChange<ProjectFileDocumentCopyChange>(documentManager.ChangeProvider)
            if isNotNull change then
                let file = change.ProjectFile
                match file.LanguageType with
                | :? FSharpProjectFileType
                | :? FSharpScriptProjectFileType ->
                     update change.Document file.Location
                | _ -> ()
            null
