module rec JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectItems.ItemsContainer

open System
open System.Collections.Generic
open System.IO
open System.Linq
open JetBrains.Application
open JetBrains.Application.Components
open JetBrains.Application.DataContext
open JetBrains.Application.PersistentMap
open JetBrains.Application.Threading
open JetBrains.Application.changes
open JetBrains.DataFlow
open JetBrains.Metadata.Reader.API
open JetBrains.Platform.MsBuildHost.Models
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Caches
open JetBrains.ProjectModel.ProjectsHost
open JetBrains.ProjectModel.ProjectsHost.Impl
open JetBrains.ProjectModel.ProjectsHost.MsBuild
open JetBrains.ProjectModel.ProjectsHost.MsBuild.Structure
open JetBrains.ProjectModel.ProjectsHost.SolutionHost
open JetBrains.ReSharper.Feature.Services.Navigation
open JetBrains.ReSharper.Feature.Services.Navigation.NavigationProviders
open JetBrains.ReSharper.Host.Features.ProjectModel.Editing
open JetBrains.ReSharper.Host.Features.ProjectModel.View
open JetBrains.ReSharper.Host.Features.ProjectModel.View.Appenders
open JetBrains.ReSharper.Host.Features.Util.Tree
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Psi
open JetBrains.Threading
open JetBrains.UI.RichText
open JetBrains.Util
open JetBrains.Util.Collections
open JetBrains.Util.DataStructures
open JetBrains.Util.Logging
open JetBrains.Util.PersistentMap

/// Keeps project mappings in solution caches to make the mappings available before MsBuild loads the projects.
[<SolutionInstanceComponent>]
type FSharpItemsContainerLoader(lifetime: Lifetime, solution: ISolution, solutionCaches: ISolutionCaches) =

    abstract GetMap: unit -> IDictionary<IProjectMark, ProjectMapping>
    default x.GetMap() =
        let projectMarks =
            solution.ProjectsHostContainer().GetComponent<ISolutionStructureContainer>().ProjectMarks.ToArray()
            |> Array.map (fun projectMark -> projectMark.UniqueProjectName, projectMark) |> dict

        let dummyProjectMark =
            DummyProjectMark(solution.GetSolutionMark(), String.Empty, Guid.Empty, FileSystemPath.Empty)

        let projectMarkMarshaller =
            { new IUnsafeMarshaller<IProjectMark> with
                member x.Marshal(writer, value) =
                    writer.Write(value.UniqueProjectName)

                member x.Unmarshal(reader) =
                    let uniqueProjectName = reader.ReadString()
                    match projectMarks.TryGetValue(uniqueProjectName) with
                    | null -> dummyProjectMark :> _ // the project mark was removed
                    | projectMark -> projectMark }

        let map =
            solutionCaches.Db
                .GetMap("FSharpItemsContainer", projectMarkMarshaller, ProjectMapping.Marshaller)
                .ToOptimized(lifetime)

        map.Remove(dummyProjectMark) |> ignore
        map :> _


