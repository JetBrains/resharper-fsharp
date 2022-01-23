namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter

open System
open System.Collections.Generic
open System.Drawing
open System.Linq.Expressions
open JetBrains.Application.Components
open JetBrains.Application.Notifications
open JetBrains.Application.UI.Options
open JetBrains.Application.UI.Options.OptionsDialog
open JetBrains.Collections.Viewable
open JetBrains.IDE.UI
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.OptionPages.CodeStyle
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Resources.Resources.Icons
open JetBrains.IDE.UI.Extensions
open JetBrains.Rider.Model.UIAutomation
open JetBrains.UI.RichText
open JetBrains.ReSharper.Feature.Services.UI.Validation
open JetBrains.IDE.UI.Extensions
open JetBrains.IDE.UI.Extensions.Validation

[<CodePreviewPreparatorComponent>]
type FSharpCodePreviewPreparator() =
    inherit CodePreviewPreparator()

    override x.Language = FSharpLanguage.Instance :> _
    override x.ProjectFileType = FSharpProjectFileType.Instance :> _
    override x.Parse(parser,_) = parser.ParseFile() :> _


[<FormattingSettingsPresentationComponent>]
type FSharpCodeStylePageSchema(lifetime, smartContext, itemViewModelFactory, container, settingsToHide) =
    inherit IndentStylePageSchema<FSharpFormatSettingsKey, FSharpCodePreviewPreparator>(lifetime, smartContext,
        itemViewModelFactory, container, settingsToHide)

    override x.Language = FSharpLanguage.Instance :> _
    override x.PageName = "Formatting Style"

    member x.GetItem(getter: Expression<Func<FSharpFormatSettingsKey,_>>) =
        base.ItemFor(getter)

    override x.Describe() =
        [| x.GetItem(fun key -> key.WRAP_LIMIT)
           x.GetItem(fun key -> key.INDENT_SIZE)
           x.GetItem(fun key -> key.SemicolonAtEndOfLine)
           x.GetItem(fun key -> key.SpaceBeforeParameter)
           x.GetItem(fun key -> key.SpaceBeforeLowercaseInvocation)
           x.GetItem(fun key -> key.SpaceBeforeUppercaseInvocation)
           x.GetItem(fun key -> key.SpaceBeforeClassConstructor)
           x.GetItem(fun key -> key.SpaceBeforeMember)
           x.GetItem(fun key -> key.SpaceBeforeColon)
           x.GetItem(fun key -> key.SpaceAfterComma)
           x.GetItem(fun key -> key.SpaceBeforeSemicolon)
           x.GetItem(fun key -> key.SpaceAfterSemicolon)
           x.GetItem(fun key -> key.IndentOnTryWith)
           x.GetItem(fun key -> key.SpaceAroundDelimiter)
           x.GetItem(fun key -> key.MaxIfThenElseShortWidth)
           x.GetItem(fun key -> key.MaxInfixOperatorExpression)
           x.GetItem(fun key -> key.MaxRecordWidth)
           x.GetItem(fun key -> key.MaxArrayOrListWidth)
           x.GetItem(fun key -> key.MaxValueBindingWidth)
           x.GetItem(fun key -> key.MaxFunctionBindingWidth)
           x.GetItem(fun key -> key.MultilineBlockBracketsOnSameColumn)
           x.GetItem(fun key -> key.NewlineBetweenTypeDefinitionAndMembers)
           x.GetItem(fun key -> key.KeepIfThenInSameLine)
           x.GetItem(fun key -> key.MaxElmishWidth)
           x.GetItem(fun key -> key.SingleArgumentWebMode)
           x.GetItem(fun key -> key.AlignFunctionSignatureToIndentation)
           x.GetItem(fun key -> key.AlternativeLongMemberDefinitions)
           x.GetItem(fun key -> key.StickComment)
           x.GetItem(fun key -> key.OutdentBinaryOperators)
           x.GetItem(fun key -> key.NeverOutdentPipeOperators) |] :> _


[<OptionsPage("FSharpCodeStylePage", "Formatting Style", typeof<PsiFeaturesUnsortedOptionsThemedIcons.Indent>)>]
type FSharpCodeStylePage(lifetime, smartContext: OptionsSettingsSmartContext, env,
                         schema: FSharpCodeStylePageSchema, preview, componentContainer: IComponentContainer) =
    inherit CodeStylePage(lifetime, smartContext, env, schema, preview, componentContainer)
    let _ = PsiFeaturesUnsortedOptionsThemedIcons.Indent // workaround to create assembly reference (dotnet/fsharp#3522)

    override x.Id = "FSharpIndentStylePage"


//struct

type FantomasRunValidationResult =
    | Ok
    | FailedToRun
    | UnsupportedVersion
    | NotFound

type FantomasVersion =
    | Bundled = 1
    | SolutionDotnetTool = 2
    | GlobalDotnetTool = 3

