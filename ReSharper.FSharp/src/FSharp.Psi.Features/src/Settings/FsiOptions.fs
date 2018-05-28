namespace JetBrains.ReSharper.Plugins.FSharp.Services.Settings.Fsi

open System
open System.Linq.Expressions
open System.Runtime.InteropServices
open JetBrains.Application
open JetBrains.Application.Settings
open JetBrains.Application.UI.Options
open JetBrains.Application.UI.Options.OptionsDialog
open JetBrains.DataFlow
open JetBrains.ProjectModel.Resources
open JetBrains.ProjectModel.Settings.Schema
open JetBrains.ReSharper.Host.Features.Settings.Layers.ExportImportWorkaround
open JetBrains.ReSharper.Plugins.FSharp.Common.Util
open JetBrains.UI.RichText

[<AutoOpen>]
module FsiOptions =
    let [<Literal>] launchOtionsSectionHeader    = "Launch options"
    let [<Literal>] commandsHistorySectionHeader = "Commands history"

    let [<Literal>] useAnyCpuVersionText         = "Use 64-bit F# Interactive"
    let [<Literal>] shadowCopyReferencesText     = "Shadow copy assemblies"
    let [<Literal>] fsiArgsText                  = "F# Interactive arguments"
    let [<Literal>] moveCaretOnSendLineText      = "Move caret down on Send Line"
    let [<Literal>] copyRecentToEditorText       = "Copy recent commands to Interactive editor"


[<SettingsKey(typeof<HierarchySettings>, "Fsi")>]
type FsiOptions() =
    [<SettingsEntry(false, useAnyCpuVersionText); DefaultValue>]
    val mutable UseAnyCpuVersion: bool

    [<SettingsEntry(true, shadowCopyReferencesText); DefaultValue>]
    val mutable ShadowCopyReferences: bool

    [<SettingsEntry("--optimize", fsiArgsText); DefaultValue>]
    val mutable FsiArgs: string

    [<SettingsEntry(true, moveCaretOnSendLineText); DefaultValue>]
    val mutable MoveCaretOnSendLine: bool

    [<SettingsEntry(true, copyRecentToEditorText); DefaultValue>]
    val mutable CopyRecentToEditor: bool

[<OptionsPage("FsiOptionsPage", "Fsi", typeof<ProjectModelThemedIcons.Fsharp>, HelpKeyword = "Settings_Languages_FSHARP_Interactive")>]
type FsiOptionsPage(lifetime, optionsContext) as this =
    inherit SimpleOptionsPage(lifetime, optionsContext)

    do
        this.AddHeader(launchOtionsSectionHeader)
        this.AddBool(useAnyCpuVersionText,     fun key -> key.UseAnyCpuVersion)
        this.AddBool(shadowCopyReferencesText, fun key -> key.ShadowCopyReferences)
        this.AddBool(moveCaretOnSendLineText,  fun key -> key.MoveCaretOnSendLine)
        this.AddString(fsiArgsText,            fun key -> key.FsiArgs)

        this.AddHeader(commandsHistorySectionHeader)
        this.AddBool(copyRecentToEditorText,   fun key -> key.CopyRecentToEditor)

        this.FinishPage()

    member x.AddBool(text, getter: Expression<Func<FsiOptions,_>>) =
        this.AddBoolOption(getter, RichText(text)) |> ignore

    member x.AddString(text, getter: Expression<Func<FsiOptions,_>>) =
        this.AddStringOption(getter, text) |> ignore

    member x.AddHeader(text: string) =
        this.AddHeader(text, null) |> ignore


[<ShellComponent>]
type FSharpSettingsCategoryProvider() =
    let categoryToKeys = Map.ofList ["F# Interactive settings", [ typeof<FsiOptions> ]]

    interface IExportableSettingsCategoryProvider with
        member x.TryGetRelatedIdeaConfigsBy(category, [<Out>] configs) =
            configs <- Array.empty
            false

        member x.TryGetCategoryBy(settingsKey, [<Out>] category) =
            category <-
                categoryToKeys
                |> Map.tryFindKey (fun _ types -> types |> List.exists settingsKey.SettingsKeyClassClrType.Equals)
                |> Option.toObj
            isNotNull category
