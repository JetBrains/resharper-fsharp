module rec JetBrains.ReSharper.Plugins.FSharp.Paket

open JetBrains.Application
open JetBrains.ProjectModel.ProjectsHost.MsBuild
open JetBrains.ProjectModel.ProjectsHost.MsBuild.Strategies
open JetBrains.Application.BuildScript.Application.Zones

let [<Literal>] paketTargets = "Paket.Restore.targets"

[<ShellComponent>]
[<ZoneMarker(typeof<JetBrains.ProjectModel.ProjectsHost.SolutionHost.IHostSolutionZone>)>]
type PaketTargetsProjectLoadModificator() =
    interface MsBuildLegacyLoadStrategy.IModificator with
        member x.IsApplicable(projectMark) =
            projectMark.HasPossibleImport(paketTargets)

        member x.Modify(targets) =
            targets.Add("PaketRestore")