/// Keeps project items in proper order and is used in creating FCS project options and F# project tree.
[<SolutionInstanceComponent>]
type FSharpItemsContainer
        (logger: ILogger, containerLoader: FSharpItemsContainerLoader, refresher: IFSharpItemsContainerRefresher) =

    let locker = JetFastSemiReenterableRWLock()
    let projectMappings = lazy (containerLoader.GetMap())
    let targetFrameworkIdsIntern = DataIntern(setComparer)

    let tryGetProjectMark (projectItem: IProjectItem) =
        match projectItem.GetProject() with
        | null -> None
        | project ->
            match project.GetProjectMark() with
            | null -> None
            | projectMark -> Some projectMark

    let tryGetProjectMapping (projectMark: IProjectMark): ProjectMapping option =
        tryGetValue projectMark projectMappings.Value

    let tryGetProjectItem (viewItem: FSharpViewItem) =
        tryGetProjectMark viewItem.ProjectItem
        |> Option.bind tryGetProjectMapping
        |> Option.bind (fun mapping -> mapping.TryGetProjectItem(viewItem))

    let getItems (msBuildProject: MsBuildProject) itemTypeFilter getItemsByName allowNonDefaultItemType =
        let items = List<RdProjectItemWithTargetFrameworks>()
        for rdProject in msBuildProject.RdProjects do
            let targetFrameworkId = msBuildProject.GetTargetFramework(rdProject)
            let filter = MsBuildItemTypeFilter(rdProject)

            rdProject.Items
            |> Seq.filter (fun item ->
                // todo: allow passing additional default item types to the filter ctor
                itemTypeFilter item.ItemType && allowNonDefaultItemType ||
                not (filter.FilterByItemType(item.ItemType, item.IsImported())))
            |> Seq.filter (fun item ->
                let (BuildAction buildAction) = item.ItemType
                not (buildAction.IsNone() && getItemsByName item.EvaluatedInclude |> Seq.length > 1))
            |> List.ofSeq
            |> List.filter (fun item -> itemTypeFilter item.ItemType)
            |> List.fold (fun index item ->
                if index < items.Count && items.[index].Item.EvaluatedInclude = item.EvaluatedInclude then
                    items.[index].TargetFrameworkIds.Add(targetFrameworkId) |> ignore
                    index + 1
                else
                    let mutable tmpIndex = index + 1
                    while tmpIndex < items.Count && items.[tmpIndex].Item.EvaluatedInclude <> item.EvaluatedInclude do
                        tmpIndex <- tmpIndex + 1

                    if tmpIndex >= items.Count then
                        items.Insert(index, { Item = item; TargetFrameworkIds = HashSet([targetFrameworkId]) })
                        index + 1
                    else
                        items.[tmpIndex].TargetFrameworkIds.Add(targetFrameworkId) |> ignore
                        tmpIndex + 1) 0 |> ignore
        items

    let createRefreshers projectMark =
        let mutable folderToRefresh = None
        let itemsToUpdate = HashSet<FSharpProjectItem>()

        let refreshFolder folder =
            match folderToRefresh, folder with
            | None, _ | _, Project -> folderToRefresh <- Some folder
            | Some (ProjectItem existingFolder), (ProjectItem newFolder) when
                    newFolder.LogicalPath.IsPrefixOf(existingFolder.LogicalPath) -> folderToRefresh <- Some folder
            | _ -> ()

        let updateItem item =
            itemsToUpdate.Add(item) |> ignore

        let refresh () =
            match folderToRefresh with
                | Some Project -> refresher.Refresh(projectMark, false)
                | Some (ProjectItem (FolderItem (_, id) as folder)) ->
                    refresher.Refresh(projectMark, folder.LogicalPath, id)
                | _ -> ()
            for item in itemsToUpdate do
                match item with
                | FileItem _ -> refresher.Update(projectMark, item.ItemInfo.LogicalPath)
                | FolderItem (_, id) -> refresher.Update(projectMark, item.ItemInfo.LogicalPath, id)

        refreshFolder, updateItem, refresh

    let updateProject projectMark update =
        use lock = locker.UsingWriteLock()
        let projectMappings = projectMappings.Value
        tryGetValue projectMark projectMappings
        |> Option.iter (fun mapping ->
            let refresher, updater, refresh = createRefreshers projectMark
            update mapping refresher updater
            projectMappings.[projectMark] <- mapping
            refresh ())

    member private x.ProjectMappings = projectMappings.Value

    member x.IsValid(viewItem: FSharpViewItem) =
        use lock = locker.UsingReadLock()
        tryGetProjectItem viewItem |> Option.isSome

    member x.RemoveProject(projectMark: IProjectMark) =
        use lock = locker.UsingWriteLock()
        x.ProjectMappings.Remove(projectMark) |> ignore

    interface IFSharpItemsContainer with
        member x.OnProjectLoaded(projectMark, msBuildProject) =
            match msBuildProject with
            | null ->
                use lock = locker.UsingWriteLock()
                x.ProjectMappings.Remove(projectMark) |> ignore
            | _ ->

            match projectMark with
            | FSharProjectMark ->
                let itemsByName = OneToListMap()
                for rdProject in msBuildProject.RdProjects do
                    for rdItem in rdProject.Items do
                        itemsByName.Add(rdItem.EvaluatedInclude, rdItem.ItemType)

                let getItemsByName name =
                    itemsByName.GetValuesSafe(name)

                let compileBeforeItems = getItems msBuildProject isCompileBefore getItemsByName true
                let compileAfterItems = getItems msBuildProject isCompileAfter getItemsByName true 
                let restItems = getItems msBuildProject (changesOrder >> not) getItemsByName false
                let items =
                    compileBeforeItems.Concat(restItems).Concat(compileAfterItems)
                    |> Seq.map (fun item ->
                        { item with TargetFrameworkIds = targetFrameworkIdsIntern.Intern(item.TargetFrameworkIds) })
                    |> List.ofSeq
                let targetFrameworkIds = HashSet(msBuildProject.TargetFrameworkIds)

                begin
                use lock = locker.UsingWriteLock()
                x.ProjectMappings.[projectMark] <-
                    let projectDirectory = projectMark.Location.Directory
                    let projectUniqueName = projectMark.UniqueProjectName
                    let mapping = ProjectMapping(projectDirectory, projectUniqueName, targetFrameworkIds, logger)
                    mapping.Update(items)
                    mapping
                end

                refresher.Refresh(projectMark, true)
            | _ -> ()

        member x.OnAddFile(projectMark, itemType, path, linkedPath, relativeTo, relativeToType) =
            logger.Trace("On add file: {0} ({1}, link: {2}) relative to: {3} {4}",
                path, itemType, linkedPath, relativeTo, relativeToType)
            updateProject projectMark (fun mapping refresher updater ->
                let logicalPath = if isNotNull linkedPath then linkedPath else path
                let relativeToType = Option.ofNullable relativeToType
                mapping.AddFile(itemType, path, logicalPath, relativeTo, relativeToType, refresher, updater))
            refresher.SelectItem(projectMark, path)

        member x.OnRemoveFile(projectMark, itemType, path) =
            logger.Trace("On remove file: {0} ({1})", path, itemType)
            updateProject projectMark (fun mapping refresher updater ->
                mapping.RemoveFile(path, refresher, updater))

        member x.OnUpdateFile(projectMark, oldItemType, oldLocation, newItemType, newLocation) =
            logger.Trace("On update file: {0} ({1}) to {2} ({3})", oldLocation, oldItemType, newLocation, newItemType)
            if not (equalsIgnoreCase oldItemType newItemType) &&
                    (changesOrder oldItemType || changesOrder newItemType) then
                refresher.ReloadProject(projectMark) else

            updateProject projectMark (fun mapping _ updater ->
                mapping.UpdateFile(oldItemType, oldLocation, newItemType, newLocation)
                refresher.Update(projectMark, newLocation))

        member x.OnUpdateFolder(projectMark, oldLocation, newLocation) =
            logger.Trace("On update folder: {0} to {1}", oldLocation, newLocation)
            if oldLocation <> newLocation then
                updateProject projectMark (fun mapping _ updater ->
                    mapping.UpdateFolder(oldLocation, newLocation, updater))

        member x.OnAddFolder(projectMark, path, relativeTo, relativeToType) =
            logger.Trace("On add folder: {0} relative to {1} {2}", path, relativeTo, relativeToType)
            updateProject projectMark (fun mapping refresher updater ->
                mapping.AddFolder(path, relativeTo, Option.ofNullable relativeToType, refresher, updater))

        member x.OnRemoveFolder(projectMark, path) =
            logger.Trace("On remove file: {0}", path)
            updateProject projectMark (fun mapping refresher updater ->
                mapping.RemoveFolder(path, refresher, updater))

        member x.CreateFoldersWithParents(folder: IProjectFolder) =
            use lock = locker.UsingReadLock()
            tryGetProjectMark folder
            |> Option.bind tryGetProjectMapping
            |> Option.map (fun mapping ->
                mapping.TryGetProjectItems(folder.Location)
                |> Seq.map (function
                    | FolderItem (_, id) as folderItem ->
                        let parent =
                            match folderItem.Parent with
                            | ProjectItem (FolderItem (_, id)) -> Some (FSharpViewFolder (folder.ParentFolder, id))
                            | _ -> None
                        FSharpViewFolder (folder, id), parent
                    | item -> sprintf "got item %O" item |> failwith)
                |> List.ofSeq)
            |> Option.defaultValue []

        member x.TryGetRelativeChildPath(projectMark, modifiedItem, relativeItem, relativeToType) =
            use lock = locker.UsingReadLock()
            tryGetValue projectMark x.ProjectMappings
            |> Option.bind (fun mapping ->
                mapping.TryGetRelativeChildPath(modifiedItem, relativeItem, relativeToType))

        member x.TryGetParentFolderIdentity(viewFile: FSharpViewItem): FSharpViewFolderIdentity option =
            use lock = locker.UsingReadLock()
            tryGetProjectMark viewFile.ProjectItem
            |> Option.bind tryGetProjectMapping 
            |> Option.bind (fun mapping ->
                mapping.TryGetProjectItem(viewFile)
                |> Option.bind (fun item ->
                    match item.Parent with
                    | ProjectItem (FolderItem (_, id)) -> Some id
                    | _ -> None))

        member x.Dump(writer: TextWriter) =
            use lock = locker.UsingReadLock()
            for KeyValuePair (projectMark, mapping) in x.ProjectMappings do
                writer.WriteLine(projectMark.Name)
                mapping.Dump(writer)

        member x.TryGetSortKey(viewItem: FSharpViewItem) =
            use lock = locker.UsingReadLock()
            tryGetProjectItem viewItem |> Option.map (fun item -> item.SortKey)

        member x.IsApplicable(projectItem) =
            use lock = locker.UsingReadLock()
            tryGetProjectMark projectItem
            |> Option.bind tryGetProjectMapping
            |> Option.isSome

        member x.GetProjectItemsPaths(projectMark, targetFrameworkId) =
            tryGetValue projectMark x.ProjectMappings
            |> Option.map (fun mapping -> mapping.GetProjectItemsPaths(targetFrameworkId))
            |> Option.defaultValue [| |]

