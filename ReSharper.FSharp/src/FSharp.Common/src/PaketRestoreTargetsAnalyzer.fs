module rec JetBrains.ReSharper.Plugins.FSharp.Paket

open JetBrains.Application
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Platform.RdFramework
open JetBrains.ProjectModel.ProjectsHost.MsBuild
open JetBrains.ProjectModel.ProjectsHost.MsBuild.Strategies

let [<Literal>] paketTargets = "Paket.Restore.targets"

[<ShellComponent>]
[<ZoneMarker(typeof<IRdFrameworkZone>, typeof<ISinceClr4HostZone>)>]
type PaketTargetsProjectLoadModificator() =
    interface MsBuildLegacyLoadStrategy.IModificator with
        member x.IsApplicable(projectMark) =
            projectMark.HasPossibleImport(paketTargets)

        member x.Modify(targets) =
            targets.Add("PaketRestore")
