module JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Host.ProjectItems.ProjectStructure

open System
open JetBrains.Diagnostics
open JetBrains.ProjectModel
open JetBrains.RdBackend.Common.Features.ProjectModel.View
open JetBrains.RdBackend.Common.Features.ProjectModel.View.Appenders.ProjectStructure
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Host.ProjectItems.ItemsContainer
open JetBrains.Rider.Model

[<SolutionComponent>]
type FSharpProjectStructureProvider(container: IFSharpItemsContainer) =
    interface IProjectStructureProvider with
        member x.Priority = 10

        member x.Process(projectItem) =
            match projectItem.GetProject() with
            | null -> null
            | project when not project.IsFSharp -> null
            | _ ->

            let getParent defaultParent =
                Option.map box >> Option.defaultValue defaultParent

            match projectItem with
            | :? IProjectFolder as projectFolder ->
                let parentFolder = projectFolder.ParentFolder.NotNull()

                container.CreateFoldersWithParents(projectFolder)
                |> List.ofSeq
                |> List.map (fun (viewFolder, parentOpt) ->
                    ProjectStructureItem(viewFolder, parentOpt |> getParent parentFolder)) :> _

            | :? IProjectFile as projectFile ->
                let item = FSharpViewFile projectFile
                let parentFolder = projectFile.ParentFolder.NotNull()

                let parentViewFolder =
                    container.TryGetParentFolderIdentity(item)
                    |> Option.map (fun identity -> FSharpViewFolder (parentFolder, identity))
                    |> getParent parentFolder
                [| ProjectStructureItem(item, parentViewFolder) |] :> _

            | _ -> null


[<SolutionInstanceComponent>]
type FSharpProjectStructurePresenter(host: ProjectModelViewHost, container: IFSharpItemsContainer,
        presenter: ProjectModelViewPresenter) =

    let presentItem (item: FSharpViewItem): RdProjectModelItemDescriptor =
        let key = container.TryGetSortKey(item) |> Option.toNullable
        match item with
        | FSharpViewFile file ->
            presenter.PresentProjectFile(file, sortKey = key) :> _

        | FSharpViewFolder (folder, _) ->
            presenter.PresentProjectFolder(folder, sortKey = key) :> _

    do
        host.Present<FSharpViewItem>(Func<_,_>(presentItem))