type IFSharpItemsContainer =
    inherit IMsBuildProjectListener
    inherit IMsBuildProjectModificationListener

    abstract member IsApplicable: IProjectItem -> bool
    abstract member TryGetSortKey: FSharpViewItem -> int option
    abstract member TryGetParentFolderIdentity: FSharpViewItem -> FSharpViewFolderIdentity option
    abstract member CreateFoldersWithParents: IProjectFolder -> (FSharpViewItem * FSharpViewItem option) list
    abstract member GetProjectItemsPaths: IProjectMark * TargetFrameworkId -> (FileSystemPath * BuildAction)[]
    abstract member Dump: TextWriter -> unit

    abstract member TryGetRelativeChildPath:
            IProjectMark * modifiedItem: FSharpViewItem * relativeItem: FSharpViewItem * RelativeToType ->
            (FileSystemPath * RelativeToType) option


type ProjectMapping(projectDirectory, projectUniqueName, targetFrameworkIds: ISet<_>, logger: ILogger) =

    // Files and folders by physical path.
    // For now we assume that a file is only included to a single item type group.
    let files = Dictionary<FileSystemPath, FSharpProjectItem>()
    let folders = OneToListMap<FileSystemPath, FSharpProjectItem>()

    let tryGetFile path =
        tryGetValue path files

    let getFolders path =
        folders.GetValuesSafe(path) |> List.ofSeq

    let getItemsForPath path =
        tryGetFile path
        |> Option.toList
        |> List.append (getFolders path)

    let tryGetProjectItem (viewItem: FSharpViewItem) =
        let path = viewItem.ProjectItem.Location
        match viewItem with
        | FSharpViewFile _ -> tryGetFile path
        | FSharpViewFolder (_, identity) as viewFolder ->
            getFolders path
            |> List.tryFind (function | FolderItem (_, id) -> id = identity | _ -> false)

    let getNewFolderIdentity path =
        { Identity = (getFolders path |> List.length) + 1 }

    let getChildren (parent: FSharpProjectModelElement) =
        folders.Values
        |> Seq.append files.Values
        |> Seq.filter (fun item -> item.Parent = parent)
        |> Seq.sortBy (fun x -> x.SortKey)

    let getNewSortKey parent =
        getChildren parent |> Seq.length |> (+) 1

    let moveFollowingItems parent sortKeyFrom direction updateItem =
        getChildren parent
        |> Seq.iter (fun item ->
            if item.SortKey >= sortKeyFrom then
                item.ItemInfo.SortKey <-
                    match direction with
                    | MoveDirection.Up -> item.SortKey - 1
                    | MoveDirection.Down -> item.SortKey + 1
                updateItem item)

    let addFolder parent sortKey path updateItem =
        let folderItem = FolderItem(ItemInfo.Create(path, path, parent, sortKey), getNewFolderIdentity path)
        moveFollowingItems parent sortKey MoveDirection.Down updateItem
        folders.Add(path, folderItem)
        ProjectItem folderItem

    let getOrCreateFolder folderRefresher parent path =
        folders.GetValuesSafe(path)
        |> Seq.sortBy (fun item -> item.SortKey)
        |> Seq.tryLast
        |> Option.defaultWith (fun _ ->
            folderRefresher parent

            let itemInfo = ItemInfo.Create(path, path, parent, getNewSortKey parent)
            let folderItem = FolderItem(itemInfo, getNewFolderIdentity path)
            folders.Add(path, folderItem) 
            folderItem)
        |> ProjectItem

    let (|EmptyFolder|_|) projectItem =
        match projectItem with
        | FolderItem _ when getChildren (ProjectItem projectItem) |> Seq.isEmpty -> Some projectItem
        | _ -> None

    let getNewRelativeSortKey (item: FSharpProjectItem) relativeToType =
        match relativeToType with
        | RelativeToType.Before -> item.SortKey
        | RelativeToType.After -> item.SortKey + 1
        | _ -> relativeToType |> sprintf "Got relativeToType %O" |> failwith

    let canBeRelative (projectItem: FSharpProjectItem) (modifiedItem: FSharpProjectItem option) =
        match projectItem, modifiedItem with
        | FileItem _, None -> true
        | FileItem (_, buildAction, _), Some (FileItem (_, modifiedItemBuildAction, _)) ->
            not (buildAction.ChangesOrder()) && not (modifiedItemBuildAction.ChangesOrder()) ||
            buildAction = modifiedItemBuildAction

        | EmptyFolder _, None -> true
        | EmptyFolder _, Some (FileItem (_, buildAction, _)) -> not (buildAction.ChangesOrder())

        | _ -> false

    let changeDirection = function
        | RelativeToType.Before -> RelativeToType.After
        | RelativeToType.After -> RelativeToType.Before
        | relativeToType -> relativeToType |> sprintf "Got relativeToType %O" |> failwith

    let tryGetAdjacentItemInParent (relativeItem: FSharpProjectItem) relativeToType =
        let otherRelativeSortKey =
            match relativeToType with
            | RelativeToType.After -> relativeItem.SortKey + 1
            | RelativeToType.Before -> relativeItem.SortKey - 1
            | _ -> relativeToType |> sprintf "Got relativeToType %O" |> failwith
        getChildren relativeItem.Parent
        |> Seq.filter (fun item -> item.SortKey = otherRelativeSortKey)
        |> List.ofSeq
        |> function | item :: [] -> Some item | _ -> None

    let splitFolder (folder: FSharpProjectItem) folderPath splitSortKey itemsUpdater =
        let newFolderPart = addFolder folder.Parent (folder.SortKey + 1) folderPath itemsUpdater

        getChildren (ProjectItem folder)
        |> Seq.filter (fun item -> item.SortKey >= splitSortKey)
        |> List.ofSeq
        |> List.iteri (fun i item ->
            item.ItemInfo.Parent <- newFolderPart
            item.ItemInfo.SortKey <- i + 1)

    let rec traverseParentFolders (item: FSharpProjectModelElement) = seq {
        match item with
        | Project -> ()
        | ProjectItem item ->
            yield item
            yield! traverseParentFolders item.Parent }

    let getTopLevelModifiedParent itemPath (relativeItem: FSharpProjectItem) relativeToType itemsUpdater =
        match relativeItem.Parent with
        | Project -> Project, relativeItem, false
        | ProjectItem relativeItemParent ->
            let commonParentPath = FileSystemPath.GetDeepestCommonParent(relativeItemParent.LogicalPath, itemPath)
            let initialState = relativeItem.Parent, relativeItem, false

            traverseParentFolders (ProjectItem relativeItemParent)
            |> Seq.takeWhile (fun item -> item.LogicalPath <> commonParentPath)
            |> Seq.fold (fun state _ ->
                match state with
                | ProjectItem parent, relativeItem, shouldRefresh ->
                    match tryGetAdjacentItemInParent relativeItem relativeToType with
                    | Some secondRelativeItem ->
                        let sortKey = Math.Max(relativeItem.SortKey, secondRelativeItem.SortKey)
                        let relativeItemParent, secondRelativeItemParentPath =
                            match relativeItem.Parent, secondRelativeItem.Parent with
                            | ProjectItem relativeParent, ProjectItem secondRelativeParent ->
                                relativeParent, secondRelativeParent.LogicalPath
                            | _ -> failwith "item parent"
                        splitFolder relativeItemParent secondRelativeItemParentPath sortKey itemsUpdater
    
                        let relativeParent, relativeItem =
                            match relativeItem.Parent with
                            | ProjectItem item -> item.Parent, item
                            | _ -> failwith "getting parent item of project"
                        relativeParent, relativeItem, true
                    | _ -> parent.Parent, parent, shouldRefresh
                | _ -> sprintf "got project as previous parent: %A" state |> failwith) initialState

    let createFoldersForItem itemPath relativeItem relativeToType folderRefresher itemUpdater =
        let parent, relativeItem, shouldRefresh =
            getTopLevelModifiedParent itemPath relativeItem relativeToType itemUpdater

        let newFolders =
            itemPath.GetParentDirectories()
            |> Seq.takeWhile (fun p -> p <> relativeItem.LogicalPath.Parent)
            |> Seq.rev
            |> List.ofSeq

        if shouldRefresh || not (List.isEmpty newFolders) then
            folderRefresher parent

        let sortKey = getNewRelativeSortKey relativeItem relativeToType
        newFolders |> List.fold (fun (parent, sortKey) folderPath ->
            addFolder parent sortKey folderPath itemUpdater, 1) (parent, sortKey)

    let rec tryGetRelativeChildItem (nodeItem: FSharpProjectItem option) modifiedItem relativeToType =
        nodeItem |> Option.bind (fun item ->
            let children = getChildren (ProjectItem item)
            let relativeChildItem =
                match relativeToType with
                | RelativeToType.Before -> Seq.tryHead children
                | RelativeToType.After -> Seq.tryLast children
                | _ -> relativeToType |> sprintf "Got relativeToType %O" |> failwith
        
            match relativeChildItem with
            | Some item when canBeRelative item modifiedItem -> Some (item, relativeToType)
            | _ -> tryGetRelativeChildItem relativeChildItem modifiedItem relativeToType)

    let getRelativeChildPathImpl (relativeViewItem: FSharpViewItem) modifiedNodeItem relativeToType =
        tryGetProjectItem relativeViewItem
        |> Option.bind (function
            | FileItem _ as fileItem -> Some (fileItem, relativeToType)
            | FolderItem _ as folderItem ->
                if canBeRelative folderItem modifiedNodeItem then Some (folderItem, relativeToType) else
                tryGetRelativeChildItem (Some (folderItem)) modifiedNodeItem relativeToType)

    let rec renameFolder oldLocation newLocation itemUpdater =
        getFolders oldLocation
        |> List.iter (fun folderItem ->
            folderItem.ItemInfo.LogicalPath <- newLocation
            folderItem.ItemInfo.PhysicalPath <- newLocation
            folders.AddValue(newLocation, folderItem)

            getChildren (ProjectItem folderItem)
            |> List.ofSeq
            |> List.iter (fun childItem ->
                let oldChildLocation = oldLocation / childItem.LogicalPath.Name // todo
                let newChildLocation = newLocation / childItem.LogicalPath.Name

                match childItem with
                | FileItem _ as childFileItem ->
                    childFileItem.ItemInfo.LogicalPath <- newChildLocation
                    childFileItem.ItemInfo.PhysicalPath <- newLocation / childItem.PhysicalPath.Name
                    files.Remove(oldChildLocation) |> ignore
                    files.Add(newChildLocation, childFileItem)
                | FolderItem _ ->
                    renameFolder oldChildLocation newChildLocation ignore)
            itemUpdater folderItem)
        folders.RemoveKey(oldLocation) |> ignore

    let rec removeSplittedFolderIfEmpty folder folderPath folderRefresher itemUpdater =
        let isFolderSplitted path = getFolders path |> List.length > 1

        match folder with
        | ProjectItem (EmptyFolder (FolderItem (_, folderId)) as folderItem) when isFolderSplitted folderPath ->
            getFolders folderPath
            |> List.iter (fun folderItem ->
                match folderItem with
                | FolderItem (_, id) ->
                    if id.Identity > folderId.Identity then id.Identity <- id.Identity - 1
                | _ -> ())

            removeItem folderRefresher itemUpdater folderItem
            folderRefresher folderItem.Parent
        | _ -> ()

    and removeItem refresher updater (item: FSharpProjectItem) =
        let siblings = getChildren item.Parent |> List.ofSeq
        let itemBefore = siblings |> List.tryFind (fun i -> i.SortKey = item.SortKey - 1)
        let itemAfter = siblings |> List.tryFind (fun i -> i.SortKey = item.SortKey + 1)

        getChildren (ProjectItem item)
        |> Seq.iter (removeItem refresher updater)

        joinRelativeFoldersIfSplitted itemBefore itemAfter refresher updater
        match item with
        | FileItem _ -> files.Remove(item.PhysicalPath) |> ignore
        | _ -> folders.RemoveValue(item.PhysicalPath, item) |> ignore

        moveFollowingItems item.Parent item.SortKey MoveDirection.Up updater
        removeSplittedFolderIfEmpty item.Parent item.LogicalPath.Parent refresher updater

    and joinRelativeFoldersIfSplitted itemBefore itemAfter folderRefresher itemUpdater =
        match itemBefore, itemAfter with
        | Some (FolderItem _ as itemBefore), Some (FolderItem _ as itemAfter) when
                itemBefore.PhysicalPath = itemAfter.PhysicalPath ->

            let folderAfterChildren = getChildren (ProjectItem itemAfter) |> List.ofSeq
            let folderBeforeChildren = getChildren (ProjectItem itemBefore) |> List.ofSeq

            let folderBeforeChildrenCount = folderBeforeChildren |> List.length
            folderAfterChildren |> List.iteri (fun i child ->
                child.ItemInfo.Parent <- ProjectItem itemBefore
                child.ItemInfo.SortKey <- folderBeforeChildrenCount + i + 1)

            folders.RemoveValue(itemAfter.PhysicalPath, itemAfter) |> ignore
            moveFollowingItems itemAfter.Parent itemAfter.SortKey MoveDirection.Up itemUpdater

            let lastChildBefore = List.tryLast folderBeforeChildren
            let firstChildAfter = List.tryHead folderAfterChildren

            joinRelativeFoldersIfSplitted lastChildBefore firstChildAfter folderRefresher itemUpdater
            folderRefresher itemBefore.Parent
        | _ -> ()

    let rec tryGetAdjacentRelativeItem nodeItem modifiedNodeItem relativeToType =
        match nodeItem with
        | Project -> None
        | ProjectItem nodeItem ->
            tryGetAdjacentItemInParent nodeItem relativeToType
            |> Option.bind (fun adjacentItem ->
                if canBeRelative adjacentItem modifiedNodeItem then Some (adjacentItem, relativeToType)
                else
                    tryGetRelativeChildItem (Some adjacentItem) modifiedNodeItem (changeDirection relativeToType)
                    |> Option.map (fun (item, _) -> item, relativeToType))
            |> Option.orElseWith (fun _ ->
                // todo: check item type
                tryGetAdjacentRelativeItem nodeItem.Parent modifiedNodeItem relativeToType)

    let createNewItemInfo path logicalPath relativeToPath relativeToType refresher updater =
        let tryGetPossiblyRelativeNodeItem path =
            if isNull path then None else
            tryGetFile path
            |> Option.orElseWith (fun _ ->
                match getFolders path with
                | EmptyFolder _ as item :: [] -> Some item
                | _ -> None)

        let parent, sortKey =
            match tryGetPossiblyRelativeNodeItem relativeToPath, relativeToType with
            | Some relativeItem, Some relativeToType ->

                // Try adjacent item, if its path matches new item path better (i.e. shares a longer common path)
                let relativeItem, relativeToType =
                    match tryGetAdjacentRelativeItem (ProjectItem relativeItem) None relativeToType with
                    | Some (item, relativeToType) when
                            let relativeCommonParent = getCommonParent logicalPath relativeItem.LogicalPath
                            let adjacentCommonParent = getCommonParent logicalPath item.LogicalPath
                            relativeCommonParent.IsPrefixOf(adjacentCommonParent) ->
                        item, changeDirection relativeToType
                    | _ -> relativeItem, relativeToType

                let relativeItemParent =
                    match relativeItem with
                    | FolderItem _ when relativeItem.LogicalPath = relativeToPath -> ProjectItem relativeItem
                    | _ -> relativeItem.Parent

                let parent, sortKey =
                    match relativeItemParent with
                    | ProjectItem item when item.LogicalPath = logicalPath.Parent ->
                        relativeItemParent, getNewRelativeSortKey relativeItem relativeToType
                    | _ ->
                        // The new item is not in the same folder as the relative item.
                        // We should add new folders and split the relative item parent if needed.
                        createFoldersForItem logicalPath relativeItem relativeToType refresher updater

                moveFollowingItems parent sortKey MoveDirection.Down updater
                parent, sortKey
            | _ ->
                let parent =
                    logicalPath.GetParentDirectories()
                    |> Seq.takeWhile (fun p -> p <> projectDirectory)
                    |> Seq.rev
                    |> Seq.fold (getOrCreateFolder refresher) Project
                parent, getNewSortKey parent

        ItemInfo.Create(path, logicalPath, parent, sortKey)

    let iter f =
        let rec iter (parent: FSharpProjectModelElement) =
            for item in getChildren parent do
                f item
                iter (ProjectItem item)
        iter Project

    member x.Update(items) =
        let folders = Stack()
        folders.Push(State.Create(projectDirectory, Project))

        let parsePaths (item: RdProjectItem) =
            let path = FileSystemPath.TryParse(item.EvaluatedInclude)
            if path.IsEmpty then None else

            let physicalPath = path.MakeAbsoluteBasedOn(projectDirectory)
            let logicalPath =
                let linkPath = item.GetLink()
                if not (linkPath.IsNullOrEmpty()) then
                    linkPath.MakeAbsoluteBasedOn(projectDirectory)
                elif projectDirectory.IsPrefixOf(physicalPath) then physicalPath
                else projectDirectory.Combine(physicalPath.Name)
            Some (physicalPath, logicalPath)

        for item in items do
            match parsePaths item.Item with
            | Some (physicalPath, logicalPath) ->
                Assertion.Assert(projectDirectory.IsPrefixOf(logicalPath), "Invalid logical path")
                if logicalPath.Directory <> folders.Peek().Path then
                    let commonParent = FileSystemPath.GetDeepestCommonParent(logicalPath.Parent, folders.Peek().Path)
                    while (folders.Peek().Path <> commonParent) do
                        folders.Pop() |> ignore

                    let newFolders =
                        logicalPath.GetParentDirectories() |> Seq.takeWhile (fun p -> p <> commonParent) |> Seq.rev

                    for folderPath in newFolders do
                        let currentState = folders.Peek()
                        currentState.NextSortKey <- currentState.NextSortKey + 1

                        let folder = addFolder currentState.Folder currentState.NextSortKey folderPath ignore
                        folders.Push(State.Create(folderPath, folder))

                let currentState = folders.Peek()
                let parent = currentState.Folder
                currentState.NextSortKey <- currentState.NextSortKey + 1

                match item.Item.ItemType with
                | Folder -> addFolder parent currentState.NextSortKey logicalPath ignore |> ignore
                | BuildAction buildAction ->
                    let itemInfo = ItemInfo.Create(physicalPath, logicalPath, parent, currentState.NextSortKey)
                    if files.ContainsKey(physicalPath) then
                        logger.Warn(sprintf "%O added twice" physicalPath)
                    else
                        files.Add(physicalPath, FileItem (itemInfo, buildAction, item.TargetFrameworkIds))
            | _ -> ()

    member x.Write(writer: UnsafeWriter) =
        let writeTargetFrameworkIds ids =
            writer.Write(UnsafeWriter.WriteDelegate(fun writer (value: TargetFrameworkId) ->
                value.Write(writer)), ids)

        writer.Write(projectDirectory)
        writer.Write(projectUniqueName)
        writeTargetFrameworkIds targetFrameworkIds
        writer.Write(files.Count + folders.Values.Count())

        let foldersIds = Dictionary<FSharpProjectModelElement, int>()
        let getFolderId el =
            foldersIds.GetOrCreateValue(el, fun () -> foldersIds.Count)
        foldersIds.[Project] <- 0

        iter (fun projectItem ->
            let info = projectItem.ItemInfo
            writer.Write(info.PhysicalPath)
            writer.Write(info.LogicalPath)
            writer.Write(getFolderId info.Parent)
            writer.Write(info.SortKey)

            match projectItem with
            | FileItem (_, buildAction, targetFrameworks) ->
                writer.Write(int FSharpProjectItemType.File)
                writer.Write(buildAction.Value)
                writeTargetFrameworkIds targetFrameworks

            | FolderItem (_, identity) ->
                writer.Write(int FSharpProjectItemType.Folder)
                writer.Write(getFolderId (ProjectItem projectItem))
                writer.Write(identity.Identity))

    member private x.AddItem(item: FSharpProjectItem) =
        let path = item.PhysicalPath
        match item with
        | FileItem _ -> files.[path] <- item
        | FolderItem _ -> folders.AddValue(path, item)

    static member Read(reader: UnsafeReader) =
        let projectDirectory = reader.ReadFileSystemPath()
        let projectUniqueName = reader.ReadString()
        let targetFrameworkIdIntern = DataIntern(setComparer)
        let readTargetFrameworkIds () =
            let ids = reader.ReadCollection(UnsafeReader.ReadDelegate(TargetFrameworkId.Read), fun _ -> HashSet())
            targetFrameworkIdIntern.Intern(ids) 
    
        let logger = Logger.GetLogger<FSharpItemsContainer>()
        let mapping = ProjectMapping(projectDirectory, projectUniqueName, readTargetFrameworkIds (), logger)
        let foldersById = Dictionary<int, FSharpProjectModelElement>()
        foldersById.[0] <- Project

        let itemsCount = reader.ReadInt()
        for _ in 1 .. itemsCount do
            let itemInfo =
                { PhysicalPath = reader.ReadFileSystemPath()
                  LogicalPath = reader.ReadFileSystemPath()
                  Parent = foldersById.[reader.ReadInt()]
                  SortKey = reader.ReadInt() }

            let item =
                match reader.ReadInt() |> LanguagePrimitives.EnumOfValue with
                | FSharpProjectItemType.File ->
                    let (BuildAction buildAction) = reader.ReadString()
                    FileItem(itemInfo, buildAction, readTargetFrameworkIds ())

                | FSharpProjectItemType.Folder ->
                    let id = reader.ReadInt()
                    let item = FolderItem(itemInfo, { Identity = reader.ReadInt() })
                    foldersById.[id] <- ProjectItem item
                    item

                | itemType -> sprintf "got item %O" itemType |> failwith
            mapping.AddItem(item)

        mapping

    static member Marshaller =
        { new IUnsafeMarshaller<ProjectMapping> with
            member x.Marshal(writer, value) = value.Write(writer)
            member x.Unmarshal(reader) = ProjectMapping.Read(reader) }

    member x.UpdateFile(oldItemType, oldLocation, BuildAction buildAction, newLocation) =
        match tryGetFile oldLocation with
        | Some (FileItem (info, oldBuildAction, targetFrameworkIds)) ->
            Assertion.Assert(equalsIgnoreCase oldItemType oldBuildAction.Value, "old build action mismatch")

            files.Remove(oldLocation) |> ignore
            files.Add(newLocation, FileItem (info, buildAction, targetFrameworkIds))
            if oldLocation <> newLocation then
                // renaming linked files isn't currently supported, but 
                info.LogicalPath <- info.LogicalPath.Directory / newLocation.Name
                info.PhysicalPath <- info.PhysicalPath.Directory / newLocation.Name
        | item -> sprintf "got item %O" item |> failwith

    member x.RemoveFile(path, refresher, updater) =
        tryGetFile path
        |> Option.orElseWith (fun _ -> sprintf "No item found for %O" path |> failwith)
        |> Option.iter (removeItem refresher updater)

    member x.RemoveFolder(path, refresher, updater) =
        getFolders path
        |> List.iter (removeItem refresher updater)

    member x.UpdateFolder(oldLocation, newLocation, updater) =
        Assertion.Assert(oldLocation.Parent = newLocation.Parent, "oldLocation.Parent = newLocation.Parent")
        renameFolder oldLocation newLocation updater

    member x.TryGetProjectItem(viewItem: FSharpViewItem): FSharpProjectItem option =
        tryGetProjectItem viewItem

    member x.TryGetProjectItems(path: FileSystemPath): FSharpProjectItem list =
        getItemsForPath path

    member x.AddFile(BuildAction buildAction, path, logicalPath, relativeToPath, relativeToType, refresher, updater) =
        let itemInfo = createNewItemInfo path logicalPath relativeToPath relativeToType refresher updater
        files.Add(path, FileItem(itemInfo, buildAction, targetFrameworkIds))

    member x.AddFolder(path, relativeToPath, relativeToType, refresher, updater) =
        let itemInfo = createNewItemInfo path path relativeToPath relativeToType refresher updater
        folders.Add(path, FolderItem(itemInfo, getNewFolderIdentity path))

    member x.TryGetRelativeChildPath(modifiedItem, relativeItem, relativeToType) =
        let modifiedNodeItem = tryGetProjectItem modifiedItem
        match getRelativeChildPathImpl relativeItem modifiedNodeItem relativeToType with
        | Some (relativeChildItem, relativeToType) when
                relativeChildItem.PhysicalPath = modifiedItem.ProjectItem.Location ->

            // When moving files, we remove each file first and then we add it next to the relative item.
            // An item should not be relative to itself as we won't be able to find place to insert after removing.
            // We need to find another item to be relative to.
            match tryGetAdjacentRelativeItem (ProjectItem relativeChildItem) modifiedNodeItem relativeToType with
            | Some (adjacentItem, relativeToType) ->
                  Some (adjacentItem.PhysicalPath, changeDirection relativeToType)
            | _ -> 
                // There were no adjacent items in this direction, try the other one.
                let relativeToType = changeDirection relativeToType
                tryGetAdjacentRelativeItem (ProjectItem relativeChildItem) modifiedNodeItem relativeToType
                |> Option.map (fun (item, relativeToType) -> item.PhysicalPath, changeDirection relativeToType)

        | Some (item, reltativeToType) -> Some (item.PhysicalPath, relativeToType)
        | _ -> None

    member x.GetProjectItemsPaths(targetFrameworkId) =
        let result = List()
        iter (function
            | FileItem (info, buildAction, ids) when ids.Contains(targetFrameworkId) ->
                result.Add((info.PhysicalPath, buildAction))
            | _ -> ())
        result.ToArray()

    member x.Dump(writer: TextWriter) =
        let rec dump (parent: FSharpProjectModelElement) ident =
            for item in getChildren parent do
                writer.WriteLine(sprintf "%s%d:%O" (String(' ', ident * 2)) item.SortKey item)
                dump (ProjectItem item) (ident + 1)
        dump Project 0

        for targetFrameworkId in targetFrameworkIds do
            writer.WriteLine()
            writer.WriteLine(targetFrameworkId)
            x.GetProjectItemsPaths(targetFrameworkId)
            |> Array.iter (fun ((UnixSeparators path), _) -> writer.WriteLine(path))
            writer.WriteLine()

    member x.DumpToString() =
        let writer = new StringWriter()
        x.Dump(writer)
        writer.ToString()

    static member DummyMapping =
        ProjectMapping(FileSystemPath.Empty, String.Empty, EmptySet.Instance, DummyLogger.Instance)

