namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter

open System
open System.Collections.Generic
open System.Linq.Expressions
open System.Threading
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
open JetBrains.Util.Media
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
    | SelectedButNotFound
    | NotFound

type FantomasVersion =
    | Bundled = 1
    | SolutionDotnetTool = 2
    | GlobalDotnetTool = 3

type FantomasRunSettings = { Version: FantomasVersion * string; Path: string }
type FantomasNotificationEvent = { Event: FantomasRunValidationResult; Version: FantomasVersion * string }

//Read-write lock
[<SolutionComponent>]
type FantomasProcessSettings(lifetime, settingsProvider: FSharpFantomasSettingsProvider) =
    let minimalSupportedVersion = Version("1.1.1")
    let notificationEvent = Signal<FantomasNotificationEvent>()
    let coockie = ReaderWriterLockSlim()

    let dataCache =
        coockie
        let dict = Dictionary(3)
        dict[FantomasVersion.Bundled] <- { Version = FantomasVersion.Bundled, "1.1.1"; Path = null }, Ok
        dict[FantomasVersion.SolutionDotnetTool] <- { Version = FantomasVersion.SolutionDotnetTool, "1.1.1"; Path = "" }, Ok
        dict[FantomasVersion.GlobalDotnetTool] <- { Version = FantomasVersion.GlobalDotnetTool, "1.1.1"; Path = "" }, Ok
        dict

    let mutable selectedVersionProp: ViewableProperty<_> = null
    let mutable autoDetectedVersion: _ = FantomasVersion.Bundled

    let isValid version =
        match dataCache.TryGetValue version with
        | true, (_, Ok) -> true
        | _ -> false

    let rec chooseNextVersion badVersion =
        match badVersion with
        | FantomasVersion.SolutionDotnetTool
            when isValid FantomasVersion.GlobalDotnetTool -> FantomasVersion.GlobalDotnetTool

        | _ -> FantomasVersion.Bundled

    let calculateVersion selectedVersion =
        match selectedVersion with
        | FantomasVersionOption.AutoDetected ->
            if isValid FantomasVersion.SolutionDotnetTool then FantomasVersion.SolutionDotnetTool
            else chooseNextVersion FantomasVersion.SolutionDotnetTool

        | FantomasVersionOption.SolutionDotnetTool ->
            if isValid FantomasVersion.SolutionDotnetTool then FantomasVersion.SolutionDotnetTool
            else
                //notificationEvent.Fire({ Event = FailedToRun; Version = selectedVersion, "" })
                chooseNextVersion FantomasVersion.SolutionDotnetTool

        | FantomasVersionOption.GlobalDotnetTool ->
            if isValid FantomasVersion.GlobalDotnetTool then FantomasVersion.GlobalDotnetTool
            else
                //notificationEvent.Fire({ Event = FailedToRun; Version = selectedVersion, "" })
                chooseNextVersion FantomasVersion.GlobalDotnetTool
        
        | _ -> FantomasVersion.Bundled
    
    let validate version =
        //if Version.Parse(version) < minimalSupportedVersion then UnsupportedVersion
        //else Ok
        Ok

    do
        settingsProvider.Version.Change.Advise(lifetime, fun x ->
            if not x.HasNew || selectedVersionProp = null then () else
            selectedVersionProp.Value <- dataCache[calculateVersion x.New])

        let dotnetToolVersions = HashSet<FantomasVersion>() //just like from dotnet tools restore
        dotnetToolVersions.Add(FantomasVersion.SolutionDotnetTool) |> ignore

        let selectedVersionString = "1.2.3"
        let selectedVersionByUser = settingsProvider.Version.Value

        //TODO: replace with real one
        //TODO: move to separate function
        for version in dotnetToolVersions do
            let versionString = "1.2.3"
            let path = ""
            dataCache[version] <-
                { Version = version, versionString; Path = path }, validate versionString

        let selectedVersion = calculateVersion selectedVersionByUser
        selectedVersionProp <- ViewableProperty(dataCache[selectedVersion])
        autoDetectedVersion <- calculateVersion FantomasVersionOption.AutoDetected
        //subscribe on dotnet-tools
        //change version to dotnet-tools/global if not selected

    member x.SelectedVersion = selectedVersionProp
    member x.AutoDetectedVersion = autoDetectedVersion

    member x.TryRun(runAction: FantomasRunSettings -> unit) =
        let { Version = selectedVersion, version; Path = _ } as settings, _ = selectedVersionProp.Value
        try runAction settings
        with _ ->
            notificationEvent.Fire({ Event = FailedToRun; Version = selectedVersion, version })
            let data, _ = dataCache[selectedVersion]
            dataCache[selectedVersion] <- data, FailedToRun
            autoDetectedVersion <- calculateVersion FantomasVersionOption.AutoDetected //todo: fix
            selectedVersionProp.Value <- (chooseNextVersion selectedVersion) |> dataCache.get_Item
            x.TryRun(runAction)

    member x.GetSettings() =
        coockie
        Dictionary(dataCache)
    member x.NotificationProducer = notificationEvent


