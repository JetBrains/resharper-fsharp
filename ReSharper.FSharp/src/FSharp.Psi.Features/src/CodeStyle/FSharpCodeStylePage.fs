namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter

open System
open System.Collections.Generic
open System.Drawing
open System.Linq.Expressions
open JetBrains.Application.Components
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


type FantomasRunSettings = { Version: FantomasVersion * string; Path: string }
type FantomasRunValidationResult =
    | Ok
    | FailedToRun
    | UnsupportedVersion
    | NotFound
//From settings and ?

[<SolutionComponent>]
type FantomasProcessSettings(lifetime, settingsProvider: FSharpFantomasSettingsProvider) =
    let minimalSupportedVersion = Version("1.1.1")
    let dataCache =
        let dict = Dictionary(3)
        dict[FantomasVersion.Bundled] <- { Version = FantomasVersion.Bundled, "1.1.1"; Path = null }, Ok
        dict[FantomasVersion.LocalDotnetTool] <- { Version = FantomasVersion.LocalDotnetTool, "1.1.1"; Path = "" }, Ok
        dict[FantomasVersion.GlobalDotnetTool] <- { Version = FantomasVersion.GlobalDotnetTool, "1.1.1"; Path = null }, Ok
        dict

    let mutable selectedVersion = ViewableProperty(dataCache[FantomasVersion.Bundled])

    let validate version =
        if Version.Parse(version) < minimalSupportedVersion then UnsupportedVersion
        else Ok

    do
        settingsProvider.Version.Change.Advise(lifetime, fun x ->
            if not x.HasNew then () else
            selectedVersion.Value <- dataCache[x.New])

    member x.SelectedVersion = selectedVersion

    //TODO: notifications?
    member x.TryRun(runAction: unit -> unit) =
        try runAction()
        with _ ->
            let key = selectedVersion.Value |> fst |> (fun x -> x.Version |> fst)
            let data, _ = dataCache[key]
            dataCache[key] <- data, FailedToRun
            selectedVersion.Value <- dataCache[FantomasVersion.Bundled]

    member x.GetSettings() = dataCache


[<OptionsPage("FantomasPage", "Fantomas", typeof<PsiFeaturesUnsortedOptionsThemedIcons.Indent>)>]
type FantomasPage(lifetime, smartContext: OptionsSettingsSmartContext, optionsPageContext: OptionsPageContext,
                  iconHostBase: IconHostBase, settings: FantomasProcessSettings) as this =
    inherit FSharpOptionsPageBase(lifetime, optionsPageContext, smartContext)
    let _ = PsiFeaturesUnsortedOptionsThemedIcons.Indent // workaround to create assembly reference (dotnet/fsharp#3522)
    let warningIcon =  ValidationStates.validationWarning.GetIcon(iconHostBase)

    let formatVersion (version: string) =
        RichText(version, TextStyle.FromForeColor(Color.Gray))

    let formatSetting (description: RichText) ({ Version = fantomasVersion, version; Path = _ }, status) =
        let version =
            match status with
            | Ok -> $" (v.{version})"
            | FailedToRun -> $" (v.{version} failed to run)"
            | UnsupportedVersion -> $" (v.{version} not supported)"
            | NotFound -> " (not found)"

        let description = description + (formatVersion version)

        match status with
        | Ok -> description.GetBeRichText() :> BeControl
        | _ -> description.GetBeRichText(warningIcon, true) :> _

    do
        use indent = this.Indent()

        this.AddComboOption((fun (key: FSharpFantomasOptions) -> key.Version),
                            (fun key ->
                                let fantomasVersionsData = settings.GetSettings()
                                let withValidationRule =
                                    key.GetBeComboBoxFromEnum(lifetime,
                                        //CHECK DICT IS VALID
                                        presentation = PresentComboItem (fun x y z ->
                                            match y with
                                            | FantomasVersion.LocalDotnetTool ->
                                                formatSetting "From dotnet-tools.json" fantomasVersionsData[FantomasVersion.LocalDotnetTool]
                                            | FantomasVersion.GlobalDotnetTool ->
                                                formatSetting  "From .NET global tools" fantomasVersionsData[FantomasVersion.GlobalDotnetTool]
                                            | _ -> formatSetting "Bundled" fantomasVersionsData[FantomasVersion.Bundled]),
                                        except = seq {
                                            FantomasVersion.NotSelected
                                            if not (fantomasVersionsData.ContainsKey(FantomasVersion.LocalDotnetTool)) then
                                                FantomasVersion.LocalDotnetTool
                                            if not (fantomasVersionsData.ContainsKey(FantomasVersion.GlobalDotnetTool)) then
                                                FantomasVersion.GlobalDotnetTool
                                        }).WithValidationRule(lifetime, (fun () -> false), "Supported formatter versions: 1.1.0 through 1.2.1. Falling back to the bundled formatter.")

                                withValidationRule),

                            prefix = "Version") |> ignore
        ()