type FSharpProjectModelElement =
    | Project
    | ProjectItem of FSharpProjectItem


[<ReferenceEquality>]
type FSharpProjectItem =
    | FileItem of ItemInfo * BuildAction * ISet<TargetFrameworkId>
    | FolderItem of ItemInfo * FSharpViewFolderIdentity 

    member x.ItemInfo: ItemInfo =
        match x with
        | FileItem (info, _, _)
        | FolderItem (info, _) -> info

    member x.SortKey = x.ItemInfo.SortKey
    member x.Parent  = x.ItemInfo.Parent
    member x.PhysicalPath: FileSystemPath = x.ItemInfo.PhysicalPath
    member x.LogicalPath: FileSystemPath = x.ItemInfo.LogicalPath

    override x.ToString() =
        let name =
            match x with
            | FolderItem (_, id) as folderItem -> sprintf "%s[%d]" x.LogicalPath.Name id.Identity
            | FileItem (_, buildAction, _) when
                    not (buildAction.IsCompile()) -> sprintf "%s (%O)" x.LogicalPath.Name buildAction
            | _ -> x.LogicalPath.Name
        if x.PhysicalPath = x.LogicalPath then name
        else
            let (UnixSeparators path) = x.PhysicalPath
            sprintf "%s (from %s)" name path

