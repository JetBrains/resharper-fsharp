namespace JetBrains.ReSharper.Plugins.FSharp.Common.Checker.Settings

open JetBrains.Application.Settings
open JetBrains.Application.UI.Options
open JetBrains.IDE.UI.Options
open JetBrains.Lifetimes
open JetBrains.ProjectModel.Resources
open JetBrains.ProjectModel.Settings.Schema
open JetBrains.UI.RichText

[<AutoOpen>]
module FSharpOptions =
    let [<Literal>] backgroundTypeCheck = "Enable background type checking"  
    let [<Literal>] outOfScopeCompletion = "Enable out of scope items completion"  


[<SettingsKey(typeof<HierarchySettings>, "FSharpOptions")>]
type FSharpOptions() =
    [<SettingsEntry(false, backgroundTypeCheck); DefaultValue>]
    val mutable BackgroundTypeCheck: bool

    [<SettingsEntry(true, outOfScopeCompletion); DefaultValue>]
    val mutable EnableOutOfScopeCompletion: bool


[<OptionsPage("FSharpOptionsPage", "F#", typeof<ProjectModelThemedIcons.Fsharp>)>]
type FSharpOptionsPage
        (lifetime: Lifetime, optionsPageContext, settings) as this =
    inherit BeSimpleOptionsPage(lifetime, optionsPageContext, settings)

    do
        this.AddBoolOption((fun (key: FSharpOptions) -> key.BackgroundTypeCheck), RichText(backgroundTypeCheck), null) |> ignore
        this.AddBoolOption((fun (key: FSharpOptions) -> key.EnableOutOfScopeCompletion), RichText(outOfScopeCompletion), null) |> ignore
