namespace JetBrains.ReSharper.Plugins.FSharp.Common

open System
open System.Collections.Generic
open JetBrains.Application
open JetBrains.Application.Settings
open JetBrains.Application.Settings.Implementation
open JetBrains.ProjectModel
open JetBrains.ProjectModel.DataContext
open JetBrains.ProjectModel.NuGet.Options
open JetBrains.ProjectModel.ProjectsHost.Diagnostic
open JetBrains.ProjectModel.ProjectsHost.MsBuild
open JetBrains.ProjectModel.ProjectsHost.MsBuild.Diagnostic
open JetBrains.ProjectModel.Properties
open JetBrains.ReSharper.Host.Features.BackgroundTasks
open JetBrains.Util

type NuGetRestoreDisabledMessage(title, message) =
    inherit LoadDiagnosticMessage(title, message)
    new() =
        let nugetSettingsLink = RiderContextNotificationHelper.MakeOpenSettingsLink("NuGet", "NuGet settings")
        let message =
            "Restore was disabled for this solution because Paket restore is not currently supported. " +
            "Please use 'dotnet restore' if needed. \n" +
            nugetSettingsLink
        NuGetRestoreDisabledMessage("NuGet restore was disabled", message)

    override x.Kind = LoadDiagnosticKind.Warning

[<SolutionInstanceComponent>]
type PaketRestoreTargetsAnalyzer(lifetime, solution: ISolution, settingsStore: SettingsStore) =
    let paketPropName = "IsPaketRestoreTargetsFileLoaded"
    let diagnosticMessage = NuGetRestoreDisabledMessage()
    member val RestoreWasDisabled = false with get, set

    interface IMsBuildProjectLoadDiagnosticProvider with
        member x.CollectDiagnostic(_, _, _) =
            if x.RestoreWasDisabled then [diagnosticMessage :> ILoadDiagnostic].AsCollection()
            else EmptyList.Instance :> _

    interface IMsBuildProjectListener with
        member x.OnProjectLoaded(_, msBuildProject) =
            let solutionContext = solution.ToDataContext()
            let solutionSettingsStore = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solutionContext))
            match solutionSettingsStore.GetValue(fun (s: NuGetOptions) -> s.ConfigRestoreEnabled) with
            | NuGetOptionConfigPolicy.Disable -> ()
            | _ ->
                let props = msBuildProject.RdProjectDescription.Properties
                if props |> Seq.exists (fun p -> p.Name.Equals(paketPropName, StringComparison.OrdinalIgnoreCase)) then
                    settingsStore
                        .BindToContextLive(lifetime, ContextRange.Custom(solutionContext, solutionContext))
                        .SetValue((fun (s: NuGetOptions) -> s.ConfigRestoreEnabled), NuGetOptionConfigPolicy.Disable)
                    x.RestoreWasDisabled <- true
