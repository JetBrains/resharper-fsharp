namespace JetBrains.ReSharper.Plugins.FSharp.Checker.Settings

open JetBrains.Application.Settings
open JetBrains.Application.UI.Options
open JetBrains.IDE.UI.Options
open JetBrains.Lifetimes
open JetBrains.ProjectModel.Resources
open JetBrains.ReSharper.Plugins.FSharp
open JetBrains.UI.RichText

[<AutoOpen>]
module FSharpOptions =
    let [<Literal>] backgroundTypeCheck = "Enable background type checking"  
    let [<Literal>] outOfScopeCompletion = "Enable out of scope items completion"
    let [<Literal>] topLevelOpenCompletion = "Add 'open' declarations to top level module or namespace"


[<SettingsKey(typeof<FSharpSettings>, "FSharpOptions")>]
type FSharpOptions =
    { [<SettingsEntry(false, backgroundTypeCheck); DefaultValue>]
      mutable BackgroundTypeCheck: bool

      [<SettingsEntry(true, outOfScopeCompletion); DefaultValue>]
      mutable EnableOutOfScopeCompletion: bool

      [<SettingsEntry(true, topLevelOpenCompletion); DefaultValue>]
      mutable TopLevelOpenCompletion: bool }


[<OptionsPage("FSharpOptionsPage", "F#", typeof<ProjectModelThemedIcons.Fsharp>)>]
type FSharpOptionsPage
        (lifetime: Lifetime, optionsPageContext, settings) as this =
    inherit BeSimpleOptionsPage(lifetime, optionsPageContext, settings)

    do
        this.AddBoolOption((fun key -> key.BackgroundTypeCheck), RichText(backgroundTypeCheck), null) |> ignore
        this.AddBoolOption((fun key -> key.EnableOutOfScopeCompletion), RichText(outOfScopeCompletion), null) |> ignore
        this.AddBoolOption((fun key -> key.TopLevelOpenCompletion), RichText(topLevelOpenCompletion), null) |> ignore
