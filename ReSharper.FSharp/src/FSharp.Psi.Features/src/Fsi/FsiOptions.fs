namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Fsi.Settings

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

    let [<Literal>] autoDetectToolText        = "Choose F# Interactive automatically"
    let [<Literal>] fsiToolText               = "F# Interactive tool"
    let [<Literal>] customToolText            = "Custom path"

    let [<Literal>] useAnyCpuText             = "Use 64-bit F# Interactive (AnyCpu)"
    let [<Literal>] shadowCopyReferencesText  = "Shadow copy assemblies"
    let [<Literal>] fsiArgsText               = "Launch arguments"
    let [<Literal>] fsiInternalArgsText       = "Internal launch arguments"

    let [<Literal>] moveCaretOnSendLineText   = "Move editor caret down on Send Line"
    let [<Literal>] executeRecentsText        = "Execute recent commands immediately"
    let [<Literal>] fsiPathText               = "F# Interactive executable path"
    let [<Literal>] fixOptionsForDebugText    = "Ensure correct launch options for debugging"

    let [<Literal>] shadowCopyReferencesDescription =
        "Copy referenced assemblies to a temporary directory to prevent locking by the F# Interactive process."

    let [<Literal>] fixOptionsForDebugDescription =
        "Always add `--optimize- --debug+` flags to allow attaching debugger."

    let [<Literal>] executeRecentsDescription =
        "When disabled, copy recent command to F# Interactive editor."


[<SettingsKey(typeof<HierarchySettings>, "Fsi")>]
type FsiOptions() =
    [<SettingsEntry(true, autoDetectToolText); DefaultValue>]
    val mutable AutoDetect: bool

    [<SettingsEntry(false, customToolText); DefaultValue>]
    val mutable IsCustomTool: bool
    
    [<SettingsEntry(false, useAnyCpuText); DefaultValue>]
    val mutable UseAnyCpu: bool

    [<SettingsEntry(false, shadowCopyReferencesText); DefaultValue>]
    val mutable ShadowCopyReferences: bool

    [<SettingsEntry("--optimize+", fsiArgsText); DefaultValue>]
    val mutable FsiArgs: string

    [<SettingsEntry("--fsi-server:rider --readline-", fsiInternalArgsText); DefaultValue>]
    val mutable FsiInternalArgs: string

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
type FsiOptionsProvider(lifetime: Lifetime, settings: IContextBoundSettingsStoreLive) =
    new(lifetime: Lifetime, solution: ISolution, settingsStore: ISettingsStore) =
        let settings = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()))
        FsiOptionsProvider(lifetime, settings)

    member val AutoDetect           = FsiOptions.GetProperty(lifetime, settings, fun s -> s.AutoDetect)
    member val IsCustomTool         = FsiOptions.GetProperty(lifetime, settings, fun s -> s.IsCustomTool)
    member val UseAnyCpu            = FsiOptions.GetProperty(lifetime, settings, fun s -> s.UseAnyCpu)
    member val ShadowCopyReferences = FsiOptions.GetProperty(lifetime, settings, fun s -> s.ShadowCopyReferences)
    member val FsiArgs              = FsiOptions.GetProperty(lifetime, settings, fun s -> s.FsiArgs)
    member val FsiInternalArgs      = FsiOptions.GetProperty(lifetime, settings, fun s -> s.FsiInternalArgs)
    member val MoveCaretOnSendLine  = FsiOptions.GetProperty(lifetime, settings, fun s -> s.MoveCaretOnSendLine)
    member val ExecuteRecents       = FsiOptions.GetProperty(lifetime, settings, fun s -> s.ExecuteRecents)
    member val FixOptionsForDebug   = FsiOptions.GetProperty(lifetime, settings, fun s -> s.FixOptionsForDebug)
    member val FsiPath              = FsiOptions.GetProperty(lifetime, settings, fun s -> s.FsiPath)

    member x.FsiPathAsPath =
        FileSystemPath.TryParse(x.FsiPath.Value)