[<SolutionComponent>]
type FantomasNotificationsManager(lifetime, settings: FantomasProcessSettings,
                                  notifications: UserNotifications, optionsManager: OptionsManager) =

    let openDotnetToolsAction = UserNotificationCommand("Open dotnet-tools.json", fun _ -> ())
    let goToSettingsAction = UserNotificationCommand("Settings", fun _ -> optionsManager.BeginShowOptions("FantomasPage"))
    let solutionToolActions = [|openDotnetToolsAction; goToSettingsAction|]
    let globalToolActions = [|goToSettingsAction|]
    
    let createNotification { Event = event; Version = fantomasVersion, _ } =
        let body, commands =
            match event with
            | NotFound ->
                (match fantomasVersion with
                 | FantomasVersion.SolutionDotnetTool ->
                     """Fantomas version specified in "dotnet-tool.json" is not installed. Falling back to the bundled formatter. Install it using command to install""",
                     solutionToolActions
                 | FantomasVersion.GlobalDotnetTool ->
                     """Fantomas installed globally via 'dotnet tool install fantomas-tool' is not found. Falling back to the bundled formatter. Supported versions: 1.2.1 and later.""",
                     globalToolActions
                 | _ -> "", Array.empty)

            | FailedToRun ->
                (match fantomasVersion with
                 | FantomasVersion.SolutionDotnetTool ->
                     """Fantomas specified in "dotnet-tool.json" failed to run. Falling back to the bundled formatter.""",
                     solutionToolActions
                 | FantomasVersion.GlobalDotnetTool ->
                     """Fantomas installed globally via 'dotnet tool install fantomas-tool' failed to run. Falling back to the bundled formatter.""",
                     globalToolActions
                 | _ -> "", Array.empty)

            | UnsupportedVersion ->
                (match fantomasVersion with
                 | FantomasVersion.SolutionDotnetTool ->
                     """Fantomas version specified in "dotnet-tool.json" is not compatible with the current Rider version. Falling back to the bundled formatter. Supported formatter versions: 1.2.1 and later.""",
                     solutionToolActions
                 | FantomasVersion.GlobalDotnetTool ->
                     """Fantomas installed globally via 'dotnet tool install fantomas-tool' is not compatible with the current Rider version. Falling back to the bundled formatter. Supported versions: 1.2.1 and later.""",
                     globalToolActions
                 | _ -> "", Array.empty)
            | _ -> "", Array.empty

        notifications.CreateNotification(lifetime,
            title = "Unable to use specified Fantomas version",
            body = body,
            additionalCommands = commands) |> ignore

    do settings.NotificationProducer.Advise(lifetime, createNotification)


