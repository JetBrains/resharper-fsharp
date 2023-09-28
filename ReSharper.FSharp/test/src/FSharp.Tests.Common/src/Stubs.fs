namespace JetBrains.ReSharper.Plugins.FSharp.Tests

open JetBrains.Application
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Components
open JetBrains.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Host.ProjectItems.ItemsContainer
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi
open JetBrains.ReSharper.Plugins.FSharp.Tests

[<ShellComponent>]
[<ZoneMarker(typeof<ITestFSharpPluginZone>)>]
type FSharpFileServiceStub() =

    interface IFSharpFileService with
        member x.IsScratchFile _ = false
        member x.IsScriptLike _ = false

[<SolutionInstanceComponent>]
[<ZoneMarker(typeof<ITestFSharpPluginZone>)>]
type StubFSharpItemsContainer() =
    interface IFSharpItemsContainer with
        member this.AdviseFSharpProjectLoaded lifetime func = ()

        member this.RemoveProject project = ()

        member this.TryGetSortKey item = None

        member this.TryGetParentFolderIdentity item = None

        member this.CreateFoldersWithParents projectFolder = []

        member this.GetProjectItemsPaths (projectMark, targetFrameworkId) = [||]

        member this.Dump writer = ()

        member this.TryGetRelativeChildPath (projectMark, modifiedItem, relativeItem, relativeToType) = None
        member this.OnAddFile(projectMark, itemType, location, linkedPath, relativeTo, relativeToType) = ()
        member this.OnAddFolder(projectMark, location, relativeTo, relativeToType) = ()
        member this.OnProjectLoaded(projectMark, projectDescriptor, msBuildProject) = ()
        member this.OnRemoveFile(projectMark, itemType, location) = ()
        member this.OnRemoveFolder(projectMark, location) = ()
        member this.OnUpdateFile(projectMark, oldItemType, oldLocation, newItemType, newLocation) = ()
        member this.OnUpdateFolder(projectMark, oldLocation, newLocation) = ()