module JetBrains.ReSharper.Plugins.FSharp.Psi.Features.CodeCleanup.FSharpEditorConfig

open System
open JetBrains.Application
open JetBrains.Application.Parts
open JetBrains.Application.Settings
open JetBrains.Application.Settings.Calculated.Implementation
open JetBrains.DataFlow
open JetBrains.Diagnostics
open JetBrains.Lifetimes
open JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
open JetBrains.ReSharper.Psi.EditorConfig

let fSharpEditorConfigPrefix = "fsharp_"

type FantomasSettingsConverter(fantomasSettingsEntry: SettingsIndexedEntry) =
    let run (context: SettingsConvertContext<'a>) _ =
        let context = context.As<SettingsConvertContext<string>>().NotNull()
        for entry in context.SourceData.FindEntriesByPrefix(fSharpEditorConfigPrefix) do
            let setting = SettingIndex(fantomasSettingsEntry, entry.Key)
            context.Target.SetValue(setting, entry.Value)

        true

    interface IEditorConfigConverter with
        member val Category = EditorConfigConverter.ReSharperCategory with get, set
        member x.IsPropertySupported(property) = property.StartsWith(fSharpEditorConfigPrefix, StringComparison.Ordinal)
        member x.Convert(context) = run context null
        member x.ConvertAndCheck(context) = run context context
        member x.ReverseConvert _ = true


[<ShellComponent(Instantiation.DemandAnyThreadSafe)>]
type FantomasSettingsConverterProvider(lifetime: Lifetime, schema: SettingsSchema) =
    let items = CollectionEvents<IEditorConfigConverter>(lifetime, $"{nameof FantomasSettingsConverterProvider}.Items")

    do
        let fantomasSettingsEntry =
            schema.GetEntry(typeof<FSharpFormatSettingsKey>, "FantomasSettings") :?> SettingsIndexedEntry
        items.Add(FantomasSettingsConverter(fantomasSettingsEntry.NotNull())) |> ignore

    interface IProvider<IEditorConfigConverter> with
        member this.Items = items :> _
