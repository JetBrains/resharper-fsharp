module JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Host.ProjectItems.ProjectStructure

open System
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Parts
open JetBrains.Diagnostics
open JetBrains.ProjectModel
open JetBrains.RdBackend.Common.Env
open JetBrains.RdBackend.Common.Features.ProjectModel.View
open JetBrains.RdBackend.Common.Features.ProjectModel.View.Appenders.ProjectStructure
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Host.ProjectItems.ItemsContainer
open JetBrains.Rider.Model

[<SolutionComponent(InstantiationEx.LegacyDefault)>]
[<ZoneMarker(typeof<IReSharperHostNetFeatureZone>)>]
type FSharpProjectStructureProvider(container: IFSharpItemsContainer) =
    interface IProjectStructureProvider with
        member x.Priority = 10

        member x.Process(projectItem) =
            match projectItem.GetProject() with
            | null -> null
            | project when not project.IsFSharp -> null
            | _ ->

            match projectItem with
            | :? IProjectFolder as projectFolder ->
                let parentFolder = projectFolder.ParentFolder.NotNull()

                container.CreateFoldersWithParents(projectFolder)
                |> List.ofSeq
                |> List.map (fun (viewFolder, parentOpt) ->
                    let parent = parentOpt |> Option.map box |> Option.defaultValue (box parentFolder)
                    ProjectStructureItem(viewFolder, parent)) :> _

            | :? IProjectFile as projectFile ->
                let item = FSharpViewFile projectFile
                [| ProjectStructureItem(item, getViewParent container item) |] :> _

            | _ -> null


[<SolutionInstanceComponent(InstantiationEx.LegacyDefault)>]
[<ZoneMarker(typeof<IReSharperHostNetFeatureZone>)>]
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
