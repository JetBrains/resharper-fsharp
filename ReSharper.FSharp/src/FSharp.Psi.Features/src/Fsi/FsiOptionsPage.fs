namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi.Settings

open System
open System.Linq.Expressions
open System.Runtime.InteropServices
open JetBrains.Application
open JetBrains.Application.UI.Options
open JetBrains.Application.UI.Options.OptionsDialog.SimpleOptions.ViewModel
open JetBrains.DataFlow
open JetBrains.IDE.UI.Extensions
open JetBrains.IDE.UI.Options
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Resources
open JetBrains.ReSharper.Host.Features.Settings.Layers.ExportImportWorkaround
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi.FsiDetector
open JetBrains.Rider.Model.UIAutomation
open JetBrains.UI.RichText
open JetBrains.Util

[<OptionsPage("FsiOptionsPage", "Fsi", typeof<ProjectModelThemedIcons.Fsharp>, HelpKeyword = fsiHelpKeyword)>]
type FsiOptionsPage
        (lifetime: Lifetime, settings, fsiDetector: FsiDetector,
         [<Optional; DefaultParameterValue(null: ISolution)>] solution: ISolution) as this =
    inherit BeSimpleOptionsPage(lifetime, settings)

    let (|ArgValue|) (arg: PropertyChangedEventArgs<_>) = arg.New
    let (|FsiTool|) (obj: obj) = obj :?> FsiTool 

    let Not = Func<_,_>(not)

    let fsiOptions = FsiOptionsProvider(lifetime, settings)

    let tools = fsiDetector.GetFsiTools(solution)
    let autoDetectAllowed = tools.Length > 1

    let autoDetect =
        if autoDetectAllowed then fsiOptions.AutoDetect else
        new Property<bool>(lifetime, "FsiNoAutoDetect", false) :> _

    let findTool (path: FileSystemPath) =
        if fsiOptions.IsCustomTool.Value || not autoDetectAllowed then customTool else

        tools
        |> Array.tryFind (fun fsi -> fsi.Path = path.Directory)
        |> Option.defaultValue customTool

    let parsedPath = fsiOptions.FsiPathAsPath
    let fsiTool =
            if autoDetect.Value then tools.[0] else
            findTool parsedPath

    let initialPath = if fsiTool.IsCustom then parsedPath else fsiTool.GetFsiPath(fsiOptions.UseAnyCpu.Value)
    let fsiPath = new Property<_>(lifetime, "FsiExePath", initialPath)
    let fsiTool = new Property<obj>(lifetime, "CurrentFsiTool", fsiTool)

    do
        if not autoDetectAllowed then fsiOptions.IsCustomTool.Value <- true

        autoDetect.Change.Advise_NoAcknowledgement(lifetime, fun (ArgValue autoDetect) ->
            fsiOptions.AutoDetect.Value <- autoDetect
            fsiTool.Value <-
                if autoDetect then tools.[0] else

                let path = fsiOptions.FsiPathAsPath
                if path.IsEmpty && autoDetectAllowed then tools.[0] else
                findTool path)

        fsiTool.Change.Advise_NoAcknowledgement(lifetime, fun (ArgValue (FsiTool fsi)) ->
            fsiOptions.IsCustomTool.Value <- fsi.IsCustom

            fsiPath.Value <-
                if fsi.IsCustom then fsiOptions.FsiPathAsPath else
                fsi.GetFsiPath(fsiOptions.UseAnyCpu.Value))

        fsiPath.Change.Advise_NoAcknowledgement(lifetime, fun (ArgValue (path: FileSystemPath)) ->
            if not autoDetect.Value then
                fsiOptions.FsiPath.Value <- path.FullPath)

        this.AddHeader(launchOptionsSectionTitle)
        this.AddAutoDetect()

        this.AddToolChooser()
        this.AddUseAnyCpu()

        this.AddBool(shadowCopyReferencesText, fsiOptions.ShadowCopyReferences)
        this.AddDescription(shadowCopyReferencesDescription)

        this.AddString(fsiArgsText, fun key -> key.FsiArgs)

        this.AddHeader(commandsSectionTitle)
        this.AddBool(moveCaretOnSendLineText, fsiOptions.MoveCaretOnSendLine)

        this.AddBool(executeRecentsText, fsiOptions.ExecuteRecents)
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
        let toolComboGrid = x.AddComboOption(fsiTool, fsiToolText, null, null, options) :?> BeGrid

        for gridItem in toolComboGrid.Items.Value do
            autoDetect.FlowIntoRd(lifetime, gridItem.Content.Enabled, Not)

        let fileChooser = x.AddFileChooserOption(fsiPath, null, fsiPath.Value, canBeEmpty = true)
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

    member x.AddString(text: string, getter: Expression<Func<FsiOptions,_>>) =
        let prop = FsiOptions.GetProperty(lifetime, settings, getter)
        let grid = [| prop.GetBeTextBox(lifetime).WithDescription(text, lifetime) |].GetGrid()
        x.AddControl(grid)
        x.AddKeyword(text)

    member x.AddDescription(text) =
        use indent = x.Indent()
        this.AddRichText(RichText(text)) |> ignore

    member x.AddBool(text, property) =
        this.AddBoolOption(property, RichText(text), text) |> ignore

    member x.AddHeader(text: string) =
        base.AddHeader(text) |> ignore


[<ShellComponent>]
type FSharpSettingsCategoryProvider() =
    let [<Literal>] categoryKey = "F# Interactive settings"

    interface IExportableSettingsCategoryProvider with
        member x.TryGetRelatedIdeaConfigsBy(category, configs) =
            configs <- EmptyArray.Instance
            false

        member x.TryGetCategoryBy(settingsKey, category) =
            if settingsKey.SettingsKeyClassClrType.Equals(typeof<FsiOptions>) then
                category <- categoryKey
            isNotNull category