type FantomasRunSettings = { Version: FantomasVersion * string; Path: string }
type FantomasNotificationEvent = { Event: FantomasRunValidationResult; Version: FantomasVersion * string }
//From settings and ?

[<SolutionComponent>]
type FantomasProcessSettings(lifetime, settingsProvider: FSharpFantomasSettingsProvider, optionsManager: OptionsManager) =
    let minimalSupportedVersion = Version("1.1.1")
    let notificationEvent = Signal<FantomasNotificationEvent>()

    let dataCache =
        let dict = Dictionary(3)
        dict[FantomasVersion.Bundled] <- { Version = FantomasVersion.Bundled, "1.1.1"; Path = null }, Ok
        dict[FantomasVersion.SolutionDotnetTool] <- { Version = FantomasVersion.SolutionDotnetTool, "1.1.1"; Path = "" }, Ok
        dict[FantomasVersion.GlobalDotnetTool] <- { Version = FantomasVersion.GlobalDotnetTool, "1.1.1"; Path = null }, NotFound
        dict

    let mutable selectedVersionProp: ViewableProperty<_> = null
    let mutable settingsVersion: FantomasVersionOption = FantomasVersionOption.AutoDetected

    //notifications
    //check is Valid
    let rec chooseNextVersion setting badVersion =
        match setting with
        | FantomasVersionOption.AutoDetected ->
            match badVersion with
            | FantomasVersion.SolutionDotnetTool ->
                if dataCache.ContainsKey FantomasVersion.GlobalDotnetTool then FantomasVersion.GlobalDotnetTool
                else FantomasVersion.Bundled

            | FantomasVersion.GlobalDotnetTool -> FantomasVersion.Bundled
            | _ -> FantomasVersion.Bundled //raise

        | FantomasVersionOption.SolutionDotnetTool
        | FantomasVersionOption.GlobalDotnetTool -> chooseNextVersion FantomasVersionOption.AutoDetected badVersion
        | _ -> FantomasVersion.Bundled



    let validate version =
        //if Version.Parse(version) < minimalSupportedVersion then UnsupportedVersion
        //else Ok
        Ok

    do
        settingsProvider.Version.Change.Advise(lifetime, fun x ->
            if not x.HasNew then () else
            selectedVersionProp.Value <- dataCache[x.New])

        let dotnetToolVersions = HashSet<FantomasVersion>()          //just like from dotnet tools restore
        dotnetToolVersions.Add(FantomasVersionOption.SolutionDotnetTool)

        let selectedVersionString = "1.2.3"
        let selectedVersionByUser = settingsProvider.Version.Value

        //TODO: replace with real one
        //TODO: move to separate function
        for version in dotnetToolVersions do
            let versionString = ""
            let path = ""
            dataCache[version] <-
                { Version = version, versionString; Path = path }, validate versionString

        let selectedVersion =
            match selectedVersionByUser with
            //TODO: move to common code
            | FantomasVersionOption.AutoDetected ->
                //check is valid
                if dataCache.ContainsKey FantomasVersion.SolutionDotnetTool then FantomasVersion.SolutionDotnetTool
                else chooseNextVersion settingsVersion FantomasVersion.SolutionDotnetTool

            | version ->
                if not (dotnetToolVersions.Contains selectedVersionByUser) then
                    notifications.CreateNotification(lifetime,
                        title = "Unable to use custom Fantomas version",
                        body = """Fantomas version specified in "HMHMHMHMHM" is not installed. *Falling back to the bundled version*. Install it using *command to install*.""",
                        additionalCommands = seq { UserNotificationCommand("Settings", fun _ -> optionsManager.BeginShowOptions("FantomasPage")) }) |> ignore
                    //createNotFoundNotification,
                else chooseNextVersion settingsVersion FantomasVersion.SolutionDotnetTool
                else
                match version, validate selectedVersionString with
                | _, Ok -> version
                | FantomasVersionOption.PreferSolutionDotnetTool, _ ->
                    notifications.CreateNotification(lifetime,
                        title = "Unable to use custom Fantomas",
                        body = """Fantomas specified in "dotnet-tool.json" is not compatible with the current Rider version. Falling back to the bundled version. Supported formatter versions: X""") |> ignore
                        else chooseNextVersion settingsVersion FantomasVersion.SolutionDotnetTool
                | FantomasVersionOption.PreferGlobalDotnetTool, _ ->
                    notifications.CreateNotification(lifetime,
                        title = "Unable to use custom Fantomas",
                        body = """F# formatter installed globally via 'dotnet tool install fantomas-tool' is not compatible with the current Rider version. Falling back to the bundled formatter. Supported versions: 1.2.1 and later.""") |> ignore
                else chooseNextVersion settingsVersion FantomasVersion.SolutionDotnetTool
                | _ -> FantomasVersionOption.PreferBundled

        selectedVersionProp <- ViewableProperty(dataCache[selectedVersion])
        //subscribe on dotnet-tools
        //change version to dotnet-tools/global if not selected

    member x.SelectedVersion = selectedVersionProp

    //TODO: notifications?
    member x.TryRun(runAction: unit -> unit) =
        let selectedVersion, version = selectedVersionProp.Value |> fst |> (fun x -> x.Version)
        try runAction()
        with _ ->
            notificationEvent.Fire({ Event = FailedToRun; Version = selectedVersion, version })
            let data, _ = dataCache[selectedVersion]
            dataCache[selectedVersion] <- data, FailedToRun
            selectedVersionProp.Value <- (chooseNextVersion settingsVersion selectedVersion) |> dataCache.get_Item

    member x.GetSettings() = Dictionary(dataCache)
    member x.NotificationProducer = notificationEvent


