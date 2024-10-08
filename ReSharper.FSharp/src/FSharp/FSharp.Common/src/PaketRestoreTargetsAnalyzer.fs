module rec JetBrains.ReSharper.Plugins.FSharp.Paket

open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.Application.Parts
open JetBrains.ProjectModel
open JetBrains.ProjectModel.ProjectsHost.MsBuild
open JetBrains.ProjectModel.ProjectsHost.MsBuild.Strategies
open JetBrains.ProjectModel.ProjectsHost.SolutionHost

let [<Literal>] paketTargets = "Paket.Restore.targets"

[<SolutionInstanceComponent(Instantiation.DemandAnyThreadSafe)>]
[<ZoneMarker(typeof<IHostSolutionZone>)>]
type PaketTargetsProjectLoadModificator() =
    interface MsBuildLegacyLoadStrategy.IModificator with
        member x.IsApplicable(projectMark) =
            projectMark.HasPossibleImport(paketTargets)

        member x.ModifyTargets(targets) =
            targets.Add("PaketRestore")

        member x.ModifyProperties(properties) =
            ()
