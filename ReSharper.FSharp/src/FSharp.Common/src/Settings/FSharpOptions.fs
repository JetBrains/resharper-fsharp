namespace JetBrains.ReSharper.Plugins.FSharp.Settings

open System
open System.Reflection
open JetBrains.Application
open JetBrains.Application.Settings
open JetBrains.Application.UI.Options
open JetBrains.Application.UI.Options.OptionsDialog.SimpleOptions
open JetBrains.DataFlow
open JetBrains.Lifetimes
open JetBrains.ProjectModel
open JetBrains.ProjectModel.DataContext
open JetBrains.ProjectModel.Resources
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel
open JetBrains.UI.RichText
open JetBrains.Application.Environment
open JetBrains.Application.Environment.Helpers
open JetBrains.Util

[<SettingsKey(typeof<Missing>, "F# settings")>]
type FSharpSettings() = class end


[<AutoOpen>]
module FSharpOptions =
    let [<Literal>] backgroundTypeCheck = "Enable background analysis (requires restart)"
    let [<Literal>] skipImplementationAnalysis = "Skip implementation files analysis when possible (requires restart)"
    let [<Literal>] nonFSharpProjectInMemoryAnalysis = "Analyze C#/VB projects without build (requires restart)"
    let [<Literal>] outOfScopeCompletion = "Enable out of scope items completion"
    let [<Literal>] topLevelOpenCompletion = "Add 'open' declarations to top level module or namespace"
    let [<Literal>] enableFcsReactorMonitor = "Enable FCS monitor"


[<SettingsKey(typeof<FSharpSettings>, "FSharpOptions")>]
type FSharpOptions =
    { [<SettingsEntry(false, skipImplementationAnalysis); DefaultValue>]
      mutable SkipImplementationAnalysis: bool

      [<SettingsEntry(false, nonFSharpProjectInMemoryAnalysis); DefaultValue>]
      mutable NonFSharpProjectInMemoryAnalysis: bool

      [<SettingsEntry(true, outOfScopeCompletion); DefaultValue>]
      mutable EnableOutOfScopeCompletion: bool

      [<SettingsEntry(true, topLevelOpenCompletion); DefaultValue>]
      mutable TopLevelOpenCompletion: bool }


module FSharpScriptOptions =
    let [<Literal>] languageVersion = "Language version"
    let [<Literal>] targetNetFramework = "Target .NET Framework"
    let [<Literal>] customDefines = "Custom defines"


[<SettingsKey(typeof<FSharpOptions>, "FSharpScriptOptions")>]
type FSharpScriptOptions =
    { [<SettingsEntry(FSharpLanguageVersion.Default, FSharpScriptOptions.languageVersion)>]
      mutable LanguageVersion: FSharpLanguageVersion

      [<SettingsEntry(false, FSharpScriptOptions.languageVersion)>]
      mutable TargetNetFramework: bool

      [<SettingsEntry("", FSharpScriptOptions.customDefines)>]
      mutable CustomDefines: string }


module FSharpExperimentalFeatures =
    let [<Literal>] postfixTemplates = "Enable postfix templates"
    let [<Literal>] redundantParenAnalysis = "Enable redundant paren analysis"
    let [<Literal>] formatter = "Enable F# code formatter"
    let [<Literal>] fsiInteractiveEditor = "Enable analysis of F# Interactive editor (experimental)"
    let [<Literal>] outOfProcessTypeProviders = "Host type providers out-of-process (solution reload required)"
    let [<Literal>] hostTypeProvidersFromTempFolder = "Host type providers from Temp folder"


[<SettingsKey(typeof<FSharpOptions>, "F# experimental features")>]
type FSharpExperimentalFeatures =
    { [<SettingsEntry(false, FSharpExperimentalFeatures.postfixTemplates)>]
      mutable PostfixTemplates: bool

      [<SettingsEntry(false, FSharpExperimentalFeatures.redundantParenAnalysis)>]
      mutable RedundantParensAnalysis: bool

      [<SettingsEntry(false, FSharpExperimentalFeatures.formatter)>]
      mutable Formatter: bool

      [<SettingsEntry(false, FSharpExperimentalFeatures.fsiInteractiveEditor); DefaultValue>]
      mutable FsiInteractiveEditor: bool

      [<SettingsEntry(true, FSharpExperimentalFeatures.outOfProcessTypeProviders)>]
      mutable OutOfProcessTypeProviders: bool

      [<SettingsEntry(false, FSharpExperimentalFeatures.hostTypeProvidersFromTempFolder)>]
      mutable HostTypeProvidersFromTempFolder: bool }


[<AllowNullLiteral>]
type FSharpSettingsProviderBase<'T>(lifetime: Lifetime, settings: IContextBoundSettingsStoreLive,
        settingsSchema: SettingsSchema) =

    let settingsKey = settingsSchema.GetKey<'T>()

    new (lifetime: Lifetime, solution: ISolution, settingsStore: ISettingsStore, settingsSchema: SettingsSchema) =
        let settings = settingsStore.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()))
        FSharpSettingsProviderBase(lifetime, settings, settingsSchema)

    member x.GetValueProperty<'V>(name: string) =
        let entry = settingsKey.TryFindEntryByMemberName(name) :?> SettingsScalarEntry
        settings.GetValueProperty(lifetime, entry, null) :> IProperty<'V>


[<SolutionInstanceComponent>]
type FSharpScriptSettingsProvider(lifetime, solution, settings, settingsSchema) =
    inherit FSharpSettingsProviderBase<FSharpScriptOptions>(lifetime, solution, settings, settingsSchema)

    member val LanguageVersion = base.GetValueProperty<FSharpLanguageVersion>("LanguageVersion")
    member val TargetNetFramework = base.GetValueProperty<bool>("TargetNetFramework")
    member val CustomDefines = base.GetValueProperty<string>("CustomDefines")


