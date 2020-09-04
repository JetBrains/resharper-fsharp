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
open JetBrains.Application.Environment
open JetBrains.Application.Environment.Helpers

[<SettingsKey(typeof<Missing>, "F# settings")>]
type FSharpSettings() = class end


[<AutoOpen>]
module FSharpOptions =
    let [<Literal>] backgroundTypeCheck = "Enable background type checking (not recommended)"
    let [<Literal>] outOfScopeCompletion = "Enable out of scope items completion"
    let [<Literal>] topLevelOpenCompletion = "Add 'open' declarations to top level module or namespace"
    let [<Literal>] enableInteractiveEditor = "Enable analysis of F# Interactive editor"
    let [<Literal>] enableFcsReactorMonitor = "Enable FCS monitor"


[<SettingsKey(typeof<FSharpSettings>, "FSharpOptions")>]
type FSharpOptions =
    { [<SettingsEntry(false, backgroundTypeCheck); DefaultValue>]
      mutable BackgroundTypeCheck: bool

      [<SettingsEntry(true, outOfScopeCompletion); DefaultValue>]
      mutable EnableOutOfScopeCompletion: bool

      [<SettingsEntry(true, topLevelOpenCompletion); DefaultValue>]
      mutable TopLevelOpenCompletion: bool
      
      [<SettingsEntry(false, enableInteractiveEditor); DefaultValue>]
      mutable EnableInteractiveEditor: bool

      [<SettingsEntry(false, enableFcsReactorMonitor); DefaultValue>]
      mutable EnableReactorMonitor: bool }


module FSharpScriptOptions =
    let [<Literal>] languageVersion = "Language version"
    let [<Literal>] customDefines = "Custom defines"


[<SettingsKey(typeof<FSharpOptions>, "FSharpScriptOptions")>]
type FSharpScriptOptions =
    { [<SettingsEntry(FSharpLanguageVersion.Default, FSharpScriptOptions.languageVersion)>]
      mutable LanguageVersion: FSharpLanguageVersion

      [<SettingsEntry("", FSharpScriptOptions.customDefines)>]
      mutable CustomDefines: string }


module FSharpExperimentalFeaturesOptions =
    let [<Literal>] enableInlineVarRefactoring = "Enable inline var refactoring"
    let [<Literal>] enablePostfixTemplates = "Enable postfix templates"
    let [<Literal>] enableRedundantParenAnalysis = "Enable redundant paren analysis"
    let [<Literal>] enableFormatter = "Enable F# code formatter"


[<SettingsKey(typeof<FSharpOptions>, "FSharpExperimentalFeaturesOptions")>]
type FSharpExperimentalFeaturesOptions =
    { [<SettingsEntry(false, FSharpExperimentalFeaturesOptions.enableInlineVarRefactoring)>]
      mutable EnableInlineVarRefactoring: bool

      [<SettingsEntry(false, FSharpExperimentalFeaturesOptions.enablePostfixTemplates)>]
      mutable EnablePostfixTemplates: bool

      [<SettingsEntry(false, FSharpExperimentalFeaturesOptions.enableRedundantParenAnalysis)>]
      mutable EnableRedundantParenAnalysis: bool

      [<SettingsEntry(false, FSharpExperimentalFeaturesOptions.enableFormatter)>]
      mutable EnableFormatter: bool }


[<SolutionInstanceComponent>]
type FSharpScriptSettingsProvider(lifetime: Lifetime, settings: IContextBoundSettingsStoreLive) =
    new (lifetime: Lifetime, solution: ISolution, settingsStore: ISettingsStore) =
        let settings = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()))
        FSharpScriptSettingsProvider(lifetime, settings)

    member val LanguageVersion = settings.GetValueProperty(lifetime, fun s -> s.LanguageVersion)
    member val CustomDefines = settings.GetValueProperty(lifetime, fun s -> s.CustomDefines)


[<SolutionInstanceComponent>]
type FSharpExperimentalFeaturesProvider(lifetime: Lifetime, settings: IContextBoundSettingsStoreLive) =
    new (lifetime: Lifetime, solution: ISolution, settingsStore: ISettingsStore) =
        let settings = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()))
        FSharpExperimentalFeaturesProvider(lifetime, settings)

    member val EnableInlineVarRefactoring = settings.GetValueProperty(lifetime, fun s -> s.EnableInlineVarRefactoring)
    member val EnablePostfixTemplates = settings.GetValueProperty(lifetime, fun s -> s.EnablePostfixTemplates)
    member val EnableRedundantParenAnalysis = settings.GetValueProperty(lifetime, fun s -> s.EnableRedundantParenAnalysis)
    member val EnableFormatter = settings.GetValueProperty(lifetime, fun s -> s.EnableFormatter)