[<RequireQualifiedAccess>]
type FSharpProjectItemType =
    | File = 0
    | Folder = 1


type ItemInfo =
    { mutable PhysicalPath: FileSystemPath
      mutable LogicalPath: FileSystemPath
      mutable Parent: FSharpProjectModelElement
      mutable SortKey: int }

    static member Create(physicalPath, logicalPath, parent, sortKey) =
        { PhysicalPath = physicalPath; LogicalPath = logicalPath; Parent = parent; SortKey = sortKey }

    override x.ToString() = x.LogicalPath.Name


[<RequireQualifiedAccess>]
type MoveDirection =
    | Up
    | Down


type State =
    { Path: FileSystemPath
      Folder: FSharpProjectModelElement
      mutable NextSortKey: int }

    static member Create(path, folder) =
        { Path = path; Folder = folder; NextSortKey = 0 }


type RdProjectItemWithTargetFrameworks =
    { Item: RdProjectItem
      TargetFrameworkIds: HashSet<TargetFrameworkId> }


type IFSharpItemsContainerRefresher =
    /// Refreshes the project structure for a project.
    abstract member Refresh: IProjectMark * isOnProjectLoad: bool -> unit

    /// Refreshes the project structure for a folder in a project.
    abstract member Refresh: IProjectMark * folder: FileSystemPath * identity: FSharpViewFolderIdentity -> unit

    /// Updates presentation (i.e. changes sort key) for a file.
    abstract member Update: IProjectMark * file: FileSystemPath -> unit

    /// Updates presentation (i.e. changes sort key) for a folder.
    abstract member Update: IProjectMark * folder: FileSystemPath * identity: FSharpViewFolderIdentity -> unit 

    /// Used on changes we currenlty cannot process, e.g. Compile -> CompileBefore build action change.
    abstract member ReloadProject: IProjectMark -> unit

    /// Selects an item after a project structure change that could make an item parent folder collapse.
    abstract member SelectItem: IProjectMark * FileSystemPath -> unit


