namespace JetBrains.ReSharper.Plugins.FSharp.Common.Settings

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


[<SettingsKey(typeof<HierarchySettings>, "FSharpOptions")>]
type FSharpOptions() =
    [<SettingsEntry(false, backgroundTypeCheck); DefaultValue>]
    val mutable BackgroundTypeCheck: bool

[<OptionsPage("FSharpOptionsPage", "F#", typeof<ProjectModelThemedIcons.Fsharp>)>]
type FSharpOptionsPage
        (lifetime: Lifetime, optionsPageContext, settings) as this =
    inherit BeSimpleOptionsPage(lifetime, optionsPageContext, settings)

    do
        this.AddBoolOption((fun (key: FSharpOptions) -> key.BackgroundTypeCheck), RichText(backgroundTypeCheck), null) |> ignore