module FSharpTypeHintOptions =
    let [<Literal>] pipeReturnTypes = "Show return type hints in |> chains"

    let [<Literal>] hideSameLinePipe = "Hide when |> is on same line as argument"


[<SettingsKey(typeof<FSharpOptions>, "FSharpTypeHintOptions")>]
type FSharpTypeHintOptions =
    { [<SettingsEntry(true, FSharpTypeHintOptions.pipeReturnTypes); DefaultValue>]
      mutable ShowPipeReturnTypes: bool

      [<SettingsEntry(true, FSharpTypeHintOptions.hideSameLinePipe); DefaultValue>]
      mutable HideSameLine: bool }


[<OptionsPage("FSharpOptionsPage", "F#", typeof<ProjectModelThemedIcons.Fsharp>)>]
type FSharpOptionsPage
        (lifetime: Lifetime, optionsPageContext, settings, configurations: RunsProducts.ProductConfigurations) as this =
    inherit FSharpOptionsPageBase(lifetime, optionsPageContext, settings)

    do
        this.AddHeader("Imports")
        this.AddBoolOption((fun key -> key.EnableOutOfScopeCompletion), RichText(outOfScopeCompletion), null) |> ignore
        this.AddBoolOption((fun key -> key.TopLevelOpenCompletion), RichText(topLevelOpenCompletion), null) |> ignore

        this.AddHeader("Script editing")
        this.AddComboEnum((fun key -> key.LanguageVersion), FSharpScriptOptions.languageVersion, FSharpLanguageVersion.toString) |> ignore
        this.AddBoolOption((fun key -> key.EnableInteractiveEditor), RichText(enableInteractiveEditor)) |> ignore

        this.AddHeader("Type hints")
        let showPipeReturnTypes = this.AddBoolOption((fun key -> key.ShowPipeReturnTypes), RichText(FSharpTypeHintOptions.pipeReturnTypes), null)
        do
            use _x = this.Indent()
            [
                this.AddBoolOption((fun key -> key.HideSameLine), RichText(FSharpTypeHintOptions.hideSameLinePipe), null)
            ]
            |> Seq.iter (fun checkbox ->
                this.AddBinding(checkbox, BindingStyle.IsEnabledProperty, (fun key -> key.ShowPipeReturnTypes), id)
            )
        
        this.AddHeader("FSharp.Compiler.Service options")
        this.AddBoolOption((fun key -> key.EnableReactorMonitor), RichText(enableFcsReactorMonitor), null) |> ignore
        this.AddBoolOption((fun key -> key.BackgroundTypeCheck), RichText(backgroundTypeCheck), null) |> ignore

        if configurations.IsInternalMode() then
            this.AddHeader("Experimental features options")
            this.AddBoolOption((fun key -> key.EnableInlineVarRefactoring), RichText(FSharpExperimentalFeaturesOptions.enableInlineVarRefactoring), null) |> ignore
            this.AddBoolOption((fun key -> key.EnablePostfixTemplates), RichText(FSharpExperimentalFeaturesOptions.enablePostfixTemplates), null) |> ignore
            this.AddBoolOption((fun key -> key.EnableRedundantParenAnalysis), RichText(FSharpExperimentalFeaturesOptions.enableRedundantParenAnalysis), null) |> ignore
            this.AddBoolOption((fun key -> key.EnableFormatter), RichText(FSharpExperimentalFeaturesOptions.enableFormatter), null) |> ignore


[<ShellComponent>]
type FSharpTypeHintOptionsStore(lifetime: Lifetime, settingsStore: ISettingsStore, highlightingSettingsManager: HighlightingSettingsManager) =
    do
        let settingsKey = settingsStore.Schema.GetKey<FSharpTypeHintOptions>()

        settingsStore.Changed.Advise(lifetime, fun args ->
            let typeHintOptionChanged =
                args.ChangedEntries
                |> Seq.exists (fun changedEntry -> changedEntry.Parent = settingsKey)

            if typeHintOptionChanged then
                highlightingSettingsManager.SettingsChanged.Fire(Nullable<_>(false))
        )
