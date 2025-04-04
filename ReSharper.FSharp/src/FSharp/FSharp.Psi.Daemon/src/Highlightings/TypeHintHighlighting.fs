﻿namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings

open System
open JetBrains.Application.Parts
open JetBrains.Application.Settings
open JetBrains.Application.UI.Controls.BulbMenu.Anchors
open JetBrains.Application.UI.Controls.BulbMenu.Items
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Daemon.Attributes
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Feature.Services.InlayHints
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Common.ActionUtils
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Options
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Resources
open JetBrains.TextControl.DocumentMarkup.Adornments
open JetBrains.UI.RichText

[<DaemonAdornmentProvider(typeof<TypeHintAdornmentProvider>)>]
[<StaticSeverityHighlighting(Severity.INFO,
     typeof<HighlightingGroupIds.IntraTextAdornments>,
     AttributeId = AnalysisHighlightingAttributeIds.PARAMETER_NAME_HINT,
     OverlapResolve = OverlapResolveKind.NONE,
     ShowToolTipInStatusBar = false)>]
type TypeHintHighlighting(typeNameString: string, range: DocumentRange, pushToHintMode: PushToHintMode, suffix,
                          bulbActionsProvider: IInlayHintBulbActionsProvider) =
    let text = RichText(": " + typeNameString + suffix)
    new (typeNameString: string, range: DocumentRange) =
        TypeHintHighlighting(typeNameString, range, PushToHintMode.Default, "", null)

    interface IHighlighting with
        member x.ToolTip = null
        member x.ErrorStripeToolTip = null
        member x.IsValid() = x.IsValid()
        member x.CalculateRange() = range

    interface IInlayHintHighlighting

    interface IHighlightingWithTestOutput with
        member x.TestOutput = text.Text

    member x.Text = text
    member x.TypeText = typeNameString
    member x.PushToHintMode = pushToHintMode
    member x.BulbActionsProvider = bulbActionsProvider
    member x.IsValid() = not text.IsEmpty && range.IsEmpty

and [<SolutionComponent(Instantiation.DemandAnyThreadSafe)>]
    TypeHintAdornmentProvider(settingsStore: ISettingsStore) =

    let createCopyToClipboardBulbItem (highlighting: TypeHintHighlighting) highlighter =
        let text = highlighting.TypeText
        BulbMenuItem(ExecutableItem(fun () -> copyToClipboard text highlighter),
                     Strings.FSharpInferredTypeHighlighting_TooltipText, null, BulbMenuAnchors.FirstClassContextItems)

    interface IHighlighterAdornmentProvider with
        member x.IsValid(highlighter) =
            match highlighter.GetHighlighting() with
            | :? TypeHintHighlighting as dm -> dm.IsValid()
            | _ -> false

        member x.CreateDataModel(highlighter) =
            match highlighter.GetHighlighting() with
            | :? TypeHintHighlighting as thh ->
                let data =
                    AdornmentData(thh.Text, null, AdornmentFlags.None, AdornmentPlacement.DefaultAfterPrevChar,
                                  thh.PushToHintMode)
                let actionsProvider = thh.BulbActionsProvider

                { new IAdornmentDataModel with
                    override x.ContextMenuTitle = null
                    override x.ContextMenuItems =
                        [|
                            yield createCopyToClipboardBulbItem thh highlighter

                            if isNotNull actionsProvider then
                                yield! actionsProvider.CreateChangeVisibilityBulbMenuItems(settingsStore, thh)

                            yield IntraTextAdornmentDataModelHelper.CreateTurnOffAllInlayHintsBulbMenuItem(settingsStore)
                            yield IntraTextAdornmentDataModelHelper.CreateConfigureBulbMenuItem(nameof(FSharpTypeHintsOptionsPage))
                        |]
                    override x.ExecuteNavigation _ = ()
                    override x.SelectionRange = Nullable()
                    override x.Data = data }
            | _ -> null
