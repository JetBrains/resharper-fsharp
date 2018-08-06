namespace JetBrains.ReSharper.Plugins.FSharp.Services.Settings.Fsi

open System
open System.Linq.Expressions
open JetBrains.Application.Settings
open JetBrains.DataFlow
open JetBrains.ProjectModel
open JetBrains.ProjectModel.DataContext
open JetBrains.ProjectModel.Settings.Schema
open JetBrains.Util

[<AutoOpen>]
module FsiOptions =
    let [<Literal>] fsiHelpKeyword            = "Settings_Languages_FSHARP_Interactive"

    let [<Literal>] launchOptionsSectionTitle = "Launch options"
    let [<Literal>] debugSectionTitle         = "Debug"
    let [<Literal>] commandsSectionTitle      = "Commands execution and history"

    let [<Literal>] useAnyCpuVersionText      = "Use 64-bit F# Interactive"
    let [<Literal>] shadowCopyReferencesText  = "Shadow copy assemblies"
    let [<Literal>] fsiArgsText               = "Launch arguments"
    let [<Literal>] moveCaretOnSendLineText   = "Move caret down on Send Line"
    let [<Literal>] executeRecentsText        = "Execute recent commands immediately"
    let [<Literal>] fsiPathText               = "F# Interactive executable path"
    let [<Literal>] fixOptionsForDebugText    = "Ensure correct launch options for debugging"

    let [<Literal>] shadowCopyReferencesDescription =
        "Copy referenced assemblies to a temporary directory to prevent locking by the F# Interactive process."

    let [<Literal>] fixOptionsForDebugDescription =
        "Fix launch options by adding additional `--optimize- --debug+` flags."

    let [<Literal>] executeRecentsDescription =
        "When disabled, copy recent command to F# Interactive editor."

    let getFsiName useFsiAnyCpu =
        if PlatformUtil.IsRunningUnderWindows then
            if useFsiAnyCpu then "fsiAnyCpu.exe" else "fsi.exe"
        else
            if useFsiAnyCpu then "fsharpiAnyCpu" else "fsharpi"


[<SettingsKey(typeof<HierarchySettings>, "Fsi")>]
type FsiOptions() =
    [<SettingsEntry(false, useAnyCpuVersionText); DefaultValue>]
    val mutable UseAnyCpuVersion: bool

    [<SettingsEntry(true, shadowCopyReferencesText); DefaultValue>]
    val mutable ShadowCopyReferences: bool

    [<SettingsEntry("--optimize+", fsiArgsText); DefaultValue>]
    val mutable FsiArgs: string

    [<SettingsEntry(true, moveCaretOnSendLineText); DefaultValue>]
    val mutable MoveCaretOnSendLine: bool

    [<SettingsEntry(true, executeRecentsText); DefaultValue>]
    val mutable ExecuteRecents: bool

    [<SettingsEntry(false, fixOptionsForDebugText); DefaultValue>]
    val mutable FixOptionsForDebug: bool

    [<SettingsEntry(null, fsiPathText); DefaultValue>]
    val mutable FsiPath: string

    static member GetValue(settings: IContextBoundSettingsStore, getter: Expression<Func<FsiOptions,_>>) =
        settings.GetValue(getter)

    static member SetValue(settings: IContextBoundSettingsStore, value, getter: Expression<Func<FsiOptions,_>>) =
        settings.SetValue(getter, value)

    static member GetProperty(lifetime, settings: IContextBoundSettingsStoreLive, getter: Expression<Func<FsiOptions,_>>) =
        settings.GetValueProperty(lifetime, getter)


[<SolutionInstanceComponent>]
type FsiOptionsProvider(lifetime: Lifetime, solution: ISolution, settingsStore: ISettingsStore) =
    let store = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()))

    member val UseAnyCpuVersion     = FsiOptions.GetProperty(lifetime, store, fun s -> s.UseAnyCpuVersion)
    member val ShadowCopyReferences = FsiOptions.GetProperty(lifetime, store, fun s -> s.ShadowCopyReferences)
    member val FsiArgs              = FsiOptions.GetProperty(lifetime, store, fun s -> s.FsiArgs)
    member val MoveCaretOnSendLine  = FsiOptions.GetProperty(lifetime, store, fun s -> s.MoveCaretOnSendLine)
    member val ExecuteRecents       = FsiOptions.GetProperty(lifetime, store, fun s -> s.ExecuteRecents)
    member val FixOptionsForDebug   = FsiOptions.GetProperty(lifetime, store, fun s -> s.FixOptionsForDebug)
    member val FsiPath              = FsiOptions.GetProperty(lifetime, store, fun s -> s.FsiPath)
