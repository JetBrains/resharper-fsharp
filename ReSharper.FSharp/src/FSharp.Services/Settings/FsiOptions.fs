namespace JetBrains.ReSharper.Plugins.FSharp.Services.Settings

open JetBrains.Application.Settings
open JetBrains.Application.UI.Icons.CommonThemedIcons
open JetBrains.Application.UI.Options
open JetBrains.Application.UI.Options.OptionsDialog
open JetBrains.DataFlow
open JetBrains.ProjectModel.Settings.Schema
open JetBrains.UI.RichText

[<SettingsKey(typeof<HierarchySettings>, "Fsi")>]
type FsiOptions() =
    [<SettingsEntry(true, "Use 64-bit F# Interactive"); DefaultValue>]
    val mutable UseAnyCpuVersion : bool

    [<SettingsEntry("--optimize", "F# Interactive arguments"); DefaultValue>]
    val mutable FsiArgs : string

[<OptionsPage("FsiOptionsPage", "Fsi", typeof<CommonThemedIcons.Abort>)>]
type FsiOptionsPage(lifetime, optionsContext) as this =
    inherit SimpleOptionsPage(lifetime, optionsContext)
    do
        this.AddBoolOption((fun (key : FsiOptions) -> key.UseAnyCpuVersion), RichText("Use 64-bit F# Interactive")) |> ignore
        this.AddStringOption((fun (key : FsiOptions) -> key.FsiArgs), "F# Interactive arguments") |> ignore