[<SolutionComponent>]
type FantomasNotificationsManager(lifetime, settings: FantomasProcessSettings, notifications: UserNotifications) =

    let createNotification { Event = event; Version = fantomasVersion, versionString } =
        let title, body, commands =
            match event with
            | FailedToRun -> ()
            | UnsupportedVersion ->
                "Unable to use custom Fantomas version",
                match fantomasVersion with
                | FantomasVersion.PreferSolutionDotnetTool ->
                    """Fantomas version specified in "dotnet-tool.json" is not compatible with the current Rider version. Falling back to the bundled formatter. Supported formatter versions: 1.2.1 and later."""
                | FantomasVersion.PreferGlobalDotnetTool ->


        notifications.CreateNotification(lifetime, title, body, additionalCommands = commands)

    do settings.NotificationProducer.Advise(lifetime, createNotification)


[<OptionsPage("FantomasPage", "Fantomas", typeof<PsiFeaturesUnsortedOptionsThemedIcons.Indent>)>]
type FantomasPage(lifetime, smartContext: OptionsSettingsSmartContext, optionsPageContext: OptionsPageContext,
                  iconHostBase: IconHostBase, settings: FantomasProcessSettings) as this =
    inherit FSharpOptionsPageBase(lifetime, optionsPageContext, smartContext)
    let _ = PsiFeaturesUnsortedOptionsThemedIcons.Indent // workaround to create assembly reference (dotnet/fsharp#3522)
    let warningIcon =  ValidationStates.validationWarning.GetIcon(iconHostBase)
    let okIcon =  iconHostBase.Transform(JetBrains.Application.UI.Icons.CommonThemedIcons.CommonThemedIcons.TransparentNothing.Id)

    let formatVersion (version: string) =
        RichText(version, TextStyle.FromForeColor(Color.Gray))

    let getTooltip = function
        | Ok -> ""
        | FailedToRun -> "The specified Fantomas version failed to run. Falling back to the bundled version."
        | UnsupportedVersion -> "Supported Fantomas versions: 1.2.1 and later. Falling back to the bundled version."
        | NotFound -> "The specified Fantomas version not found. Falling back to the bundled version."

    let formatSetting ({ Version = fantomasVersion, version; Path = _ }, status) =
        let description =
            match fantomasVersion with
            | FantomasVersionOption.SolutionDotnetTool -> "From dotnet-tools.json"
            | FantomasVersionOption.GlobalDotnetTool -> "From .NET global tools"
            | _ -> "Bundled"
            |> RichText

        let version =
            match status with
            | Ok -> $" (v.{version})"
            | FailedToRun -> $" (v.{version} failed to run)"
            | UnsupportedVersion -> $" (v.{version} not supported)"
            | NotFound -> " (not found)"

        let tooltip = getTooltip status

        let description = description + (formatVersion version)

        match status with
        | Ok -> description.GetBeRichText(okIcon, true) :> BeControl
        | _ ->
            let control = description.GetBeRichText(warningIcon, true)
            control.Tooltip.Value <- tooltip
            control :> _

    do
        use indent = this.Indent()

        this.AddComboOption((fun (key: FSharpFantomasOptions) -> key.Version),
                            (fun key ->
                                let fantomasVersionsData = settings.GetSettings()
                                let beComboBoxFromEnum =
                                    key.GetBeComboBoxFromEnum(lifetime,
                                        PresentComboItem (fun x y z -> formatSetting fantomasVersionsData[y]),
                                        seq {
                                            if not (fantomasVersionsData.ContainsKey(FantomasVersionOption.SolutionDotnetTool)) then
                                                FantomasVersionOption.SolutionDotnetTool
                                            if not (fantomasVersionsData.ContainsKey(FantomasVersionOption.GlobalDotnetTool)) then
                                                FantomasVersionOption.GlobalDotnetTool
                                        }
                                    )
                                let _, status = fantomasVersionsData[key.Value]
                                beComboBoxFromEnum.Tooltip.Value <- getTooltip status
                                beComboBoxFromEnum),//.WithValidationRule(lifetime, (fun () -> false), "Supported formatter versions: 1.1.0 through 1.2.1. Falling back to the bundled formatter.")),
                            prefix = "Version") |> ignore
