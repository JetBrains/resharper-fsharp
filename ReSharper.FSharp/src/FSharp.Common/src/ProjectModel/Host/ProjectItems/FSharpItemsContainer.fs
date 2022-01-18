module rec JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Host.ProjectItems.ItemsContainer

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Linq
open JetBrains.Application
open JetBrains.Application.Components
open JetBrains.Application.DataContext
open JetBrains.Application.PersistentMap
open JetBrains.Application.Threading
open JetBrains.DataFlow
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.Platform.MsBuildHost.Models
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Caches
open JetBrains.ProjectModel.ProjectsHost
open JetBrains.ProjectModel.ProjectsHost.Impl
open JetBrains.ProjectModel.ProjectsHost.MsBuild
open JetBrains.ProjectModel.ProjectsHost.MsBuild.Structure
open JetBrains.ProjectModel.ProjectsHost.SolutionHost
open JetBrains.ProjectModel.Update
open JetBrains.ReSharper.Feature.Services.Navigation
open JetBrains.ReSharper.Feature.Services.Navigation.NavigationProviders
open JetBrains.RdBackend.Common.Features.ProjectModel.View
open JetBrains.RdBackend.Common.Features.ProjectModel.View.Appenders
open JetBrains.RdBackend.Common.Features.ProjectModel.View.Ordering
open JetBrains.RdBackend.Common.Features.Util.Tree
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.ReSharper.Psi
open JetBrains.Serialization
open JetBrains.Threading
open JetBrains.UI.RichText
open JetBrains.Util
open JetBrains.Util.Caches
open JetBrains.Util.DataStructures
open JetBrains.Util.Dotnet.TargetFrameworkIds
open JetBrains.Util.Logging
open JetBrains.Util.PersistentMap

/// Keeps project mappings in solution caches so mappings available on warm start before MsBuild loads projects.
[<SolutionInstanceComponent>]
type FSharpItemsContainerLoader(lifetime: Lifetime, solution: ISolution, solutionCaches: ISolutionCaches) =

    abstract GetMap: unit -> IDictionary<IProjectMark, ProjectMapping>
    default x.GetMap() =
        let projectMarks =
            solution.ProjectsHostContainer().GetComponent<ISolutionStructureContainer>().ProjectMarks
            |> Seq.map (fun projectMark -> projectMark.UniqueProjectName, projectMark)
            |> dict

        let dummyProjectMark =
            DummyProjectMark(solution.GetSolutionMark(), String.Empty, Guid.Empty, VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext))

        let projectMarkMarshaller =
            { new IUnsafeMarshaller<IProjectMark> with
                member x.Marshal(writer, value) =
                    writer.Write(value.UniqueProjectName)

                member x.Unmarshal(reader) =
                    let uniqueProjectName = reader.ReadString()
                    match projectMarks.TryGetValue(uniqueProjectName) with
                    | null -> dummyProjectMark :> _ // The project mark was already removed.
                    | projectMark -> projectMark }

        let map =
            solutionCaches.Db
                .GetMap("FSharpItemsContainer", projectMarkMarshaller, ProjectMapping.Marshaller)
                .ToOptimized(lifetime, Cache = DirectMappedCache(1 <<< 8))

        map.Remove(dummyProjectMark) |> ignore
        map :> _


type IItemTypeFilterProvider =
    abstract CreateItemFilter: RdProject * IProjectDescriptor -> MsBuildItemTypeFilter


[<SolutionInstanceComponent>]
type ItemTypeFilterProvider(buildActions: MsBuildDefaultBuildActions) =
    interface IItemTypeFilterProvider with
        member x.CreateItemFilter(rdProject, projectDescriptor) =
            buildActions.CreateItemFilter(rdProject, projectDescriptor)


