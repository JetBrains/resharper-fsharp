namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi.Settings

open System
open System.Linq.Expressions
open JetBrains.Application.Settings
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.DataContext
open JetBrains.ReSharper.Plugins.FSharp
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

    let [<Literal>] moveCaretOnSendLineText   = "Move editor caret down on Send Line"
    let [<Literal>] executeRecentText        = "Execute recent commands immediately"
    let [<Literal>] fsiPathText               = "F# Interactive executable path"
    let [<Literal>] fixOptionsForDebugText    = "Ensure correct launch options for debugging"

    let [<Literal>] shadowCopyReferencesDescription =
        "Copy referenced assemblies to a temporary directory to prevent locking by the F# Interactive process."

    let [<Literal>] fixOptionsForDebugDescription =
        "Always add `--optimize- --debug+` flags to allow attaching debugger."

    let [<Literal>] executeRecentsDescription =
        "When disabled, copy recent command to F# Interactive editor."


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

      [<SettingsEntry(true, executeRecentText); DefaultValue>]
      mutable ExecuteRecent: bool

      [<SettingsEntry(false, fixOptionsForDebugText); DefaultValue>]
      mutable FixOptionsForDebug: bool

      [<SettingsEntry(null, fsiPathText); DefaultValue>]
      mutable FsiPath: string }

    static member GetValue(settings: IContextBoundSettingsStore, getter: Expression<Func<FsiOptions,_>>) =
        settings.GetValue(getter)

    static member SetValue(settings: IContextBoundSettingsStore, value, getter: Expression<Func<FsiOptions,_>>) =
        settings.SetValue(getter, value)

    static member GetProperty(lifetime, settings: IContextBoundSettingsStoreLive, getter: Expression<Func<FsiOptions,_>>) =
        settings.GetValueProperty(lifetime, getter)


[<SolutionInstanceComponent>]
type FsiOptionsProvider(lifetime: Lifetime, settings: IContextBoundSettingsStoreLive) =
    new(lifetime: Lifetime, solution: ISolution, settingsStore: ISettingsStore) =
        let settings = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()))
        FsiOptionsProvider(lifetime, settings)

    member val AutoDetect           = settings.GetValueProperty(lifetime, fun s -> s.AutoDetect)
    member val IsCustomTool         = settings.GetValueProperty(lifetime, fun s -> s.IsCustomTool)
    member val UseAnyCpu            = settings.GetValueProperty(lifetime, fun s -> s.UseAnyCpu)
    member val ShadowCopyReferences = settings.GetValueProperty(lifetime, fun s -> s.ShadowCopyReferences)
    member val FsiArgs              = settings.GetValueProperty(lifetime, fun s -> s.FsiArgs)
    member val FsiInternalArgs      = settings.GetValueProperty(lifetime, fun s -> s.FsiInternalArgs)
    member val MoveCaretOnSendLine  = settings.GetValueProperty(lifetime, fun s -> s.MoveCaretOnSendLine)
    member val ExecuteRecent       = settings.GetValueProperty(lifetime, fun s -> s.ExecuteRecent)
    member val FixOptionsForDebug   = settings.GetValueProperty(lifetime, fun s -> s.FixOptionsForDebug)
    member val FsiPath              = settings.GetValueProperty(lifetime, fun s -> s.FsiPath)

    member x.FsiPathAsPath =
        FileSystemPath.TryParse(x.FsiPath.Value)
