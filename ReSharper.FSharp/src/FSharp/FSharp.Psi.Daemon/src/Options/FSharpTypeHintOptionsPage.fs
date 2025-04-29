namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Options

open System.Runtime.InteropServices
open JetBrains.Application.UI.Options
open JetBrains.Application.UI.Options.OptionsDialog
open JetBrains.Application.UI.Options.OptionsDialog.SimpleOptions
open JetBrains.IDE.UI.Options
open JetBrains.Lifetimes
open JetBrains.ReSharper.Feature.Services.InlayHints
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Resources
open JetBrains.ReSharper.Plugins.FSharp.Settings

[<OptionsPage(nameof(FSharpInlayHintsPage),
              "F#",
              null,
              Sequence = 3.,
              ParentId = InlayHintsOptionsPage.PID,
              NameResourceType = typeof<Strings>,
              NameResourceName = nameof(Strings.FSharpOptionPageTitle))>]
type FSharpInlayHintsPage(lifetime: Lifetime, optionsPageContext: OptionsPageContext, optionsSettingsSmartContext: OptionsSettingsSmartContext, [<Optional; DefaultParameterValue(false)>] wrapInScrollablePanel) =
    inherit BeSimpleOptionsPage(lifetime, optionsPageContext, optionsSettingsSmartContext, wrapInScrollablePanel)


[<OptionsPage(nameof(FSharpTypeHintsOptionsPage),
              "Type Hints",
              null,
              ParentId = nameof(FSharpInlayHintsPage),
              NestingType = OptionPageNestingType.Child,
              Sequence = 1.,
              NameResourceType = typeof<Strings>,
              NameResourceName = nameof(Strings.FSharpTypeHints_OptionsPage_Title))>]
type FSharpTypeHintsOptionsPage(lifetime: Lifetime, optionsPageContext: OptionsPageContext,
                                optionsSettingsSmartContext: OptionsSettingsSmartContext) as this =
    inherit InlayHintsOptionPageBase(lifetime, optionsPageContext, optionsSettingsSmartContext)

    do
        this.AddVisibilityHelpText()

        this.AddHeader(Strings.FSharpTypeHints_TopLevelMembersSettings_Header) |> ignore
        this.AddVisibilityOption(fun (s: FSharpTypeHintOptions) -> s.ShowTypeHintsForTopLevelMembers)
        this.AddCommentText(Strings.FSharpTypeHints_TopLevelMembersSettings_Comment)

        this.AddHeader(Strings.FSharpTypeHints_LocalBindingsSettings_Header) |> ignore
        this.AddVisibilityOption(fun (s: FSharpTypeHintOptions) -> s.ShowTypeHintsForLocalBindings)

        this.AddHeader(Strings.FSharpTypeHints_OtherPatternsSettings_Header) |> ignore
        this.AddVisibilityOption(fun (s: FSharpTypeHintOptions) -> s.ShowForOtherPatterns)


        this.AddHeader(Strings.FSharpTypeHints_PipesSettings_Header) |> ignore
        this.AddBoolOption((fun (s: FSharpTypeHintOptions) -> s.ShowPipeReturnTypes),
                           Strings.FSharpTypeHints_ShowPipeReturnTypes_Description) |> ignore

        do
            use indent = this.Indent()
            let checkbox =
                this.AddBoolOption((fun (s: FSharpTypeHintOptions) -> s.HideSameLine),
                                   Strings.FSharpTypeHints_HideSameLinePipe_Description)
            this.AddBinding(checkbox, BindingStyle.IsEnabledProperty, _.ShowPipeReturnTypes, fun t -> t :> obj)
