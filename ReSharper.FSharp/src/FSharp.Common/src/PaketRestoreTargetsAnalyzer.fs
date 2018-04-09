module rec JetBrains.ReSharper.Plugins.FSharp.Common.Paket

open JetBrains.Application
open JetBrains.Application.Settings
open JetBrains.Application.Settings.Implementation
open JetBrains.ProjectModel
open JetBrains.ProjectModel.DataContext
open JetBrains.ProjectModel.NuGet.Options
open JetBrains.ProjectModel.ProjectsHost.Diagnostic
open JetBrains.ProjectModel.ProjectsHost.MsBuild
open JetBrains.ProjectModel.ProjectsHost.MsBuild.Diagnostic
open JetBrains.ProjectModel.Settings.Schema
open JetBrains.ReSharper.Host.Features.BackgroundTasks
open JetBrains.Util

let [<Literal>] paketTargets = "Paket.Restore.targets"

[<ShellComponent>]
type PaketTargetsProjectLoadModificator() =
    interface IMsBuildProjectLoadModificator with
        member x.IsApplicable(projectMark) =
            projectMark.HasPossbleImport(paketTargets)

        member x.Modify(context) =
            context.Targets.Add("PaketRestore")


[<SolutionInstanceComponent>]
type PaketRestoreTargetsAnalyzer(lifetime, solution: ISolution, settingsStore: SettingsStore, logger: ILogger) =
    let mutable restoreOptionsWereReset = false

    interface IMsBuildProjectLoadDiagnosticProvider with
        member x.CollectDiagnostic(projectMark, _, _) =
            match restoreOptionsWereReset, projectMark.HasPossbleImport(paketTargets) with
            | true, _ | _, false -> EmptyList.Instance :> _
            | _ ->

            let context = solution.ToDataContext()
            let solutionSettingsStore = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(context))
            match solutionSettingsStore.GetValue(fun (s: RestoreResetOptions) -> s.RestoreOptionsWereReset) with
            | true ->
                restoreOptionsWereReset <- true
                EmptyList.Instance :> _
            | _ ->

            match solutionSettingsStore.GetValue(fun (s: NuGetOptions) -> s.ConfigRestoreEnabled) with
            | NuGetOptionConfigPolicy.Disable ->
                solutionSettingsStore.ResetValue((fun (s: NuGetOptions) -> s.ConfigRestoreEnabled))
                solutionSettingsStore.SetValue((fun (s: RestoreResetOptions) -> s.RestoreOptionsWereReset), true)
                logger.LogMessage(LoggingLevel.WARN, "Found core project using Paket. Reset NuGet restore options.")

                restoreOptionsWereReset <- true
                [NuGetRestoreEnabledMessage.InstanceDiagnostic].AsCollection()

            | _ -> EmptyList.Instance :> _


type NuGetRestoreEnabledMessage(title, message) =
    inherit LoadDiagnosticMessage(title, message)

    static member val InstanceDiagnostic =
        let message =
            "Restore settings were restored to default value for this solution.\n" +
            RiderContextNotificationHelper.MakeOpenSettingsLink("NuGet", "NuGet settings")

        NuGetRestoreEnabledMessage("NuGet restore options were reset", message) :> ILoadDiagnostic


[<SettingsKey(typeof<HierarchySettings>, "RestoreOptionsWereReset")>]
type RestoreResetOptions() =
    [<SettingsEntry(false, "RestoreOptionsWereReset"); DefaultValue>]
    val mutable RestoreOptionsWereReset: bool
