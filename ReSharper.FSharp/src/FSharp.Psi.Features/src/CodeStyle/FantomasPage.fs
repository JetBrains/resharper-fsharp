namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter

open System.Collections.Generic
open System.Runtime.InteropServices
open JetBrains.Application.Notifications
open JetBrains.Application.UI.Components
open JetBrains.Application.UI.Options
open JetBrains.Application.UI.Options.OptionsDialog
open JetBrains.ProjectModel
open JetBrains.ProjectModel.NuGet.DotNetTools
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Resources.Resources.Icons
open JetBrains.IDE.UI.Extensions
open JetBrains.Rider.Model
open JetBrains.Rider.Model.UIAutomation
open JetBrains.IDE.UI.Extensions.Validation

[<OptionsPage(nameof(FantomasPage), "Fantomas", typeof<PsiFeaturesUnsortedOptionsThemedIcons.Indent>)>]
type FantomasPage(lifetime, smartContext: OptionsSettingsSmartContext, optionsPageContext: OptionsPageContext,
                  [<Optional; DefaultParameterValue(null: ISolution)>] solution: ISolution,
                  uiApplication: IUIApplication) as this =
    inherit FSharpOptionsPageBase(lifetime, optionsPageContext, smartContext)

    let fantomasDetector =
        if isNull solution then FantomasDetector.Create(lifetime) else solution.GetComponent<FantomasDetector>()

    let formatSettingItem setting (settingsData: Dictionary<_, _>) =
        let { Location = location; Version = version; Status = status} = settingsData[setting]
        let description =
            match setting with
            | FantomasLocationSettings.LocalDotnetTool -> "From dotnet-tools.json"
            | FantomasLocationSettings.GlobalDotnetTool -> "From .NET global tools"
            | FantomasLocationSettings.Bundled -> "Bundled"
            | _ -> "Auto detected"

        let location =
            match setting with
            | FantomasLocationSettings.AutoDetected ->
                match location with
                | FantomasLocation.LocalDotnetTool -> $" (v.{version} dotnet-tools.json)"
                | FantomasLocation.GlobalDotnetTool -> $" (v.{version} global)"
                | _ -> $" (Bundled v.{version})"
            | _ ->

            match status with
            | Ok -> $" (v.{version})"
            | FailedToRun -> $" (v.{version} failed to run)"
            | UnsupportedVersion -> $" (v.{version} not supported)"
            | SelectedButNotFound -> " (not found)"

        RichTextModel(List [| RichStringModel(description); RichStringModel(location, ThemeColorId(ColorId.Gray))|])
            .GetBeRichText() :> BeControl

    let validate (comboBox: BeComboBox) (settingsData: Dictionary<_, _>) =
        let settingName = settingsData.Keys |> Seq.sortBy id |> Seq.item comboBox.SelectedIndex.Value
        match settingsData[settingName] with
        | { Status = FailedToRun } ->
            ValidationResult(ValidationStates.validationWarning, "The specified Fantomas version failed to run.")
        | { Status = UnsupportedVersion } ->
            ValidationResult(ValidationStates.validationWarning, $"Supported Fantomas versions: {MinimalSupportedVersion} and later.")
        | { Status = SelectedButNotFound } ->
            ValidationResult(ValidationStates.validationWarning, "The specified Fantomas version not found.")
        | _ ->
            ValidationResult(ValidationStates.validationPassed)

    let createComboBox (key: JetBrains.DataFlow.IProperty<FantomasLocationSettings>) =
        let settingsData = fantomasDetector.GetSettings()
        let beComboBoxFromEnum =
            key.GetBeComboBoxFromEnum(lifetime,
                PresentComboItem (fun x y z -> formatSettingItem y settingsData),
                seq { FantomasLocationSettings.LocalDotnetTool
                      FantomasLocationSettings.GlobalDotnetTool } |> Seq.filter (not << settingsData.ContainsKey)
            )
        let beComboBoxFromEnum = beComboBoxFromEnum.WithValidationRule(lifetime, (fun () ->
            let res = validate beComboBoxFromEnum settingsData
            struct(res.ResultMessage, res.State)))

        let validationLabel = BeValidationLabel(BeControls.GetRichText())

        let beSpanGrid = BeControls.GetSpanGrid("auto,auto")

        beComboBoxFromEnum.ValidationResult.Change.Advise(lifetime, fun x -> validationLabel.ValidationResult.Value <- ValidationResult(x.State, x.ResultMessage))
        beComboBoxFromEnum.SelectedIndex.Change.Advise(lifetime, fun x -> beComboBoxFromEnum.Revalidate.Fire(ValidationTrigger.ValueChanged))
        beComboBoxFromEnum.ValidationResult.Value <- validate beComboBoxFromEnum settingsData

        //TODO fix controls size in BeControls
        beSpanGrid
            .AddColumnElementsToNewRow(BeSizingType.Fit, false,
                                       [|"Fantomas version".GetBeLabel() :> BeControl
                                         beComboBoxFromEnum.WithMinSize(BeControlSizeFixed(BeControlSizeType.FIT_TO_CONTENT, BeControlSizeType.FIT_TO_CONTENT), lifetime)|])
            .AddColumnElementsToNewRow(BeSizingType.Fit, false,
                                       [|"".GetBeLabel() :> BeControl
                                         validationLabel.WithFixedSize(BeControlSizeCustom(1, 1), lifetime) |])

    do
        use indent = this.Indent()

        this.AddControl((fun (key: FSharpFantomasOptions) -> key.Location), createComboBox) |> ignore

        this.AddCommentText("To use a specified Fantomas version, install it globally via 'dotnet tool install fantomas-tool -g'\n" +
                            "or specify it in dotnet-tools.json file in the solution directory.")
        this.AddLinkButton("DotnetToolsLink", "Learn more", fun () -> uiApplication.OpenUri("https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install")) |> ignore


