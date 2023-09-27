module rec JetBrains.ReSharper.Plugins.FSharp.Paket

open JetBrains.Application
open JetBrains.ProjectModel.ProjectsHost.MsBuild
open JetBrains.ProjectModel.ProjectsHost.MsBuild.Strategies

let [<Literal>] paketTargets = "Paket.Restore.targets"

[<ShellComponent>]
type PaketTargetsProjectLoadModificator() =
    interface MsBuildLegacyLoadStrategy.IModificator with
        member x.IsApplicable(projectMark) =
            projectMark.HasPossibleImport(paketTargets)

        member x.Modify(targets) =
            targets.Add("PaketRestore")