[<OptionsPage("FantomasPage", "Fantomas", typeof<PsiFeaturesUnsortedOptionsThemedIcons.Indent>)>]
type FantomasPage(lifetime, smartContext: OptionsSettingsSmartContext, optionsPageContext: OptionsPageContext,
                  iconHostBase: IconHostBase, settings: FantomasProcessSettings) as this =
    inherit FSharpOptionsPageBase(lifetime, optionsPageContext, smartContext)
    let _ = PsiFeaturesUnsortedOptionsThemedIcons.Indent // workaround to create assembly reference (dotnet/fsharp#3522)
    let warningIcon =  ValidationStates.validationWarning.GetIcon(iconHostBase)
    let okIcon =  iconHostBase.Transform(JetBrains.Application.UI.Icons.CommonThemedIcons.CommonThemedIcons.TransparentNothing.Id)

    let formatVersion (version: string) =
        RichText(version, TextStyle.FromForeColor(JetRgbaColors.Gray))

    let getTooltip = function
        | FailedToRun -> "The specified Fantomas version failed to run."
        | UnsupportedVersion -> "Supported Fantomas versions: 1.2.1 and later."
        | SelectedButNotFound -> "The specified Fantomas version not found. Falling back to the bundled version."
        | _ -> ""

    let formatSetting setting ({ Version = fantomasVersion, version; Path = _ }, status) =
        let description =
            match setting with
            | FantomasVersionOption.SolutionDotnetTool -> "From dotnet-tools.json"
            | FantomasVersionOption.GlobalDotnetTool -> "From .NET global tools"
            | FantomasVersionOption.Bundled -> "Bundled"
            | _ -> "Auto detected"
            |> RichText

        let version =
            match setting with
            | FantomasVersionOption.AutoDetected ->
                match fantomasVersion with
                | FantomasVersion.SolutionDotnetTool -> $" (v.{version} dotnet-tools.json)"
                | FantomasVersion.GlobalDotnetTool -> $" (v.{version} global)"
                | _ -> $" (Bundled v.{version})"
            | _ ->

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
                                        PresentComboItem (fun x y z ->
                                            let value =
                                                match y with
                                                | FantomasVersionOption.AutoDetected -> settings.AutoDetectedVersion
                                                | FantomasVersionOption.GlobalDotnetTool -> FantomasVersion.GlobalDotnetTool
                                                | FantomasVersionOption.SolutionDotnetTool -> FantomasVersion.SolutionDotnetTool
                                                | FantomasVersionOption.Bundled -> FantomasVersion.Bundled
                                            formatSetting y fantomasVersionsData[value] (*fix*)),
                                        seq {
                                            if not (fantomasVersionsData.ContainsKey(FantomasVersion.SolutionDotnetTool)) then
                                                FantomasVersionOption.SolutionDotnetTool
                                            if not (fantomasVersionsData.ContainsKey(FantomasVersion.GlobalDotnetTool)) then
                                                FantomasVersionOption.GlobalDotnetTool
                                        }
                                    ).WithValidationRule(lifetime, (fun () -> false), "Supported formatter versions: 1.1.0 through 1.2.1. Falling back to the bundled formatter.", ValidationStates.validationWarning)
                                let _, status = fantomasVersionsData[settings.AutoDetectedVersion]
                                //beComboBoxFromEnum.ValidationStyle.Value <- ValidationStyle.Border
                                //beComboBoxFromEnum.Tooltip.Value <- getTooltip status
                                beComboBoxFromEnum.SelectedIndex.Change.Advise(lifetime, fun x -> beComboBoxFromEnum.Revalidate.Fire(ValidationTrigger.ValueChanged);)
                                beComboBoxFromEnum),//.WithValidationRule(lifetime, (fun () -> false), "Supported formatter versions: 1.1.0 through 1.2.1. Falling back to the bundled formatter.")),
                            prefix = "Fantomas version") |> ignore
