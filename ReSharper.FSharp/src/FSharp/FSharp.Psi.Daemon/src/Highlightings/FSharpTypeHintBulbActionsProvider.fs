module JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Highlightings.FSharpTypeHintsBulbActionsProvider

open System
open System.Linq.Expressions
open JetBrains.Application.I18n
open JetBrains.Application.Settings
open JetBrains.Application.UI.Controls.BulbMenu.Anchors
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Feature.Services.InlayHints
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Options
open JetBrains.ReSharper.Plugins.FSharp.Settings
open JetBrains.TextControl.DocumentMarkup.Adornments

type ActionStrings = JetBrains.ReSharper.Feature.Services.Resources.Strings
type Strings = JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Resources.Strings

[<AbstractClass>]
type FSharpTypeHintBulbActionsProvider(setting: Expression<Func<FSharpTypeHintOptions, PushToHintMode>>, settingTitle) =
    interface IInlayHintBulbActionsProvider with
        member x.CreateChangeVisibilityActions(settingsStore: ISettingsStore, _: IHighlighting, anchor: IAnchor) =
            IntraTextAdornmentDataModelHelper.CreateChangeVisibilityActions<FSharpTypeHintOptions>(
                settingsStore, setting, anchor, ActionStrings.ChangeVisibilityFor__Caption.Format(settingTitle: string)
            )

        member x.CreateChangeVisibilityBulbMenuItems(settingsStore: ISettingsStore, _: IHighlighting) =
            IntraTextAdornmentDataModelHelper.CreateChangeVisibilityBulbMenuItems<FSharpTypeHintOptions>(
                settingsStore, setting, BulbMenuAnchors.FirstClassContextItems,
                ActionStrings.ChangeVisibilityFor__Caption.Format(settingTitle: string)
            )

        member _.GetOptionsPageId() = nameof(FSharpTypeHintsOptionsPage)

type FSharpTopLevelMembersTypeHintBulbActionsProvider private () =
    inherit FSharpTypeHintBulbActionsProvider((fun x -> x.ShowTypeHintsForTopLevelMembers), Strings.FSharpTypeHints_TopLevelMembersSettings_Header)
    static member val Instance = FSharpTopLevelMembersTypeHintBulbActionsProvider()

type FSharpLocalBindingTypeHintBulbActionsProvider private () =
    inherit FSharpTypeHintBulbActionsProvider((fun x -> x.ShowTypeHintsForLocalBindings), Strings.FSharpTypeHints_LocalBindingsSettings_Header)
    static member val Instance = FSharpLocalBindingTypeHintBulbActionsProvider()

type FSharpOtherPatternsTypeHintBulbActionsProvider private () =
    inherit FSharpTypeHintBulbActionsProvider((fun x -> x.ShowTypeHintsForOtherPatterns), Strings.FSharpTypeHints_OtherPatternsSettings_Header)
    static member val Instance = FSharpOtherPatternsTypeHintBulbActionsProvider()
