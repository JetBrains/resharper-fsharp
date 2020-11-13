namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi.Settings

open JetBrains.Application.Settings
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.DataContext
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.Util

[<AutoOpen>]
module FsiOptions =
    let [<Literal>] fsiHelpKeyword            = "Settings_Languages_FSHARP_Interactive"

    let [<Literal>] launchOptionsSectionTitle = "Launch options"
    let [<Literal>] debugSectionTitle         = "Debug"
    let [<Literal>] commandsSectionTitle      = "Commands execution and history"

    let [<Literal>] autoDetectToolText        = "Choose F# Interactive automatically"
    let [<Literal>] fsiToolText               = "F# Interactive tool"
    let [<Literal>] customToolText            = "Custom path"

    let [<Literal>] useAnyCpuText             = "Use 64-bit F# Interactive (AnyCpu)"
    let [<Literal>] shadowCopyReferencesText  = "Shadow copy assemblies"
    let [<Literal>] fsiArgsText               = "Launch arguments"
    let [<Literal>] fsiInternalArgsText       = "Internal launch arguments"

    let [<Literal>] specifyLanguageVersion    = "Target specific language version"
    let [<Literal>] useOptionsLanguageVersion = "Version from scripts settings"

    let [<Literal>] moveCaretOnSendLineText        = "Move editor caret down on Send Line"
    let [<Literal>] moveCaretOnSendSelectionText   = "Move editor caret right on Send Selection"
    let [<Literal>] executeRecentText              = "Execute recent commands immediately"
    let [<Literal>] fsiPathText                    = "F# Interactive executable path"
    let [<Literal>] fixOptionsForDebugText         = "Ensure correct launch options for debugging"

    let [<Literal>] shadowCopyReferencesDescription =
        "Copy referenced assemblies to a temporary directory to prevent locking by the F# Interactive process."

    let [<Literal>] fixOptionsForDebugDescription =
        "Always add `--optimize- --debug+` flags to allow attaching debugger."

    let [<Literal>] executeRecentsDescription =
        "When disabled, copy recent command to F# Interactive editor."

    let [<Literal>] specifyLanguageVersionDescription =
        "This option may be unavailable when using older F# Interactive."

[<SettingsKey(typeof<FSharpSettings>, "Fsi")>]
type FsiOptions =
    { [<SettingsEntry(true, autoDetectToolText); DefaultValue>]
      mutable AutoDetect: bool

      [<SettingsEntry(false, customToolText); DefaultValue>]
      mutable IsCustomTool: bool

      [<SettingsEntry(false, useAnyCpuText); DefaultValue>]
      mutable UseAnyCpu: bool

      [<SettingsEntry(false, shadowCopyReferencesText); DefaultValue>]
      mutable ShadowCopyReferences: bool

      [<SettingsEntry("--optimize+", fsiArgsText); DefaultValue>]
      mutable FsiArgs: string

      [<SettingsEntry("--fsi-server:0 --readline-", fsiInternalArgsText); DefaultValue>]
      mutable FsiInternalArgs: string

      [<SettingsEntry(true, moveCaretOnSendLineText); DefaultValue>]
      mutable MoveCaretOnSendLine: bool

      [<SettingsEntry(true, moveCaretOnSendSelectionText); DefaultValue>]
      mutable MoveCaretOnSendSelection: bool

      [<SettingsEntry(true, executeRecentText); DefaultValue>]
      mutable ExecuteRecent: bool

      [<SettingsEntry(false, fixOptionsForDebugText); DefaultValue>]
      mutable FixOptionsForDebug: bool

      [<SettingsEntry(null, fsiPathText); DefaultValue>]
      mutable FsiPath: string

      [<SettingsEntry(false, specifyLanguageVersion); DefaultValue>]
      mutable SpecifyLanguageVersion: bool

      [<SettingsEntry(FSharpLanguageVersion.Default, FSharpScriptOptions.languageVersion); DefaultValue>]
      mutable LanguageVersion: FSharpLanguageVersion

      [<SettingsEntry(true, useOptionsLanguageVersion); DefaultValue>]
      mutable UseLanguageVersionFromScriptOptions: bool }


[<SolutionInstanceComponent>]
type FsiOptionsProvider(lifetime, settings, settingsSchema) =
    inherit FSharpSettingsProviderBase<FsiOptions>(lifetime, settings, settingsSchema)

    new (lifetime: Lifetime, solution: ISolution, settingsStore: ISettingsStore, settingsSchema) =
        let settings = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()))
        FsiOptionsProvider(lifetime, settings, settingsSchema)

    member val AutoDetect               = base.GetValueProperty<bool>("AutoDetect")
    member val IsCustomTool             = base.GetValueProperty<bool>("IsCustomTool")
    member val UseAnyCpu                = base.GetValueProperty<bool>("UseAnyCpu")
    member val ShadowCopyReferences     = base.GetValueProperty<bool>("ShadowCopyReferences")
    member val FsiArgs                  = base.GetValueProperty<string>("FsiArgs")
    member val FsiInternalArgs          = base.GetValueProperty<string>("FsiInternalArgs")
    member val MoveCaretOnSendLine      = base.GetValueProperty<bool>("MoveCaretOnSendLine")
    member val MoveCaretOnSendSelection = base.GetValueProperty<bool>("MoveCaretOnSendSelection")
    member val ExecuteRecent            = base.GetValueProperty<bool>("ExecuteRecent")
    member val FixOptionsForDebug       = base.GetValueProperty<bool>("FixOptionsForDebug")
    member val FsiPath                  = base.GetValueProperty<string>("FsiPath")
    member val LanguageVersion          = base.GetValueProperty<FSharpLanguageVersion>("LanguageVersion")
    member val SpecifyLanguageVersion   = base.GetValueProperty<bool>("SpecifyLanguageVersion")

    member val UseLanguageVersionFromScriptOptions =
        base.GetValueProperty<bool>("UseLanguageVersionFromScriptOptions")

    member x.FsiPathAsPath =
        FileSystemPath.TryParse(x.FsiPath.Value)