[<SolutionComponent>]
type FantomasNotificationsManager(lifetime, settings: FantomasDetector, notifications: UserNotifications,
                                  optionsManager: OptionsManager, dotnetToolsTracker: NuGetDotnetToolsTracker,
                                  uiApplication: IUIApplication) =
    let goToSettings = [| UserNotificationCommand("Settings", fun _ -> optionsManager.BeginShowOptions(nameof(FantomasPage))) |]
    let goToYouTrack = [| UserNotificationCommand("Report issue", fun _ -> uiApplication.OpenUri("https://youtrack.jetbrains.com/newissue?project=RIDER&clearDraft=true")) |]
    let openDotnetToolsOrGoToSettings toolsManifestPath =
        [| UserNotificationCommand("Open dotnet-tools.json", fun _ -> uiApplication.OpenUri(toolsManifestPath))
           goToSettings[0] |]

    let createFallbackMessage = function
        | LocalDotnetTool -> ""
        | GlobalDotnetTool -> "<b>Falling back to the global dotnet tool Fantomas.</b>"
        | Bundled -> "<b>Falling back to the bundled formatter.</b>"

    let createBodyMessage { Event = event; Location = version; FallbackLocation = fallbackVersion } =
        let fallbackMessage = createFallbackMessage fallbackVersion

        match event with
        | SelectedButNotFound ->
            match version with
            | FantomasLocation.LocalDotnetTool ->
                $"""dotnet-tools.json file not found in the solution directory.<br>{fallbackMessage}"""
            | FantomasLocation.GlobalDotnetTool ->
                $"""Fantomas is not installed globally.<br>{fallbackMessage}"""
            | _ -> ""

        | FailedToRun ->
            match version with
            | FantomasLocation.LocalDotnetTool ->
                $"""Fantomas specified in 'dotnet-tool.json' failed to run.<br>{fallbackMessage}"""
            | FantomasLocation.GlobalDotnetTool ->
                $"""Fantomas installed globally via 'dotnet tool install fantomas-tool' failed to run.<br>{fallbackMessage}"""
            | FantomasLocation.Bundled ->
                "An unexpected error has occurred. Please report this problem through the bug tracker."

        | UnsupportedVersion ->
            match version with
            | FantomasLocation.LocalDotnetTool ->
                $"""Fantomas specified in 'dotnet-tool.json' is not compatible with the current Rider version.<br>{fallbackMessage}<br>Supported versions: {MinimalSupportedVersion} and later."""
            | FantomasLocation.GlobalDotnetTool ->
                $"""Fantomas installed globally via 'dotnet tool install fantomas-tool' is not compatible with the current Rider version.<br>{fallbackMessage}<br>Supported versions: {MinimalSupportedVersion} and later."""
            | _ -> ""
        | _ -> ""

    let getCommands = function
        | FantomasLocation.Bundled ->
            goToYouTrack
        | FantomasLocation.LocalDotnetTool ->
            let manifestPath = dotnetToolsTracker.GetSolutionManifestPath()
            if isNotNull manifestPath && manifestPath.ExistsFile then
                openDotnetToolsOrGoToSettings manifestPath.FullPath
            else goToSettings
        | _ -> goToSettings

    let createNotification (notification: FantomasDiagnosticNotification) =
        let title =
            match notification.Location with
            | Bundled -> "Unable to use bundled Fantomas"
            | _ -> "Unable to use specified Fantomas version"

        let body = createBodyMessage notification
        let commands = getCommands notification.Location

        notifications.CreateNotification(lifetime,
            title = title,
            body = body,
            additionalCommands = commands) |> ignore

    do settings.NotificationProducer.Advise(lifetime, createNotification)
