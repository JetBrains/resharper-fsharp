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


[<SolutionInstanceComponent>]
type PaketRestoreTargetsAnalyzer(lifetime, solution: ISolution, settingsStore: ISettingsStore, logger: ILogger) =
    let mutable restoreOptionsWereReset = false

    let settings = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()))
    let restoreResetProp = settings.GetValueProperty(lifetime, fun key -> key.RestoreOptionsWereReset)
    let restoreEnabledProp = settings.GetValueProperty(lifetime, fun (s: NuGetOptions) -> s.ConfigRestoreEnabled)
    
    interface IMsBuildProjectLoadDiagnosticProvider with
        member x.CollectDiagnostic(projectMark, _, _) =
            match restoreOptionsWereReset, projectMark.HasPossibleImport(paketTargets) with
            | true, _ | _, false -> EmptyList.Instance :> _
            | _ ->

            match restoreResetProp.GetValue() with
            | true ->
                restoreOptionsWereReset <- true
                EmptyList.Instance :> _
            | _ ->

            match restoreEnabledProp.GetValue() with
            | NuGetOptionConfigPolicy.Disable ->
                settings.ResetValue((fun (s: NuGetOptions) -> s.ConfigRestoreEnabled))
                settings.SetValue((fun key -> key.RestoreOptionsWereReset), true)
                logger.LogMessage(LoggingLevel.WARN, "Found core project using Paket. Reset NuGet restore options.")

                restoreOptionsWereReset <- true
                [| NuGetRestoreEnabledMessage.InstanceDiagnostic |] :> _

            | _ -> EmptyList.Instance :> _


type NuGetRestoreEnabledMessage(title, message) =
    inherit LoadDiagnosticMessage(title, message)

    static member val InstanceDiagnostic =
        let message =
            "Restore settings were restored to default value for this solution.\n" +
            RiderContextNotificationHelper.MakeOpenSettingsLink("NuGet", "NuGet settings")

        NuGetRestoreEnabledMessage("NuGet restore options were reset", message) :> ILoadDiagnostic


[<SettingsKey(typeof<HierarchySettings>, "RestoreOptionsWereReset")>]
type RestoreResetOptions =
    { [<SettingsEntry(false, "RestoreOptionsWereReset"); DefaultValue>]
      mutable RestoreOptionsWereReset: bool }
