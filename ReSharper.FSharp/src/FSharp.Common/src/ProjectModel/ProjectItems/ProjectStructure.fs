module JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectItems.ProjectStructure

open System
open System.Collections.Generic
open JetBrains.ProjectModel
open JetBrains.ReSharper.Host.Features.ProjectModel.View
open JetBrains.ReSharper.Host.Features.ProjectModel.View.Appenders.ProjectStructure
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectItems.ItemsContainer
open JetBrains.Rider.Model
open JetBrains.Util

[<SolutionComponent>]
type FSharpProjectStructureProvider(container: IFSharpItemsContainer) =
    interface IProjectStructureProvider with
        member x.Priority = 10

        member x.Process(projectItem) =
            if not (container.IsApplicable(projectItem)) then null else

            let getParent defaultParent =
                Option.map box >> Option.defaultValue defaultParent

            match projectItem with
            | :? IProjectFolder as projectFolder ->
                let parentFolder = projectFolder.ParentFolder.NotNull()
    
                container.CreateFoldersWithParents(projectFolder)
                |> List.map (fun (viewFolder, parentOpt) ->
                    ProjectStructureItem(viewFolder, parentOpt |> getParent parentFolder)) :> _
    
            | :? IProjectFile as projectFile ->
                let item = FSharpViewFile projectFile
                let parentFolder = projectFile.ParentFolder.NotNull()
    
                let parentViewFolder =
                    container.TryGetParentFolderIdentity(item)
                    |> Option.map (fun identity -> FSharpViewFolder (parentFolder, identity))
                    |> getParent parentFolder
                [ProjectStructureItem(item, parentViewFolder)] :> _
    
            | _ -> null


[<SolutionInstanceComponent>]
type FSharpProjectStructurePresenter(host: ProjectModelViewHost, container: IFSharpItemsContainer) =

    let presentItem (item: FSharpViewItem): RdProjectModelItemDescriptor =
        let key = container.TryGetSortKey(item) |> Option.toNullable
        match item with
        | FSharpViewFile file ->
            let userData =
                file.Properties.GetBuildActions()
                |> Seq.tryHead
                |> Option.bind (fun (Pair (_, action)) ->
                    if action.ChangesOrder() then Some (dict ["FSharpCompileType", action.ToString()])
                    else None)
                |> Option.toObj
            ProjectModelViewPresenter.PresentProjectFile(file, sortKey = key, userData = userData) :> _

        | FSharpViewFolder (folder, _) ->
            ProjectModelViewPresenter.PresentProjectFolder(folder, sortKey = key) :> _

    do
        host.Present<FSharpViewItem>(Func<_,_>(presentItem))
