namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi

open System.Linq.Expressions
open System.Runtime.InteropServices
open JetBrains.Application
open JetBrains.Application.Settings
open JetBrains.Application.UI.Options
open JetBrains.DataFlow
open JetBrains.IDE.UI.Extensions
open JetBrains.IDE.UI.Options
open JetBrains.ProjectModel.Resources
open JetBrains.ReSharper.Host.Features.Settings.Layers.ExportImportWorkaround
open JetBrains.ReSharper.Plugins.FSharp.Services.Settings.Fsi
open JetBrains.UI.RichText
open JetBrains.Util
open System

[<OptionsPage("FsiOptionsPage", "Fsi", typeof<ProjectModelThemedIcons.Fsharp>, HelpKeyword = fsiHelpKeyword)>]
type FsiOptionsPage(lifetime: Lifetime, optionsContext) as this =
    inherit BeSimpleOptionsPage(lifetime, optionsContext)

    do
        this.AddHeader(launchOptionsSectionTitle)
        this.AddBool(useAnyCpuVersionText,     fun key -> key.UseAnyCpuVersion)
        this.AddBool(shadowCopyReferencesText, fun key -> key.ShadowCopyReferences)
        this.AddDescription(shadowCopyReferencesDescription)

        this.AddString(fsiArgsText, fun key -> key.FsiArgs)

        this.AddHeader(commandsSectionTitle)
        this.AddBool(moveCaretOnSendLineText, fun key -> key.MoveCaretOnSendLine)
        this.AddBool(executeRecentsText,      fun key -> key.ExecuteRecents)
        this.AddDescription(executeRecentsDescription)

        if PlatformUtil.IsRunningUnderWindows then
            this.AddHeader(debugSectionTitle)
            this.AddBool(fixOptionsForDebugText, fun key -> key.FixOptionsForDebug)
            this.AddDescription(fixOptionsForDebugDescription)
    
    member x.AddBool(text, getter: Expression<Func<FsiOptions,_>>) =
        this.AddBoolOption(getter, RichText(text)) |> ignore

    member x.AddHeader(text: string) =
        base.AddHeader(text) |> ignore

    member x.AddDescription(text) =
        use indent = x.Indent()
        this.AddRichText(RichText(text)) |> ignore

    member x.AddString(text: string, getter: Expression<Func<FsiOptions,_>>) =
        let property =
            let tyString = x.GetType().ToString()
            new Property<string>(lifetime, tyString + "_StringOptionViewModel_" + text + "_checkedProperty")

        let settingsEntry = x.OptionsSettingsSmartContext.Schema.GetScalarEntry(getter)
        x.OptionsSettingsSmartContext.SetBinding(lifetime, settingsEntry, property)
        x.AddKeyword(text)
        x.AddControl(property.GetBeTextBox(lifetime).WithDescription(text, lifetime))


[<ShellComponent>]
type FSharpSettingsCategoryProvider() =
    let categoryKey = "F# Interactive settings"
    let optionsType = typeof<FsiOptions>

    interface IExportableSettingsCategoryProvider with
        member x.TryGetRelatedIdeaConfigsBy(category, [<Out>] configs) =
            configs <- Array.empty
            false

        member x.TryGetCategoryBy(settingsKey, [<Out>] category) =
            if settingsKey.SettingsKeyClassClrType.Equals(optionsType) then
                category <- categoryKey
            not (isNull category)