[<SolutionInstanceComponent>]
type FSharpExperimentalFeaturesProvider(lifetime, solution, settings, settingsSchema) =
    inherit FSharpSettingsProviderBase<FSharpExperimentalFeatures>(lifetime, solution, settings, settingsSchema)

    member val EnablePostfixTemplates = base.GetValueProperty<bool>("PostfixTemplates")
    member val RedundantParensAnalysis = base.GetValueProperty<bool>("RedundantParensAnalysis")
    member val Formatter = base.GetValueProperty<bool>("Formatter")
    member val OutOfProcessTypeProviders = base.GetValueProperty<bool>("OutOfProcessTypeProviders")
    member val HostTypeProvidersFromTempFolder = base.GetValueProperty<bool>("HostTypeProvidersFromTempFolder")


[<SolutionInstanceComponent>]
type FSharpOptionsProvider(lifetime, solution, settings, settingsSchema) =
    inherit FSharpSettingsProviderBase<FSharpOptions>(lifetime, solution, settings, settingsSchema)

    member val NonFSharpProjectInMemoryAnalysis = base.GetValueProperty<bool>("NonFSharpProjectInMemoryAnalysis")


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
type FSharpOptionsPage(lifetime: Lifetime, optionsPageContext, settings,
        configurations: RunsProducts.ProductConfigurations) as this =
    inherit FSharpOptionsPageBase(lifetime, optionsPageContext, settings)

    do
        this.AddHeader("Imports")
        this.AddBoolOption((fun key -> key.EnableOutOfScopeCompletion), RichText(outOfScopeCompletion), null) |> ignore
        this.AddBoolOption((fun key -> key.TopLevelOpenCompletion), RichText(topLevelOpenCompletion), null) |> ignore

        this.AddHeader("Script editing")
        this.AddComboEnum((fun key -> key.LanguageVersion), FSharpScriptOptions.languageVersion, FSharpLanguageVersion.toString) |> ignore
        if PlatformUtil.IsRunningUnderWindows then
            this.AddBoolOption((fun key -> key.TargetNetFramework), RichText(FSharpScriptOptions.targetNetFramework)) |> ignore
        this.AddBoolOption((fun key -> key.FsiInteractiveEditor), RichText(FSharpExperimentalFeatures.fsiInteractiveEditor)) |> ignore

        this.AddHeader("Type hints")
        this.AddBoolOption((fun key -> key.ShowPipeReturnTypes), RichText(FSharpTypeHintOptions.pipeReturnTypes), null) |> ignore

        do
            use indent = this.Indent()
            [ this.AddBoolOption((fun key -> key.HideSameLine), RichText(FSharpTypeHintOptions.hideSameLinePipe), null) ]
            |> Seq.iter (fun checkbox ->
                this.AddBinding(checkbox, BindingStyle.IsEnabledProperty, (fun key -> key.ShowPipeReturnTypes), id))

        this.AddHeader("FSharp.Compiler.Service options")
        this.AddBoolOption((fun key -> key.SkipImplementationAnalysis), RichText(skipImplementationAnalysis), null) |> ignore
        this.AddBoolOption((fun key -> key.OutOfProcessTypeProviders), RichText(FSharpExperimentalFeatures.outOfProcessTypeProviders), null) |> ignore
        do
            use indent = this.Indent()
            [ this.AddBoolOption((fun key -> key.HostTypeProvidersFromTempFolder), RichText(FSharpExperimentalFeatures.hostTypeProvidersFromTempFolder), null) ]
            |> Seq.iter (fun checkbox ->
                this.AddBinding(checkbox, BindingStyle.IsEnabledProperty, (fun key -> key.OutOfProcessTypeProviders), id))

        if configurations.IsInternalMode() then
            this.AddHeader("Experimental features options")
            this.AddBoolOption((fun key -> key.PostfixTemplates), RichText(FSharpExperimentalFeatures.postfixTemplates), null) |> ignore
            this.AddBoolOption((fun key -> key.RedundantParensAnalysis), RichText(FSharpExperimentalFeatures.redundantParenAnalysis), null) |> ignore
            this.AddBoolOption((fun key -> key.Formatter), RichText(FSharpExperimentalFeatures.formatter), null) |> ignore
            this.AddBool(nonFSharpProjectInMemoryAnalysis, fun key -> key.NonFSharpProjectInMemoryAnalysis)


[<ShellComponent>]
type FSharpTypeHintOptionsStore(lifetime: Lifetime, settingsStore: ISettingsStore,
        highlightingSettingsManager: HighlightingSettingsManager) =
    do
        let settingsKey = settingsStore.Schema.GetKey<FSharpTypeHintOptions>()

        settingsStore.Changed.Advise(lifetime, fun (args: SettingsStoreChangeArgs) ->
            let typeHintOptionChanged =
                args.ChangedEntries
                |> Seq.exists (fun changedEntry -> changedEntry.Parent = settingsKey)

            if typeHintOptionChanged then
                highlightingSettingsManager.SettingsChanged.Fire(Nullable(false)))


module SettingsUtil =
    let getEntry<'TKey> (settingsStore: ISettingsStore) name =
        settingsStore.Schema.GetKey<'TKey>().TryFindEntryByMemberName(name) :?> SettingsScalarEntry

    let getValue<'TKey, 'TValue> (settingsStore: ISettingsStore) name =
        let entry = getEntry<'TKey> settingsStore name
        let settingsStoreLive = settingsStore.BindToContextTransient(ContextRange.ApplicationWide)
        settingsStoreLive.GetValue(entry, null) :?> 'TValue
