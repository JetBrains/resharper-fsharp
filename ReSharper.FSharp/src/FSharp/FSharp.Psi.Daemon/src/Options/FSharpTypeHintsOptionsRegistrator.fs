namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Options

open JetBrains.Application.Parts
open JetBrains.Application.Settings
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.InlayHints
open JetBrains.ReSharper.Plugins.FSharp.Settings

[<SolutionComponent(InstantiationEx.LegacyDefault)>]
type FSharpTypeHintsOptionsRegistrator(inlayHintsOptionsStore: InlayHintsOptionsStore, settingsStore: ISettingsStore) =
    do
        let settingsKey = settingsStore.Schema.GetKey<FSharpTypeHintOptions>()
        inlayHintsOptionsStore.RegisterSettingsKeyToRehighlightVisibleDocumentOnItsChange(settingsKey)