/// Keeps project items in proper order and is used in creating FCS project options and F# project tree.
[<SolutionInstanceComponent>]
type FSharpItemsContainer(lifetime: Lifetime, logger: ILogger, containerLoader: FSharpItemsContainerLoader,
        projectRefresher: IFSharpItemsContainerRefresher, filterProvider: IItemTypeFilterProvider) =

    let locker = JetFastSemiReenterableRWLock()
    let projectMappings = lazy (containerLoader.GetMap())
    let targetFrameworkIdsIntern = DataIntern(setComparer)

    let fsProjectLoaded = new Signal<IProjectMark>(lifetime, "fsProjectLoaded")
 
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

    let getItems (msBuildProject: MsBuildProject) projectDescriptor itemTypeFilter (itemsByName: CompactOneToListMap<_,_>) allowNonDefaultItemType =
        let items = List<RdProjectItemWithTargetFrameworks>()
        for rdProject in msBuildProject.RdProjects do
            let targetFrameworkId = msBuildProject.GetTargetFramework(rdProject)
            let filter = filterProvider.CreateItemFilter(rdProject, projectDescriptor)

            rdProject.Items
            |> Seq.filter (fun item ->
                itemTypeFilter item.ItemType &&

                // todo: allow passing additional default item types to the filter ctor
                (allowNonDefaultItemType || not (filter.FilterByItemType(item.ItemType, item.IsImported()))) &&

                (let (BuildAction buildAction) = item.ItemType
                 not (buildAction.IsNone() && itemsByName.[item.EvaluatedInclude].Count > 1)))

            |> Seq.fold (fun index item ->
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
            | None, _ | _, Project _ -> folderToRefresh <- Some folder
            | Some (ProjectItem existingFolder), ProjectItem newFolder when
                    newFolder.LogicalPath.IsPrefixOf(existingFolder.LogicalPath) -> folderToRefresh <- Some folder
            | _ -> ()

        let updateItem item =
            itemsToUpdate.Add(item) |> ignore

        let refresh () =
            match folderToRefresh with
                | Some (Project _) -> projectRefresher.RefreshProject(projectMark, false)
                | Some (ProjectItem (FolderItem (_, id) as folder)) ->
                    projectRefresher.RefreshFolder(projectMark, folder.LogicalPath, id)
                | _ -> ()
            for item in itemsToUpdate do
                match item with
                | FileItem _ -> projectRefresher.UpdateFile(projectMark, item.ItemInfo.PhysicalPath)
                | FolderItem (_, id) -> projectRefresher.UpdateFolder(projectMark, item.ItemInfo.PhysicalPath, id)

        refreshFolder, updateItem, refresh

    let updateProject projectMark updateFunction =
        use lock = locker.UsingWriteLock()
        tryGetProjectMapping projectMark
        |> Option.iter (fun mapping ->
            let refreshFolder, update, refresh = createRefreshers projectMark
            updateFunction mapping refreshFolder update
            projectMappings.Value.[projectMark] <- mapping
            refresh ())

    let addProjectMapping targetFrameworkIds items projectMark =
        use lock = locker.UsingWriteLock()
        projectMappings.Value.[projectMark] <-
            let projectDirectory = projectMark.Location.Directory
            let projectUniqueName = projectMark.UniqueProjectName
            let mapping = ProjectMapping(projectDirectory, projectUniqueName, targetFrameworkIds, logger)
            mapping.Update(items)
            mapping

    member x.ProjectMappings = projectMappings.Value

    member x.FSharpProjectLoaded = fsProjectLoaded

    member x.IsValid(viewItem: FSharpViewItem) =
        use lock = locker.UsingReadLock()
        tryGetProjectItem viewItem |> Option.isSome

    member x.RemoveProject(project: IProject) =
        use lock = locker.UsingWriteLock()
        let projectMark = project.GetProjectMark()
        x.ProjectMappings.Remove(projectMark) |> ignore

    interface IFSharpItemsContainer with
        member x.OnProjectLoaded(projectMark, msBuildProject, projectDescriptor) =
            match msBuildProject with
            | null ->
                use lock = locker.UsingWriteLock()
                x.ProjectMappings.Remove(projectMark) |> ignore
            | _ ->

            match projectMark with
            | FSharpProjectMark ->
                let itemsByName = CompactOneToListMap()
                for rdProject in msBuildProject.RdProjects do
                    let itemFilter = filterProvider.CreateItemFilter(rdProject, projectDescriptor)

                    let isAllowedItem (item: RdProjectItem) =
                        match item.ItemType with
                        | CompileBefore | CompileAfter -> true
                        | itemType -> not (itemFilter.FilterByItemType(itemType, item.IsImported()))

                    for rdItem in rdProject.Items do
                        if isAllowedItem rdItem then
                            itemsByName.AddValue(rdItem.EvaluatedInclude, rdItem.ItemType)

                let compileBeforeItems = getItems msBuildProject projectDescriptor isCompileBefore itemsByName true
                let compileAfterItems = getItems msBuildProject projectDescriptor isCompileAfter itemsByName true
                let restItems = getItems msBuildProject projectDescriptor (changesOrder >> not) itemsByName false
                let items =
                    compileBeforeItems.Concat(restItems).Concat(compileAfterItems)
                    |> Seq.map (fun item ->
                        { item with TargetFrameworkIds = targetFrameworkIdsIntern.Intern(item.TargetFrameworkIds) })
                    |> List.ofSeq
                let targetFrameworkIds = HashSet(msBuildProject.TargetFrameworkIds)

                fsProjectLoaded.Fire(projectMark)                
                addProjectMapping targetFrameworkIds items projectMark
                projectRefresher.RefreshProject(projectMark, true)
            | _ -> ()

        member x.OnAddFile(projectMark, itemType, path, linkedPath, relativeTo, relativeToType) =
            match projectMark with
            | FSharpProjectMark ->
                logger.Trace("On add file: {0} ({1}, link: {2}) relative to: {3} {4}",
                    path, itemType, linkedPath, relativeTo, relativeToType)
                updateProject projectMark (fun mapping refreshFolder update ->
                    let logicalPath = if isNotNull linkedPath then linkedPath else path
                    let relativeToType = Option.ofNullable relativeToType
                    mapping.AddFile(itemType, path, logicalPath, relativeTo, relativeToType, refreshFolder, update))
                projectRefresher.SelectItem(projectMark, path)
            | _ -> ()

        member x.OnRemoveFile(projectMark, itemType, path) =
            logger.Trace("On remove file: {0} ({1})", path, itemType)
            updateProject projectMark (fun mapping refreshFolder update ->
                mapping.RemoveFile(path, refreshFolder, update))

        member x.OnUpdateFile(projectMark, oldItemType, oldLocation, newItemType, newLocation) =
            match projectMark with
            | FSharpProjectMark ->
                logger.Trace("On update file: {0} ({1}) to {2} ({3})", oldLocation, oldItemType, newLocation, newItemType)
                if not (equalsIgnoreCase oldItemType newItemType) &&
                        (changesOrder oldItemType || changesOrder newItemType) then
                    projectRefresher.ReloadProject(projectMark) else

                updateProject projectMark (fun mapping _ _ ->
                    mapping.UpdateFile(oldItemType, oldLocation, newItemType, newLocation)
                    projectRefresher.UpdateFile(projectMark, newLocation))
            | _ -> ()

        member x.OnUpdateFolder(projectMark, oldLocation, newLocation) =
            logger.Trace("On update folder: {0} to {1}", oldLocation, newLocation)
            if oldLocation <> newLocation then
                updateProject projectMark (fun mapping _ update ->
                    mapping.UpdateFolder(oldLocation, newLocation, update))

        member x.OnAddFolder(projectMark, path, relativeTo, relativeToType) =
            logger.Trace("On add folder: {0} relative to {1} {2}", path, relativeTo, relativeToType)
            updateProject projectMark (fun mapping refreshFolder update ->
                mapping.AddFolder(path, relativeTo, Option.ofNullable relativeToType, refreshFolder, update))

        member x.OnRemoveFolder(projectMark, path) =
            logger.Trace("On remove file: {0}", path)
            updateProject projectMark (fun mapping refreshFolder update ->
                mapping.RemoveFolder(path, refreshFolder, update))

        member x.CreateFoldersWithParents(folder: IProjectFolder) =
            use lock = locker.UsingReadLock()
            tryGetProjectMark folder
            |> Option.bind tryGetProjectMapping
            |> Option.map (fun mapping ->
                mapping.TryGetFolderItems(folder.Location)
                |> Seq.map (function
                    | FolderItem (_, id) as folderItem ->
                        let parent =
                            match folderItem.Parent with
                            | ProjectItem (FolderItem (_, id)) -> Some (FSharpViewFolder (folder.ParentFolder, id))
                            | _ -> None
                        FSharpViewFolder (folder, id), parent
                    | item -> failwithf "got item %O" item))
            |> Option.defaultValue Seq.empty

        member x.TryGetRelativeChildPath(projectMark, modifiedItem, relativeItem: FSharpViewItem option, relativeToType) =
            use lock = locker.UsingReadLock()
            tryGetProjectMapping projectMark
            |> Option.bind (fun mapping ->
                let relativeItem, relativeToType =
                    relativeItem
                    |> Option.bind tryGetProjectItem
                    |> Option.map (fun item -> ProjectItem item, relativeToType)
                    |> Option.defaultValue (Project projectMark.Location.Directory, RelativeToType.Inside)

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
            for KeyValue (projectMark, mapping) in x.ProjectMappings do
                writer.WriteLine(projectMark.Name)
                mapping.Dump(writer)

        member x.TryGetSortKey(viewItem: FSharpViewItem) =
            use lock = locker.UsingReadLock()
            tryGetProjectItem viewItem |> Option.map (fun item -> item.SortKey)

        member x.GetProjectItemsPaths(projectMark, targetFrameworkId) =
            tryGetProjectMapping projectMark
            |> Option.map (fun mapping -> mapping.GetProjectItemsPaths(targetFrameworkId))
            |> Option.defaultValue [| |]

type IFSharpItemsContainer =
    inherit IMsBuildProjectListener
    inherit IMsBuildProjectModificationListener

    abstract member TryGetSortKey: FSharpViewItem -> int option
    abstract member TryGetParentFolderIdentity: FSharpViewItem -> FSharpViewFolderIdentity option
    abstract member CreateFoldersWithParents: IProjectFolder -> (FSharpViewItem * FSharpViewItem option) seq
    abstract member GetProjectItemsPaths: IProjectMark * TargetFrameworkId -> (VirtualFileSystemPath * BuildAction)[]
    abstract member Dump: TextWriter -> unit

    abstract member TryGetRelativeChildPath:
            IProjectMark * modifiedItem: FSharpViewItem option * relativeItem: FSharpViewItem option * RelativeToType ->
            (VirtualFileSystemPath * RelativeToType) option

[<DebuggerDisplay("{projectUniqueName}")>]
type ProjectMapping(projectDirectory, projectUniqueName, targetFrameworkIds: ISet<_>, logger: ILogger) =
    let project = Project projectDirectory

    // Files and folders by physical path.
    // For now we assume that a file is only included to a single item type group.
    let files = Dictionary<VirtualFileSystemPath, FSharpProjectItem>()
    let folders = CompactOneToListMap<VirtualFileSystemPath, FSharpProjectItem>()

    let children = CompactOneToListMap<FSharpProjectModelElement, FSharpProjectItem>()

    let addChild (item: FSharpProjectItem) =
        children.AddValue(item.Parent, item)

    let removeChild (item: FSharpProjectItem) =
        children.RemoveValue(item.Parent, item)

    let tryGetFile path =
        tryGetValue path files

    let tryGetProjectItem (viewItem: FSharpViewItem) =
        let path = viewItem.ProjectItem.Location
        match viewItem with
        | FSharpViewFile _ -> tryGetFile path
        | FSharpViewFolder (_, identity) ->
            folders.[path]
            |> Seq.tryFind (function | FolderItem (_, id) -> id = identity | _ -> false)

    let getNewFolderIdentity path =
        let folders = folders.[path]
        { Identity = folders.Count + 1 }

    let getChildrenSorted (parent: FSharpProjectModelElement) =
        children.[parent]
        |> Seq.sortBy (fun x -> x.SortKey)

    let getNewSortKey parent =
        children.[parent].Count + 1

    let moveFollowingItems parent sortKeyFrom direction updateItem =
        for item in getChildrenSorted parent do
            if item.SortKey >= sortKeyFrom then
                item.ItemInfo.SortKey <-
                    match direction with
                    | MoveDirection.Up -> item.SortKey - 1
                    | MoveDirection.Down -> item.SortKey + 1
                updateItem item

    let addFolder parent sortKey path updateItem =
        let item = FolderItem(ItemInfo.Create(path, path, parent, sortKey), getNewFolderIdentity path)
        moveFollowingItems parent sortKey MoveDirection.Down updateItem

        folders.AddValue(path, item)
        addChild item

        ProjectItem item

    let getOrCreateFolder folderRefresher parent path =
        folders.[path]
        |> Seq.sortBy (fun item -> item.SortKey)
        |> Seq.tryLast
        |> Option.defaultWith (fun _ ->
            folderRefresher parent

            let info = ItemInfo.Create(path, path, parent, getNewSortKey parent)
            let item = FolderItem(info, getNewFolderIdentity path)

            folders.AddValue(path, item)
            addChild item

            item)
        |> ProjectItem

    let (|EmptyFolder|_|) projectItem =
        match projectItem with
        | FolderItem _ when children.[ProjectItem projectItem].IsEmpty() -> Some projectItem
        | _ -> None

    let getNewRelativeSortKey (item: FSharpProjectItem) relativeToType =
        match relativeToType with
        | RelativeToType.Before -> item.SortKey
        | RelativeToType.After
        | RelativeToType.Inside -> item.SortKey + 1
        | _ -> relativeToType |> failwithf "Got relativeToType %O"

    let canBeRelative (projectItem: FSharpProjectItem) (modifiedItemBuildAction: BuildAction option) =
        match projectItem, modifiedItemBuildAction with
        | FileItem _, None -> true
        | FileItem (_, buildAction, _, _), Some modifiedItemBuildAction ->
            not buildAction.ChangesOrder && not modifiedItemBuildAction.ChangesOrder ||
            buildAction = modifiedItemBuildAction

        | EmptyFolder _, None -> true
        | EmptyFolder _, Some buildAction -> not buildAction.ChangesOrder

        | _ -> false

    let changeDirection = function
        | RelativeToType.Before -> RelativeToType.After
        | RelativeToType.After
        | RelativeToType.Inside -> RelativeToType.Before
        | relativeToType -> relativeToType |> failwithf "Got relativeToType %O"

    let tryGetAdjacentItemInParent (relativeItem: FSharpProjectItem) relativeToType =
        let otherRelativeSortKey =
            match relativeToType with
            | RelativeToType.After
            | RelativeToType.Inside -> relativeItem.SortKey + 1
            | RelativeToType.Before -> relativeItem.SortKey - 1
            | _ -> relativeToType |> failwithf "Got relativeToType %O"
        getChildrenSorted relativeItem.Parent
        |> Seq.filter (fun item -> item.SortKey = otherRelativeSortKey)
        |> Seq.tryExactlyOne

    let splitFolder (folder: FSharpProjectItem) folderPath splitSortKey itemsUpdater =
        let newFolderPart = addFolder folder.Parent (folder.SortKey + 1) folderPath itemsUpdater

        let oldParent = ProjectItem folder
        getChildrenSorted oldParent
        |> Seq.filter (fun item -> item.SortKey >= splitSortKey)
        |> Seq.iteri (fun i item ->
            item.ItemInfo.Parent <- newFolderPart
            item.ItemInfo.SortKey <- i + 1

            children.RemoveValue(oldParent, item)
            addChild item)

    let rec traverseParentFolders (item: FSharpProjectModelElement) = seq {
        match item with
        | Project _ -> ()
        | ProjectItem item ->
            yield item
            yield! traverseParentFolders item.Parent }

    let getTopLevelModifiedParent itemPath (relativeItem: FSharpProjectItem) relativeToType itemsUpdater =
        match relativeItem.Parent with
        | Project projectPath -> Project projectPath, relativeItem, false
        | ProjectItem relativeItemParent ->
            let commonParentPath = FileSystemPathEx.GetDeepestCommonParent(relativeItemParent.LogicalPath, itemPath)
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
                | _ -> failwithf "got project as previous parent: %A" state) initialState

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

    let rec tryGetRelativeChildItem (relativeElement: FSharpProjectModelElement) modifiedItem relativeToType =
            let children = getChildrenSorted relativeElement
            let relativeChildItem =
                match relativeToType with
                | RelativeToType.Before -> Seq.tryHead children
                | RelativeToType.After
                | RelativeToType.Inside -> Seq.tryLast children
                | _ -> relativeToType |> failwithf "Got relativeToType %O"

            match relativeChildItem with
            | Some item when canBeRelative item modifiedItem -> Some (item, relativeToType)
            | Some item -> tryGetRelativeChildItem (ProjectItem item) modifiedItem relativeToType
            | _ -> None

    let getRelativeChildPathImpl (relativeElement: FSharpProjectModelElement) modifiedItemBuildAction relativeToType =
        match relativeElement with
        | ProjectItem (FileItem _ as fileItem) -> Some (fileItem, relativeToType)

        | ProjectItem (FolderItem _ as folderItem) ->
            if canBeRelative folderItem modifiedItemBuildAction then Some (folderItem, relativeToType) else
            tryGetRelativeChildItem relativeElement modifiedItemBuildAction relativeToType

        | Project _ ->
            tryGetRelativeChildItem relativeElement modifiedItemBuildAction relativeToType

    let rec renameFolder oldLocation newLocation itemUpdater =
        folders.[oldLocation]
        |> Seq.iter (fun folderItem ->
            folderItem.ItemInfo.LogicalPath <- newLocation
            folderItem.ItemInfo.PhysicalPath <- newLocation

            folders.AddValue(newLocation, folderItem)

            getChildrenSorted (ProjectItem folderItem)
            |> Seq.iter (fun childItem ->
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
        folders.RemoveKey(oldLocation)

    let rec removeSplittedFolderIfEmpty folder folderPath folderRefresher itemUpdater =
        let isFolderSplitted path = folders.[path].Count > 1

        match folder with
        | ProjectItem (EmptyFolder (FolderItem (_, folderId)) as folderItem) when isFolderSplitted folderPath ->
            folders.[folderPath]
            |> Seq.iter (fun folderItem ->
                match folderItem with
                | FolderItem (_, id) ->
                    if id.Identity > folderId.Identity then id.Identity <- id.Identity - 1
                | _ -> ())

            removeItem folderRefresher itemUpdater folderItem
            folderRefresher folderItem.Parent
        | _ -> ()

    and removeItem refreshFolder update (item: FSharpProjectItem) =
        let siblings = getChildrenSorted item.Parent |> List.ofSeq
        let itemBefore = siblings |> List.tryFind (fun i -> i.SortKey = item.SortKey - 1)
        let itemAfter = siblings |> List.tryFind (fun i -> i.SortKey = item.SortKey + 1)

        getChildrenSorted (ProjectItem item)
        |> Seq.iter (removeItem refreshFolder update)

        tryJoinRelativeFolders itemBefore itemAfter refreshFolder update
        match item with
        | FileItem _ -> files.Remove(item.PhysicalPath) |> ignore
        | _ -> folders.RemoveValue(item.PhysicalPath, item)
        removeChild item

        moveFollowingItems item.Parent item.SortKey MoveDirection.Up update
        removeSplittedFolderIfEmpty item.Parent item.LogicalPath.Parent refreshFolder update

    and tryJoinRelativeFolders itemBefore itemAfter folderRefresher itemUpdater =
        match itemBefore, itemAfter with
        | Some (FolderItem _ as itemBefore), Some (FolderItem _ as itemAfter) when
                itemBefore.PhysicalPath = itemAfter.PhysicalPath ->

            let folderAfterChildren = getChildrenSorted (ProjectItem itemAfter) |> List.ofSeq
            let folderBeforeChildren = getChildrenSorted (ProjectItem itemBefore) |> List.ofSeq

            let folderBeforeChildrenCount = folderBeforeChildren |> List.length
            folderAfterChildren |> List.iteri (fun i child ->
                let oldParent = child.Parent
                child.ItemInfo.Parent <- ProjectItem itemBefore
                child.ItemInfo.SortKey <- folderBeforeChildrenCount + i + 1

                children.RemoveValue(oldParent, child)
                addChild child)

            folders.RemoveValue(itemAfter.PhysicalPath, itemAfter)
            children.RemoveKey(ProjectItem itemAfter)
            removeChild itemAfter

            moveFollowingItems itemAfter.Parent itemAfter.SortKey MoveDirection.Up itemUpdater

            let lastChildBefore = List.tryLast folderBeforeChildren
            let firstChildAfter = List.tryHead folderAfterChildren

            tryJoinRelativeFolders lastChildBefore firstChildAfter folderRefresher itemUpdater
            folderRefresher itemBefore.Parent
        | _ -> ()

    let rec tryGetAdjacentRelativeItem relativeToItem modifiedItemBuildAction relativeToType =
        match relativeToItem with
        | Project _ -> None
        | ProjectItem relativeToItem ->
            tryGetAdjacentItemInParent relativeToItem relativeToType
            |> Option.bind (fun adjacentItem ->
                if canBeRelative adjacentItem modifiedItemBuildAction then Some (adjacentItem, relativeToType) else

                tryGetRelativeChildItem (ProjectItem adjacentItem) modifiedItemBuildAction (changeDirection relativeToType)
                |> Option.map (fun (item, _) -> item, relativeToType))
            |> Option.orElseWith (fun _ ->
                // todo: check item type
                tryGetAdjacentRelativeItem relativeToItem.Parent modifiedItemBuildAction relativeToType)

    let createNewItemInfo (path: VirtualFileSystemPath) logicalPath relativeToPath relativeToType refreshFolder update =
        let tryGetPossiblyRelativeNodeItem path =
            if isNull path then None else
            tryGetFile path
            |> Option.orElseWith (fun _ ->
                let folders = folders.[path]
                if folders.Count <> 1 then None else
                match folders.[0] with
                | EmptyFolder _ as item -> Some item
                | _ -> None)

        let parent, sortKey =
            match tryGetPossiblyRelativeNodeItem relativeToPath, relativeToType with
            | Some relativeItem, Some relativeToType ->

                // Try adjacent item, if its path matches new item path better (i.e. shares a longer common path)
                let relativeItem, relativeToType =
                    match tryGetAdjacentRelativeItem (ProjectItem relativeItem) None relativeToType with
                    | Some (adjacentItem, relativeToType) when
                            relativeToPath.Parent <> path.Parent &&

                            let relativeCommonParent = getCommonParent logicalPath relativeItem.LogicalPath
                            let adjacentCommonParent = getCommonParent logicalPath adjacentItem.LogicalPath
                            relativeCommonParent.IsPrefixOf(adjacentCommonParent) ->
                        adjacentItem, changeDirection relativeToType
                    | _ -> relativeItem, relativeToType

                let relativeItemParent =
                    match relativeItem with
                    | FolderItem _ when relativeItem.LogicalPath = relativeToPath -> ProjectItem relativeItem
                    | _ -> relativeItem.Parent

                let parent, sortKey =
                    match relativeItemParent with
                    | ProjectItem (EmptyFolder _) when path.Parent = relativeToPath ->
                        ProjectItem relativeItem, 1

                    | ProjectItem item when item.LogicalPath = logicalPath.Parent ->
                        relativeItemParent, getNewRelativeSortKey relativeItem relativeToType
                    | _ ->
                        // The new item is not in the same folder as the relative item.
                        // We should add new folders and split the relative item parent if needed.
                        createFoldersForItem logicalPath relativeItem relativeToType refreshFolder update

                moveFollowingItems parent sortKey MoveDirection.Down update
                parent, sortKey
            | _ ->
                let parent =
                    logicalPath.GetParentDirectories()
                    |> Seq.takeWhile (fun p -> p <> projectDirectory)
                    |> Seq.rev
                    |> Seq.fold (getOrCreateFolder refreshFolder) project
                parent, getNewSortKey parent

        ItemInfo.Create(path, logicalPath, parent, sortKey)

    let iter f =
        let rec iter (parent: FSharpProjectModelElement) =
            for item in getChildrenSorted parent do
                f item
                iter (ProjectItem item)
        iter project

    member x.Update(items) =
        let folders = Stack()
        folders.Push(State.Create(projectDirectory, project))

        let parsePaths (item: RdProjectItem) =
            let path = VirtualFileSystemPath.TryParse(item.EvaluatedInclude, InteractionContext.SolutionContext)
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
                if not (projectDirectory.IsPrefixOf(logicalPath)) then
                    logger.Warn("Invalid logical path {0} for project dir: {1}", logicalPath, projectDirectory) else

                if logicalPath.Directory <> folders.Peek().Path then
                    let commonParent = FileSystemPathEx.GetDeepestCommonParent(logicalPath.Parent, folders.Peek().Path)
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
                        let isThisProjectItem = item.Item.Origin :? RdThisProjectItemOrigin
                        let item = FileItem (itemInfo, buildAction, item.TargetFrameworkIds, isThisProjectItem)
                        files.Add(physicalPath, item)
                        addChild item
            | _ -> ()

    member x.Write(writer: UnsafeWriter) =
        let writeTargetFrameworkIds ids =
            writer.Write(UnsafeWriter.WriteDelegate<_>(fun writer (value: TargetFrameworkId) ->
                value.Write(writer)), ids)

        writer.Write(projectDirectory)
        writer.Write(projectUniqueName)
        writeTargetFrameworkIds targetFrameworkIds
        writer.Write(files.Count + folders.AllValues.Count)

        let foldersIds = Dictionary<FSharpProjectModelElement, int>()
        let getFolderId el =
            foldersIds.GetOrCreateValue(el, fun () -> foldersIds.Count)
        foldersIds.[project] <- 0

        iter (fun projectItem ->
            let info = projectItem.ItemInfo
            writer.Write(info.PhysicalPath)
            writer.Write(info.LogicalPath)
            writer.Write(getFolderId info.Parent)
            writer.Write(info.SortKey)

            match projectItem with
            | FileItem (_, buildAction, targetFrameworks, isThisProjectItem) ->
                writer.Write(int FSharpProjectItemType.File)
                writer.Write(buildAction.Value)
                writeTargetFrameworkIds targetFrameworks
                writer.Write(isThisProjectItem)

            | FolderItem (_, identity) ->
                writer.Write(int FSharpProjectItemType.Folder)
                writer.Write(getFolderId (ProjectItem projectItem))
                writer.Write(identity.Identity))

    member private x.AddItem(item: FSharpProjectItem) =
        let path = item.PhysicalPath
        match item with
        | FileItem _ -> files.[path] <- item
        | FolderItem _ -> folders.AddValue(path, item)

        addChild item

    static member Read(reader: UnsafeReader) =
        let projectDirectory = reader.ReadCurrentSolutionVirtualFileSystemPath()
        let projectUniqueName = reader.ReadString()
        let targetFrameworkIdIntern = DataIntern(setComparer)
        let readTargetFrameworkIds () =
            let ids = reader.ReadCollection(UnsafeReader.ReadDelegate<_>(TargetFrameworkId.Read), fun _ -> HashSet())
            targetFrameworkIdIntern.Intern(ids)

        let logger = Logger.GetLogger<FSharpItemsContainer>()
        let mapping = ProjectMapping(projectDirectory, projectUniqueName, readTargetFrameworkIds (), logger)
        let foldersById = Dictionary<int, FSharpProjectModelElement>()
        foldersById.[0] <- Project projectDirectory

        let itemsCount = reader.ReadInt()
        for _ in 1 .. itemsCount do
            let itemInfo =
                { PhysicalPath = reader.ReadCurrentSolutionVirtualFileSystemPath()
                  LogicalPath = reader.ReadCurrentSolutionVirtualFileSystemPath()
                  Parent = foldersById.[reader.ReadInt()]
                  SortKey = reader.ReadInt() }

            let item =
                match reader.ReadInt() |> LanguagePrimitives.EnumOfValue with
                | FSharpProjectItemType.File ->
                    let (BuildAction buildAction) = reader.ReadString()
                    FileItem(itemInfo, buildAction, readTargetFrameworkIds (), reader.ReadBool())

                | FSharpProjectItemType.Folder ->
                    let id = reader.ReadInt()
                    let item = FolderItem(itemInfo, { Identity = reader.ReadInt() })
                    foldersById.[id] <- ProjectItem item
                    item

                | itemType -> failwithf "got item %O" itemType
            mapping.AddItem(item)

        mapping

    static member val Marshaller =
        { new IUnsafeMarshaller<ProjectMapping> with
            member x.Marshal(writer, value) = value.Write(writer)
            member x.Unmarshal(reader) = ProjectMapping.Read(reader) }

    member x.UpdateFile(oldItemType, oldLocation, BuildAction buildAction, newLocation) =
        match tryGetFile oldLocation with
        | Some (FileItem (info, oldBuildAction, targetFrameworkIds, _) as item) ->
            Assertion.Assert(equalsIgnoreCase oldItemType oldBuildAction.Value, "old build action mismatch")

            files.Remove(oldLocation) |> ignore
            removeChild item

            let newItem = FileItem (info, buildAction, targetFrameworkIds, true)
            files.Add(newLocation, newItem)
            addChild newItem

            if oldLocation <> newLocation then
                // renaming linked files isn't currently supported, but
                info.LogicalPath <- info.LogicalPath.Directory / newLocation.Name
                info.PhysicalPath <- info.PhysicalPath.Directory / newLocation.Name
        | item -> failwithf "got item %O" item

    member x.RemoveFile(path, refreshFolder, update) =
        match tryGetFile path with
        | Some item -> removeItem refreshFolder update item
        | _ -> failwithf "No item found for %O" path

    member x.RemoveFolder(path, refreshFolder, update) =
        for folder in folders.[path] do
            removeItem refreshFolder update folder

    member x.UpdateFolder(oldLocation, newLocation, update) =
        Assertion.Assert(oldLocation.Parent = newLocation.Parent, "oldLocation.Parent = newLocation.Parent")
        renameFolder oldLocation newLocation update

    member x.TryGetProjectItem(viewItem: FSharpViewItem): FSharpProjectItem option =
        tryGetProjectItem viewItem

    member x.TryGetFolderItems(path: VirtualFileSystemPath): IList<FSharpProjectItem> =
        folders.[path]

    member x.AddFile(BuildAction action, path, logicalPath, relativeToPath, relativeToType, refreshFolder, update) =
        let info = createNewItemInfo path logicalPath relativeToPath relativeToType refreshFolder update
        let item = FileItem(info, action, targetFrameworkIds, true)

        files.Add(path, item)
        addChild item

    member x.AddFolder(path, relativeToPath, relativeToType, refreshFolder, update) =
        let info = createNewItemInfo path path relativeToPath relativeToType refreshFolder update
        let item = FolderItem(info, getNewFolderIdentity path)

        folders.AddValue(path, item)
        addChild item

    member x.TryGetRelativeChildPath(modifiedViewItem, relativeItem: FSharpProjectModelElement, relativeToType) =
        let modifiedItemBuildAction =
            modifiedViewItem
            |> Option.bind tryGetProjectItem
            |> Option.bind (function | FileItem (_, buildAction, _, _) -> Some buildAction | _ -> None)

        let modifiedItemLocation =
            modifiedViewItem
            |> Option.map (fun item -> item.ProjectItem.Location)
            |> Option.defaultValue null

        match getRelativeChildPathImpl relativeItem modifiedItemBuildAction relativeToType with
        | Some (relativeChildItem, relativeToType) when
                 relativeChildItem.PhysicalPath = modifiedItemLocation ||
                 relativeChildItem.IsNonThisProjectFileItem ->

            // When moving files, we remove each file first and then we add it next to the relative item.
            // An item should not be relative to itself as we won't be able to find place to insert after removing.
            // We need to find another item to be relative to.
            match tryGetAdjacentRelativeItem (ProjectItem relativeChildItem) modifiedItemBuildAction relativeToType with
            | Some (adjacentItem, relativeToType) ->
                  Some (adjacentItem.PhysicalPath, changeDirection relativeToType)
            | _ ->
                // There were no adjacent items in this direction, try the other one.
                let relativeToType = changeDirection relativeToType
                tryGetAdjacentRelativeItem (ProjectItem relativeChildItem) modifiedItemBuildAction relativeToType
                |> Option.map (fun (item, relativeToType) -> item.PhysicalPath, changeDirection relativeToType)

        | Some (item, relativeToType) -> Some (item.PhysicalPath, relativeToType)
        | _ -> None

    member x.GetProjectItemsPaths(targetFrameworkId) =
        let result = List()
        iter (function
            | FileItem (info, buildAction, ids, _) when ids.Contains(targetFrameworkId) ->
                result.Add((info.PhysicalPath, buildAction))
            | _ -> ())
        result.ToArray()

    member x.Dump(writer: TextWriter) =
        let rec dump (parent: FSharpProjectModelElement) ident =
            for item in getChildrenSorted parent do
                writer.WriteLine(sprintf "%s%d:%O" ident item.SortKey item)
                dump (ProjectItem item) (ident + "  ")
        dump project ""

        for targetFrameworkId in targetFrameworkIds do
            writer.WriteLine()
            writer.WriteLine(targetFrameworkId)
            for path, _ in x.GetProjectItemsPaths(targetFrameworkId) do
                let (UnixSeparators path) = path.MakeRelativeTo(projectDirectory)
                writer.WriteLine(path)
            writer.WriteLine()

    member x.DumpToString() =
        let writer = new StringWriter()
        x.Dump(writer)
        writer.ToString()

    static member val DummyMapping =
        ProjectMapping(VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext), String.Empty, EmptySet.Instance, DummyLogger.Instance)


[<Struct>]
type FSharpProjectModelElement =
    | Project of path: VirtualFileSystemPath
    | ProjectItem of item: FSharpProjectItem

    member x.GetProjectDirectory() =
        match x with
        | Project path -> path
        | ProjectItem item -> item.Parent.GetProjectDirectory()

[<ReferenceEquality>]
[<StructuredFormatDisplay("{RelativePhysicalPath}")>]
type FSharpProjectItem =
    | FileItem of ItemInfo * BuildAction * ISet<TargetFrameworkId> * isThisProjectItem: bool
    | FolderItem of ItemInfo * FSharpViewFolderIdentity

    member x.ItemInfo =
        match x with
        | FileItem(info, _, _, _)
        | FolderItem(info, _) -> info

    member x.SortKey = x.ItemInfo.SortKey
    member x.Parent  = x.ItemInfo.Parent
    member x.PhysicalPath: VirtualFileSystemPath = x.ItemInfo.PhysicalPath
    member x.LogicalPath: VirtualFileSystemPath = x.ItemInfo.LogicalPath

    member x.ProjectDirectory = x.Parent.GetProjectDirectory()
    member x.RelativePhysicalPath = x.PhysicalPath.MakeRelativeTo(x.ProjectDirectory)

    // todo: it will break for an imported empty folder item, check folders properly
    member x.IsNonThisProjectFileItem =
        match x with
        | FileItem(_, _, _, isThisProjectItem) -> not isThisProjectItem
        | _ -> false

    override x.ToString() =
        let name =
            match x with
            | FolderItem (_, id) ->
                sprintf "%s[%d]" x.LogicalPath.Name id.Identity

            | FileItem (_, buildAction, _, _) when not (buildAction.IsCompile()) ->
                sprintf "%s (%O)" x.LogicalPath.Name buildAction

            | _ -> x.LogicalPath.Name

        if x.PhysicalPath = x.LogicalPath then name else

        let (UnixSeparators path) = x.RelativePhysicalPath
        sprintf "%s (from %s)" name path

type FSharpProjectItemType =
    | File = 0
    | Folder = 1


type ItemInfo =
    { mutable PhysicalPath: VirtualFileSystemPath
      mutable LogicalPath: VirtualFileSystemPath
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
    { Path: VirtualFileSystemPath
      Folder: FSharpProjectModelElement
      mutable NextSortKey: int }

    static member Create(path, folder: FSharpProjectModelElement) =
        { Path = path; Folder = folder; NextSortKey = 0 }


type RdProjectItemWithTargetFrameworks =
    { Item: RdProjectItem
      TargetFrameworkIds: HashSet<TargetFrameworkId> }


type IFSharpItemsContainerRefresher =
    /// Refresh the project tree structure for a project.
    abstract member RefreshProject: IProjectMark * isOnProjectLoad: bool -> unit

    /// Refresh the project tree structure for a folder in a project.
    abstract member RefreshFolder: IProjectMark * folder: VirtualFileSystemPath * identity: FSharpViewFolderIdentity -> unit

    /// Update view item presentation (e.g. change sort key).
    abstract member UpdateFile: IProjectMark * file: VirtualFileSystemPath -> unit

    /// Update view item presentation (e.g. change sort key).
    abstract member UpdateFolder: IProjectMark * folder: VirtualFileSystemPath * identity: FSharpViewFolderIdentity -> unit

    /// Used on changes we currently cannot process, e.g. Compile -> CompileBefore build action change.
    abstract member ReloadProject: IProjectMark -> unit

    /// Select view item after a project structure change that could collapse the item parent folder.
    abstract member SelectItem: IProjectMark * VirtualFileSystemPath -> unit


[<SolutionInstanceComponent>]
type FSharpItemsContainerRefresher(lifetime: Lifetime, solution: ISolution, viewHost: ProjectModelViewHost) =

    let tryGetProject projectMark =
        solution.GetProjectByMark(projectMark) |> Option.ofObj

    let refresh projectMark getFolders =
        use lock = solution.Locks.UsingReadLock()
        solution.Locks.QueueReadLock(lifetime, "Refresh View", fun _ ->
            tryGetProject projectMark
            |> Option.iter (fun project ->
                for projectFolder in getFolders project do
                        match solution.TryGetComponent<ProjectModelAppender>() with
                        | null -> ()
                        | appender -> appender.Refresh(projectFolder)))

    let update projectMark path viewItemCtor =
        use lock = solution.Locks.UsingReadLock()
        solution.Locks.QueueReadLock(lifetime, "Update Items View", fun _ ->
            tryGetProject projectMark
            |> Option.iter (fun project ->
                for projectItem in project.FindProjectItemsByLocation(path) do
                    match viewItemCtor projectItem with
                    | None -> ()
                    | Some viewItem ->

                    if solution.GetComponent<FSharpItemsContainer>().IsValid(viewItem) then
                        viewHost.UpdateItemIfExists(viewItem)))

    interface IFSharpItemsContainerRefresher with
        member x.RefreshProject(projectMark, _) =
            refresh projectMark (fun project -> [project])

        // todo: single identity
        member x.RefreshFolder(projectMark, folder, _) =
            refresh projectMark (fun project -> project.FindProjectItemsByLocation(folder).OfType<IProjectFolder>())

        member x.UpdateFile(projectMark, path) =
            update projectMark path (function
                | ProjectFile file -> Some(FSharpViewFile(file))
                | _ -> None)

        member x.UpdateFolder(projectMark, path, id) =
            update projectMark path (function
                | ProjectFolder folder -> Some(FSharpViewFolder(folder, id))
                | _ -> None)

        member x.ReloadProject(projectMark) =
            let opName = sprintf "Reload %O after FSharpItemsContainer change" projectMark
            solution.Locks.QueueReadLock(lifetime, opName, fun _ ->
                solution.ProjectsHostContainer().GetComponent<ISolutionHost>().ReloadProjectAsync(projectMark))

        member x.SelectItem(projectMark, filePath) =
            let opName = sprintf "Select %O after FSharpItemsContainer change" filePath
            solution.Locks.QueueReadLock(lifetime, opName, fun _ ->
                tryGetProject projectMark
                |> Option.bind (fun project ->
                    project.FindProjectItemsByLocation(filePath).OfType<IProjectFile>() |> Seq.tryHead)
                |> Option.filter (fun projectFile -> projectFile.IsValid())
                |> Option.iter (fun projectFile ->

                let navigationManager = NavigationManager.GetInstance(solution)
                ignore (Lifetime.Using(fun lifetime ->
                    let points =
                        navigationManager
                            .GetNavigationPoints<ISolutionExplorerNavigationProvider, IProjectItem>(projectFile)
                    let solutionExplorerDataContext = solution.GetComponent<DataContexts>().CreateOnSelection(lifetime)
                    let caption = RichText("Navigate to Solution Explorer")
                    let options = NavigationOptions.FromDataContext(solutionExplorerDataContext, caption, true)
                    navigationManager.Navigate(points, options)))))


[<CustomEquality; NoComparison>]
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

    override x.GetHashCode() = x.ProjectItem.GetHashCode()
    override x.Equals(other) =
        match other with
        | :? FSharpViewItem as other ->
            match x, other with
            | FSharpViewFile f1, FSharpViewFile f2 -> f1.Equals(f2)
            | FSharpViewFolder (f1, id1), FSharpViewFolder (f2, id2) -> f1.Equals(f2) && id1.Equals(id2)
            | _ -> false
        | :? IProjectElementHolder as other -> other.Element.Equals(x.ProjectItem)
        | _ -> false

    interface IProjectElementHolder with
        member x.Element = x.ProjectItem :> _
        member x.Value = x.ProjectItem :> _

    interface IEquatable<IProjectElementHolder> with
        member x.Equals(other) = other.Element.Equals(x.ProjectItem)


type FSharpViewFolderIdentity =
    { mutable Identity: int }

    override x.ToString() = x.Identity.ToString()


[<SolutionFeaturePart>]
type FSharpItemModificationContextProvider(container: IFSharpItemsContainer) =
    inherit OrderingContextProvider()

    override x.IsApplicable(project) = project.IsFSharp

    override x.CreateOrderingContext(modifiedItems, relativeItems, relativeToType) =
        let modifiedItem = modifiedItems.FirstOrDefault()
        let relativeItem = relativeItems.FirstOrDefault(fun item -> item :? FSharpViewItem || item :? IProject)

        let context =
            match modifiedItem with
            | :? FSharpViewItem as modifiedViewItem ->
                x.CreateModificationContext(Some modifiedViewItem, relativeItem, relativeToType)

            | null ->
                x.CreateModificationContext(None, relativeItem, relativeToType)

            | _ -> None

        match context with
        | Some context -> context
        | _ -> base.CreateOrderingContext(modifiedItems, relativeItems, relativeToType)

    member x.CreateModificationContext(modifiedViewItem, relativeViewItem: obj, relativeToType) =
        let project, relativeElement =
            match relativeViewItem with
            | :? FSharpViewItem as viewItem -> viewItem.ProjectItem.GetProject(), Some viewItem
            | :? IProject as project -> project, None
            | _ -> failwithf "Relative item: %O" relativeViewItem

        container.TryGetRelativeChildPath(project.GetProjectMark(), modifiedViewItem, relativeElement, relativeToType)
        |> Option.bind (fun (path, relativeToType) ->
            match project.FindProjectItemsByLocation(path).FirstOrDefault() with
            | null -> None
            | item -> Some(OrderingContext(RelativeTo(item, relativeToType))))


[<ShellComponent>]
type FSharpModificationSettingsProvider() =
    interface IMsBuildModificationSettingsProvider with
        member x.SmartModificationsFilter = ["fsproj"] :> _


//[<SolutionInstanceComponent>]
type FSharpBuildActionsProvider() =
    inherit MsBuildDefaultBuildActionsProvider()

    let buildActions =
        [| BuildActions.compileBefore
           BuildActions.compileAfter |]

    override x.DefaultBuildActions = buildActions :> _
    override x.IsApplicable(projectProperties) = projectProperties :? FSharpProjectProperties
