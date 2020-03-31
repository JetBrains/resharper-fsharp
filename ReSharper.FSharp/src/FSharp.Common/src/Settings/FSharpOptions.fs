namespace JetBrains.ReSharper.Plugins.FSharp.Settings

open System.Reflection
open JetBrains.Application.Settings
open JetBrains.Application.UI.Options
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.DataContext
open JetBrains.ProjectModel.Resources
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.UI.RichText

[<SettingsKey(typeof<Missing>, "F# settings")>]
type FSharpSettings() = class end


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


module FSharpScriptOptions =
    let [<Literal>] languageVersion = "Language version"
    let [<Literal>] customDefines = "Custom defines"


[<SettingsKey(typeof<FSharpOptions>, "FSharpScriptOptions")>]
type FSharpScriptOptions =
    { [<SettingsEntry(FSharpLanguageVersion.Default, FSharpScriptOptions.languageVersion)>]
      mutable LanguageVersion: FSharpLanguageVersion

      [<SettingsEntry("", FSharpScriptOptions.customDefines)>]
      mutable CustomDefines: string }


[<SolutionInstanceComponent>]
type FSharpScriptOptionsProvider(lifetime: Lifetime, settings: IContextBoundSettingsStoreLive) =
    new (lifetime: Lifetime, solution: ISolution, settingsStore: ISettingsStore) =
        let settings = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()))
        FSharpScriptOptionsProvider(lifetime, settings)

    member val LanguageVersion = settings.GetValueProperty(lifetime, fun s -> s.LanguageVersion)
    member val CustomDefines = settings.GetValueProperty(lifetime, fun s -> s.CustomDefines)



module FSharpTypeHintOptions =
    let [<Literal>] pipeReturnTypes = "Show return type hints in |> chains"


[<SettingsKey(typeof<FSharpOptions>, "FSharpTypeHintOptions")>]
type FSharpTypeHintOptions =
    { [<SettingsEntry(true, FSharpTypeHintOptions.pipeReturnTypes); DefaultValue>]
      mutable ShowPipeReturnTypes: bool }


[<SolutionInstanceComponent>]
type FSharpTypeHintOptionsProvider(lifetime: Lifetime, settings: IContextBoundSettingsStoreLive) =
    new (lifetime: Lifetime, solution: ISolution, settingsStore: ISettingsStore) =
        let settings = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()))
        FSharpTypeHintOptionsProvider(lifetime, settings)

    member val ShowPipeReturnTypes = settings.GetValueProperty(lifetime, fun s -> s.ShowPipeReturnTypes)



[<OptionsPage("FSharpOptionsPage", "F#", typeof<ProjectModelThemedIcons.Fsharp>)>]
type FSharpOptionsPage
        (lifetime: Lifetime, optionsPageContext, settings) as this =
    inherit FSharpOptionsPageBase(lifetime, optionsPageContext, settings)

    do ignoreAll {
        this.AddHeader("Imports")
        this.AddBoolOption((fun key -> key.EnableOutOfScopeCompletion), RichText(outOfScopeCompletion), null)
        this.AddBoolOption((fun key -> key.TopLevelOpenCompletion), RichText(topLevelOpenCompletion), null)

        this.AddHeader("Script editing")
        this.AddComboEnum((fun key -> key.LanguageVersion), FSharpScriptOptions.languageVersion, FSharpLanguageVersion.toString)

        this.AddHeader("Type hints")
        this.AddBoolOption((fun key -> key.ShowPipeReturnTypes), RichText(FSharpTypeHintOptions.pipeReturnTypes), null)
        
        this.AddHeader("FSharp.Compiler.Service options")
        this.AddBoolOption((fun key -> key.BackgroundTypeCheck), RichText(backgroundTypeCheck), null)
    }
