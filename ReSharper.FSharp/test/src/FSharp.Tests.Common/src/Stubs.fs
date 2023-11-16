namespace JetBrains.ReSharper.Plugins.FSharp.Tests

open JetBrains.Application
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.DataFlow
open JetBrains.ProjectModel
open JetBrains.ProjectModel.ProjectsHost
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Host.ProjectItems.ItemsContainer
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel.Scripts
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
        member this.RemoveProject _ = ()
        member this.TryGetSortKey _ = None
        member this.TryGetParentFolderIdentity _ = None
        member this.CreateFoldersWithParents _ = []
        member this.GetProjectItemsPaths(_, _) = [||]
        member this.Dump _ = ()
        member this.TryGetRelativeChildPath(_, _, _, _) = None
        member this.OnAddFile(_, _, _, _, _, _) = ()
        member this.OnAddFolder(_, _, _, _) = ()
        member this.OnProjectLoaded(_, _, _) = ()
        member this.OnRemoveFile(_, _, _) = ()
        member this.OnRemoveFolder(_, _) = ()
        member this.OnUpdateFile(_, _, _, _, _) = ()
        member this.OnUpdateFolder(_, _, _) = ()

        member val ProjectUpdated = new Signal<IProjectMark>("todo")