[<SolutionInstanceComponent>]
type FSharpItemsContainerRefresher(lifetime: Lifetime, solution: ISolution, viewHost: ProjectModelViewHost) =

    let tryGetProject projectMark =
        solution.GetProjectByMark(projectMark) |> Option.ofObj

    let refresh projectMark getFolders =
        use lock = solution.Locks.UsingReadLock()
        tryGetProject projectMark
        |> Option.iter (fun project ->
            for projectFolder in getFolders project do
                solution.Locks.QueueReadLock(lifetime, "Refresh View", fun _ ->
                    match solution.TryGetComponent<ProjectModelAppender>() with
                    | null -> ()
                    | appender -> appender.Refresh(projectFolder)))

    let update projectMark path viewItemCtor =
        use lock = solution.Locks.UsingReadLock()
        tryGetProject projectMark
        |> Option.iter (fun project ->
            for viewItem in project.FindProjectItemsByLocation(path) |> Seq.choose viewItemCtor do
                solution.Locks.QueueReadLock(lifetime, "Refresh View", fun _ ->
                    if solution.GetComponent<FSharpItemsContainer>().IsValid(viewItem) then
                        viewHost.UpdateItemIfExists(viewItem)))

    interface IFSharpItemsContainerRefresher with
        member x.Refresh(projectMark, isOnProjectLoad) =
            refresh projectMark (fun project -> [project])

        // todo: single identity
        member x.Refresh(projectMark, folder, folderIdentity) =
            refresh projectMark (fun project -> project.FindProjectItemsByLocation(folder).OfType<IProjectFolder>()) 
    
        member x.Update(projectMark, path) =
            update projectMark path (function | ProjectFile x -> Some (FSharpViewFile(x)) | _ -> None)

        member x.Update(projectMark, path, id) =
            update projectMark path (function | ProjectFolder x -> Some (FSharpViewFolder (x, id)) | _ -> None)

        member x.ReloadProject(projectMark) =
            let opName = sprintf "Reload %O after FSharpItemsContainer change" projectMark
            solution.Locks.QueueReadLock(lifetime, opName, fun _ ->
                solution.ProjectsHostContainer().GetComponent<ISolutionHost>().ReloadProject(projectMark))

        member x.SelectItem(projectMark, filePath) =
            let opName = sprintf "Select %O after FSharpItemsContainer change" filePath
            solution.Locks.QueueReadLock(lifetime, opName, fun _ ->
                tryGetProject projectMark
                |> Option.bind (fun project ->
                    project.FindProjectItemsByLocation(filePath).OfType<IProjectFile>() |> Seq.tryHead)
                |> Option.filter (fun projectFile -> projectFile.IsValid())
                |> Option.iter (fun projectFile ->

                let navigationManager = NavigationManager.GetInstance(solution)
                ignore (Lifetimes.Using(fun lifetime ->
                    let points =
                        navigationManager
                            .GetNavigationPoints<ISolutionExplorerNavigationProvider, IProjectItem>(projectFile)
                    let solutionExplorerDataContext = solution.GetComponent<DataContexts>().CreateOnSelection(lifetime)
                    let caption = RichText("Navigate to Solution Explorer")
                    let options = NavigationOptions.FromDataContext(solutionExplorerDataContext, caption, true)
                    navigationManager.Navigate(points, options)))))


