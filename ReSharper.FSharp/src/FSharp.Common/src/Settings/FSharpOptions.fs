namespace JetBrains.ReSharper.Plugins.FSharp.Settings

open System
open System.Reflection
open JetBrains.Application
open JetBrains.Application.Settings
open JetBrains.Application.UI.Options
open JetBrains.Application.UI.Options.OptionsDialog.SimpleOptions
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.DataContext
open JetBrains.ProjectModel.Resources
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.UI.RichText

[<SettingsKey(typeof<Missing>, "F# settings")>]
type FSharpSettings() =
    class
    end


[<AutoOpen>]
module FSharpOptions =
    [<Literal>]
    let backgroundTypeCheck = "Enable background type checking"

    [<Literal>]
    let outOfScopeCompletion = "Enable out of scope items completion"

    [<Literal>]
    let topLevelOpenCompletion = "Add 'open' declarations to top level module or namespace"


[<SettingsKey(typeof<FSharpSettings>, "FSharpOptions")>]
type FSharpOptions =
    { [<SettingsEntry(false, backgroundTypeCheck); DefaultValue>]
      mutable BackgroundTypeCheck: bool

      [<SettingsEntry(true, outOfScopeCompletion); DefaultValue>]
      mutable EnableOutOfScopeCompletion: bool

      [<SettingsEntry(true, topLevelOpenCompletion); DefaultValue>]
      mutable TopLevelOpenCompletion: bool }


module FSharpScriptOptions =
    [<Literal>]
    let languageVersion = "Language version"

    [<Literal>]
    let customDefines = "Custom defines"


[<SettingsKey(typeof<FSharpOptions>, "FSharpScriptOptions")>]
type FSharpScriptOptions =
    { [<SettingsEntry(FSharpLanguageVersion.Default, FSharpScriptOptions.languageVersion)>]
      mutable LanguageVersion: FSharpLanguageVersion

      [<SettingsEntry("", FSharpScriptOptions.customDefines)>]
      mutable CustomDefines: string }


[<SolutionInstanceComponent>]
type FSharpScriptOptionsProvider(lifetime: Lifetime, settings: IContextBoundSettingsStoreLive) =

    new(lifetime: Lifetime, solution: ISolution, settingsStore: ISettingsStore) =
        let settings = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()))
        FSharpScriptOptionsProvider(lifetime, settings)

    member val LanguageVersion = settings.GetValueProperty(lifetime, (fun s -> s.LanguageVersion))
    member val CustomDefines = settings.GetValueProperty(lifetime, (fun s -> s.CustomDefines))


module FSharpTypeHintOptions =
    [<Literal>]
    let pipeReturnTypes = "Show return type hints in |> chains"

    [<Literal>]
    let hideSameLinePipe = "Hide when |> is on same line as argument"

    [<Literal>]
    let inferredTypes = "Show inferred types on identifiers"


[<SettingsKey(typeof<FSharpOptions>, "FSharpTypeHintOptions")>]
type FSharpTypeHintOptions =
    { [<SettingsEntry(true, FSharpTypeHintOptions.pipeReturnTypes); DefaultValue>]
      mutable ShowPipeReturnTypes: bool

      [<SettingsEntry(true, FSharpTypeHintOptions.hideSameLinePipe); DefaultValue>]
      mutable HideSameLine: bool

      [<SettingsEntry(true, FSharpTypeHintOptions.inferredTypes); DefaultValue>]
      mutable ShowInferredTypes: bool }


[<OptionsPage("FSharpOptionsPage", "F#", typeof<ProjectModelThemedIcons.Fsharp>)>]
type FSharpOptionsPage(lifetime: Lifetime, optionsPageContext, settings) as this =
    inherit FSharpOptionsPageBase(lifetime, optionsPageContext, settings)

    do
        this.AddHeader("Imports")
        this.AddBoolOption((fun key -> key.EnableOutOfScopeCompletion), RichText(outOfScopeCompletion), null) |> ignore
        this.AddBoolOption((fun key -> key.TopLevelOpenCompletion), RichText(topLevelOpenCompletion), null) |> ignore

        this.AddHeader("Script editing")
        this.AddComboEnum
            ((fun key -> key.LanguageVersion), FSharpScriptOptions.languageVersion, FSharpLanguageVersion.toString)
        |> ignore

        this.AddHeader("Type hints")
        let showPipeReturnTypes =
            this.AddBoolOption
                ((fun key -> key.ShowPipeReturnTypes), RichText(FSharpTypeHintOptions.pipeReturnTypes), null)
        do use _x = this.Indent()
           [ this.AddBoolOption((fun key -> key.HideSameLine), RichText(FSharpTypeHintOptions.hideSameLinePipe), null) ]
           |> Seq.iter
               (fun checkbox ->
                   this.AddBinding(checkbox, BindingStyle.IsEnabledProperty, (fun key -> key.ShowPipeReturnTypes), id))

        this.AddBoolOption((fun key -> key.ShowInferredTypes), RichText(FSharpTypeHintOptions.inferredTypes), null)
        |> ignore

        this.AddHeader("FSharp.Compiler.Service options")
        this.AddBoolOption((fun key -> key.BackgroundTypeCheck), RichText(backgroundTypeCheck), null) |> ignore


[<ShellComponent>]
type FSharpTypeHintOptionsStore(lifetime: Lifetime, settingsStore: ISettingsStore, highlightingSettingsManager: HighlightingSettingsManager) =
    do
        let settingsKey = settingsStore.Schema.GetKey<FSharpTypeHintOptions>()

        settingsStore.Changed.Advise
            (lifetime,
             (fun args ->
                 let typeHintOptionChanged =
                     args.ChangedEntries |> Seq.exists (fun changedEntry -> changedEntry.Parent = settingsKey)

                 if typeHintOptionChanged then highlightingSettingsManager.SettingsChanged.Fire(Nullable<_>(false))))
