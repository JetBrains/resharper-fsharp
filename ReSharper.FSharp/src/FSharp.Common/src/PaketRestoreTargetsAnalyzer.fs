module rec JetBrains.ReSharper.Plugins.FSharp.Paket

open JetBrains.Application
open JetBrains.Application.Settings
open JetBrains.Diagnostics
open JetBrains.ProjectModel
open JetBrains.ProjectModel.DataContext
open JetBrains.ProjectModel.NuGet.Options
open JetBrains.ProjectModel.ProjectsHost.Diagnostic
open JetBrains.ProjectModel.ProjectsHost.MsBuild
open JetBrains.ProjectModel.ProjectsHost.MsBuild.Diagnostic
open JetBrains.ProjectModel.ProjectsHost.MsBuild.Strategies
open JetBrains.ProjectModel.Settings.Schema
open JetBrains.ReSharper.Host.Features.BackgroundTasks
open JetBrains.Util

let [<Literal>] paketTargets = "Paket.Restore.targets"

[<ShellComponent>]
type PaketTargetsProjectLoadModificator() =
    interface MsBuildLegacyLoadStrategy.IModificator with
        member x.IsApplicable(projectMark) =
            projectMark.HasPossibleImport(paketTargets)

        member x.Modify(targets) =
            targets.Add("PaketRestore")
