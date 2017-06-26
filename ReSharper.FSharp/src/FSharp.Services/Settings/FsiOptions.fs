namespace JetBrains.ReSharper.Plugins.FSharp.Services.Settings

open JetBrains.Application.Settings
open JetBrains.ProjectModel.Resources
open JetBrains.Application.UI.Options
open JetBrains.Application.UI.Options.OptionsDialog
open JetBrains.DataFlow
open JetBrains.ProjectModel.Settings.Schema
open JetBrains.UI.RichText

[<AutoOpen>]
module FsiOptions =
    let [<Literal>] useAnyCpuVersionText = "Use 64-bit F# Interactive"
    let [<Literal>] shadowCopyReferencesText = "Shadow copy assemblies"
    let [<Literal>] fsiArgsText = "F# Interactive arguments"
    let [<Literal>] moveCaretOnSendLineText = "Move caret down on Send Line"

    [<SettingsKey(typeof<HierarchySettings>, "Fsi")>]
    type FsiOptions() =
        [<SettingsEntry(false, useAnyCpuVersionText); DefaultValue>]
        val mutable UseAnyCpuVersion : bool

        [<SettingsEntry(true, shadowCopyReferencesText); DefaultValue>]
        val mutable ShadowCopyReferences : bool

        [<SettingsEntry("--optimize", fsiArgsText); DefaultValue>]
        val mutable FsiArgs : string

        [<SettingsEntry(true, fsiArgsText); DefaultValue>]
        val mutable MoveCaretOnSendLine : bool

    [<OptionsPage("FsiOptionsPage", "Fsi", typeof<ProjectModelThemedIcons.Fsharp>)>]
    type FsiOptionsPage(lifetime, optionsContext) as this =
        inherit SimpleOptionsPage(lifetime, optionsContext)
        do
            this.AddBoolOption((fun (key : FsiOptions) -> key.UseAnyCpuVersion), RichText(useAnyCpuVersionText)) |> ignore
            this.AddBoolOption((fun (key : FsiOptions) -> key.ShadowCopyReferences), RichText(shadowCopyReferencesText)) |> ignore
            this.AddBoolOption((fun (key : FsiOptions) -> key.MoveCaretOnSendLine), RichText(moveCaretOnSendLineText)) |> ignore
            this.AddStringOption((fun (key : FsiOptions) -> key.FsiArgs), fsiArgsText) |> ignore
            this.FinishPage()
