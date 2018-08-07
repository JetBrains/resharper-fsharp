namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi

open System.Linq.Expressions
open System.Runtime.InteropServices
open JetBrains.Application
open JetBrains.Application.UI.Options
open JetBrains.Application.UI.Options.OptionsDialog
open JetBrains.ProjectModel.Resources
open JetBrains.ReSharper.Host.Features.Settings.Layers.ExportImportWorkaround
open JetBrains.ReSharper.Plugins.FSharp.Services.Settings.Fsi
open JetBrains.UI.RichText
open JetBrains.Util
open System

[<OptionsPage("FsiOptionsPage", "Fsi", typeof<ProjectModelThemedIcons.Fsharp>, HelpKeyword = fsiHelpKeyword)>]
type FsiOptionsPage(lifetime, optionsContext) as this =
    inherit SimpleOptionsPage(lifetime, optionsContext)

    do
        this.AddHeader(launchOptionsSectionTitle)
        this.AddBool(useAnyCpuVersionText,     fun key -> key.UseAnyCpuVersion)
        this.AddBool(shadowCopyReferencesText, fun key -> key.ShadowCopyReferences)
        this.AddDescription(shadowCopyReferencesDescription)
        this.AddEmptyLine() |> ignore

        this.AddString(fsiArgsText,            fun key -> key.FsiArgs)
        this.AddEmptyLine() |> ignore

        if PlatformUtil.IsRunningUnderWindows then
            this.AddHeader(debugSectionTitle)
            this.AddBool(fixOptionsForDebugText,   fun key -> key.FixOptionsForDebug)
            this.AddDescription(fixOptionsForDebugDescription)

        this.AddHeader(commandsSectionTitle)
        this.AddBool(moveCaretOnSendLineText,  fun key -> key.MoveCaretOnSendLine)
        this.AddBool(executeRecentsText,       fun key -> key.ExecuteRecents)
        this.AddDescription(executeRecentsDescription)

        this.FinishPage()

    member x.AddBool(text, getter: Expression<Func<FsiOptions,_>>) =
        this.AddBoolOption(getter, RichText(text)) |> ignore

    member x.AddString(text, getter: Expression<Func<FsiOptions,_>>) =
        this.AddStringOption(getter, text) |> ignore

    member x.AddHeader(text: string) =
        this.AddHeader(text, null) |> ignore

    member x.AddDescription(text) =
        let option = this.AddText(text)
        this.SetIndent(option, 1)


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