type FSharpViewItem =
    | FSharpViewFile of IProjectFile
    | FSharpViewFolder of IProjectFolder * FSharpViewFolderIdentity

    member x.ProjectItem: IProjectItem =
        match x with
        | FSharpViewFile file -> file :> _
        | FSharpViewFolder (folder, _) -> folder :> _

    member x.Location = x.ProjectItem.Location

    override x.ToString() =
        match x with
        | FSharpViewFile file -> file.Name
        | FSharpViewFolder (folder, identity) -> sprintf "%s[%O]" folder.Name identity

    interface IProjectElementHolder with
        member x.Element = x.ProjectItem :> _


type FSharpViewFolderIdentity =
    { mutable Identity: int }

    override x.ToString() = x.Identity.ToString()


[<SolutionFeaturePart>]
type FSharpItemModificationContextProvider(container: IFSharpItemsContainer) =
    inherit ItemModificationContextProvider()

    override x.IsApplicable(project) = project.IsFSharp

    override x.CreateModificationContext(modifiedItem, relativeItem, relativeToType) =
        let context =
            match modifiedItem, relativeItem with
            | (:? FSharpViewItem as modifiedViewItem), (:? FSharpViewItem as relativeViewItem) ->
                x.CreateModificationContext(modifiedViewItem, relativeViewItem, relativeToType)
            | _ -> None
        match context with
        | Some context -> context :> _
        | _ -> base.CreateModificationContext(modifiedItem, relativeItem, relativeToType)

    member x.CreateModificationContext(modifiedViewItem, (relativeViewItem: FSharpViewItem), relativeToType) =
        let project = relativeViewItem.ProjectItem.GetProject().NotNull()
        container.TryGetRelativeChildPath(project.GetProjectMark(), modifiedViewItem, relativeViewItem, relativeToType)
        |> Option.map (fun (path, relativeToType) ->
            let relativeProjectItem = project.FindProjectItemsByLocation(path).First()
            RiderItemModificationContext(RelativeTo(relativeProjectItem, relativeToType)))


[<ShellComponent>]
type FSharpModificationSettingsProvider() =
    interface IMsBuildModificationSettingsProvider with
        member x.SmartModificationsFilter = ["fsproj"] :> _
