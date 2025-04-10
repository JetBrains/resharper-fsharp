module JetBrains.ReSharper.Plugins.FSharp.Util.SettingsStoreUtil

open JetBrains.Application.Settings

[<Struct; RequireQualifiedAccess>]
type SpecifiedSettingsStore<'T>(settingsStore: IContextBoundSettingsStore) =
    member x.GetValue<'TEntryValue>(expr) =
        settingsStore.GetValue<'T, 'TEntryValue>(expr)

type IContextBoundSettingsStore with
    member inline x.For<'T>() = SpecifiedSettingsStore<'T>(x)
