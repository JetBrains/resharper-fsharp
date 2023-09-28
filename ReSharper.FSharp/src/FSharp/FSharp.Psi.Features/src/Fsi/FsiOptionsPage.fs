namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi

open System
open System.Runtime.InteropServices
open JetBrains.Application
open JetBrains.Application.UI.Controls.FileSystem
open JetBrains.Application.UI.Options
open JetBrains.Application.UI.Options.OptionsDialog.SimpleOptions.ViewModel
open JetBrains.DataFlow
open JetBrains.Lifetimes
open JetBrains.IDE.UI
open JetBrains.IDE.UI.Extensions
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Resources
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi.FsiDetector
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.ReSharper.Plugins.FSharp.Util
open JetBrains.Rider.Backend.Features.Settings.Layers.ExportImportWorkaround
open JetBrains.Rider.Model.UIAutomation
open JetBrains.UI.RichText
open JetBrains.Util

[<OptionsPage("FsiOptionsPage", "Fsi", typeof<ProjectModelThemedIcons.Fsharp>, HelpKeyword = fsiHelpKeyword)>]
type FsiOptionsPage(lifetime: Lifetime, optionsPageContext, settings, settingsSchema, fsiDetector: FsiDetector,
        [<Optional; DefaultParameterValue(null: ISolution)>] solution: ISolution, dialogs: ICommonFileDialogs,
        iconHost: IconHostBase) as this =
    inherit FSharpOptionsPageBase(lifetime, optionsPageContext, settings)

    let (|FsiTool|) (obj: obj) = obj :?> FsiTool

    let Not = Func<_,_>(not)

    let fsiOptions = FsiOptionsProvider(lifetime, settings, settingsSchema)

    let tools = fsiDetector.GetFsiTools(solution)
    let autoDetectAllowed = tools.Length > 1

    let autoDetect =
        if autoDetectAllowed then fsiOptions.AutoDetect else
        new Property<bool>(lifetime, "FsiNoAutoDetect", false) :> _

    let getActiveTool () =
        if fsiOptions.IsCustomTool.Value || not autoDetectAllowed then customTool else
        fsiDetector.GetActiveTool(solution, fsiOptions)

    let parsedPath = fsiOptions.FsiPathAsPath
    let fsiTool = if autoDetect.Value then tools[0] else getActiveTool ()

    let initialPath = if fsiTool.IsCustom then parsedPath else fsiTool.GetFsiPath(fsiOptions.UseAnyCpu.Value)
    let fsiPath = new Property<_>(lifetime, "FsiExePath", initialPath)
    let fsiTool = new Property<obj>(lifetime, "CurrentFsiTool", fsiTool)

    do
        if not autoDetectAllowed then fsiOptions.IsCustomTool.Value <- true

        autoDetect.Change.Advise_NoAcknowledgement(lifetime, fun (ArgValue autoDetect) ->
            fsiOptions.AutoDetect.Value <- autoDetect
            fsiTool.Value <-
                if autoDetect then tools[0] else

                let path = fsiOptions.FsiPathAsPath
                if path.IsEmpty && autoDetectAllowed then tools[0] else
                getActiveTool ())

        fsiTool.Change.Advise_NoAcknowledgement(lifetime, fun (ArgValue (FsiTool fsi)) ->
            fsiOptions.IsCustomTool.Value <- fsi.IsCustom

            fsiPath.Value <-
                if fsi.IsCustom then fsiOptions.FsiPathAsPath else
                fsi.GetFsiPath(fsiOptions.UseAnyCpu.Value))

        fsiPath.Change.Advise_NoAcknowledgement(lifetime, fun (ArgValue (path: VirtualFileSystemPath)) ->
            if not autoDetect.Value then
                fsiOptions.FsiPath.Value <- path.FullPath)

        this.AddHeader(launchOptionsSectionTitle)
        this.AddAutoDetect()

        this.AddToolChooser()
        this.AddUseAnyCpu()

        this.AddBool(shadowCopyReferencesText, fsiOptions.ShadowCopyReferences)
        this.AddDescription(shadowCopyReferencesDescription)

        this.AddString(fsiArgsText, fun key -> key.FsiArgs)

        this.AddHeader(FSharpScriptOptions.languageVersion)
        this.AddBool(specifyLanguageVersion, fsiOptions.SpecifyLanguageVersion)

        let languageVersion =
            this.AddComboEnum((fun (key: FsiOptions) -> key.LanguageVersion), FSharpScriptOptions.languageVersion, FSharpLanguageVersion.toString)
        fsiOptions.SpecifyLanguageVersion.FlowIntoRd(lifetime, languageVersion.Enabled)

        this.AddHeader(commandsSectionTitle)
        this.AddBool(moveCaretOnSendLineText, fsiOptions.MoveCaretOnSendLine)
        this.AddBool(moveCaretOnSendSelectionText, fsiOptions.MoveCaretOnSendSelection)

        this.AddBool(executeRecentText, fsiOptions.ExecuteRecent)
        this.AddDescription(executeRecentsDescription)

        if PlatformUtil.IsRunningUnderWindows then
            this.AddHeader(debugSectionTitle)
            this.AddBool(fixOptionsForDebugText, fsiOptions.FixOptionsForDebug)
            this.AddDescription(fixOptionsForDebugDescription)

    member x.AddAutoDetect() =
        let checkBox = this.AddBoolOption(autoDetect, RichText(autoDetectToolText), autoDetectToolText)
        checkBox.Enabled.Value <- autoDetectAllowed

    member x.AddToolChooser() =
        let options = tools |> Array.map (fun fsi -> RadioOptionPoint(fsi, fsi.Title))
        let toolComboGrid = x.AddComboOption(fsiTool, fsiToolText, "", "", options) :?> BeGrid

        for gridItem in toolComboGrid.Items.Value do
            autoDetect.FlowIntoRd(lifetime, gridItem.Content.Enabled, Not)

        let fileChooser =
            let path = fsiPath.Value.ToNativeFileSystemPath()
            x.AddFileChooserOption(fsiPath.SelectTwoWay(lifetime, (fun p -> p.ToNativeFileSystemPath()), (fun p -> p.ToVirtualFileSystemPath())),
                                   null, path, iconHost, dialogs, canBeEmpty = true, predefinedValues = [])
        fsiOptions.IsCustomTool.FlowIntoRd(lifetime, fileChooser.Enabled)

    member x.AddUseAnyCpu() =
        let initialValue = not fsiOptions.IsCustomTool.Value && fsiOptions.UseAnyCpu.Value
        let useAnyCpu = new Property<bool>(lifetime, useAnyCpuText, initialValue)
        let checkBox = this.AddBoolOption(useAnyCpu, RichText(useAnyCpuText), useAnyCpuText)
        fsiOptions.IsCustomTool.FlowIntoRd(lifetime, checkBox.Enabled, Not)

        fsiOptions.IsCustomTool.Change.Advise_NoAcknowledgement(lifetime, fun (ArgValue isCustomTool) ->
            checkBox.Enabled.Value <- not isCustomTool
            useAnyCpu.Value <-
                if isCustomTool then false else
                fsiOptions.UseAnyCpu.Value)

        useAnyCpu.Change.Advise_NoAcknowledgement(lifetime, fun (ArgValue useAnyCpu) ->
            if not fsiOptions.IsCustomTool.Value then
                fsiOptions.UseAnyCpu.Value <- useAnyCpu

                let (FsiTool fsi) = fsiTool.Value
                fsiPath.Value <- fsi.GetFsiPath(fsiOptions.UseAnyCpu.Value))


[<ShellComponent>]
type FSharpSettingsCategoryProvider() =
    let [<Literal>] categoryKey = "F# Interactive settings"

    interface IExportableSettingsCategoryProvider with
        member x.TryGetRelatedIdeaConfigsBy(_, configs) =
            configs <- EmptyArray.Instance
            false

        member x.TryGetCategoryBy(settingsKey, category) =
            if settingsKey.SettingsKeyClassClrType.Equals(typeof<FsiOptions>) then
                category <- categoryKey
            isNotNull category
