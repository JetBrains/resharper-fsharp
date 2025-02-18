namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings

open System
open JetBrains.Application.Parts
open JetBrains.Application.Settings
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Daemon.Attributes
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Feature.Services.InlayHints
open JetBrains.ReSharper.Plugins.FSharp.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Options
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.TextControl.DocumentMarkup.Adornments
open JetBrains.UI.RichText

[<DaemonAdornmentProvider(typeof<TypeHintAdornmentProvider>)>]
[<StaticSeverityHighlighting(Severity.INFO,
     typeof<HighlightingGroupIds.IntraTextAdornments>,
     AttributeId = AnalysisHighlightingAttributeIds.PARAMETER_NAME_HINT,
     OverlapResolve = OverlapResolveKind.NONE,
     ShowToolTipInStatusBar = false)>]
type TypeHintHighlighting(typeNameString: string, range: DocumentRange, pushToHintMode: PushToHintMode, suffix,
                          bulbActionsProvider: IInlayHintBulbActionsProvider, semanticNode: IFSharpTreeNode) =
    let text = RichText(": " + typeNameString + suffix)
    new (typeNameString: string, range: DocumentRange) =
        TypeHintHighlighting(typeNameString, range, PushToHintMode.Default, "", null, null)

    interface IHighlighting with
        member x.ToolTip = null
        member x.ErrorStripeToolTip = null
        member x.IsValid() = x.IsValid()
        member x.CalculateRange() = range

    interface IInlayHintHighlighting

    interface IHighlightingWithTestOutput with
        member x.TestOutput = text.Text

    member x.Text = text
    member x.PushToHintMode = pushToHintMode
    member x.BulbActionsProvider = bulbActionsProvider
    member x.SemanticNode = semanticNode
    member x.IsValid() = not text.IsEmpty && range.IsEmpty

and [<SolutionComponent(Instantiation.DemandAnyThreadSafe)>]
    TypeHintAdornmentProvider(settingsStore: ISettingsStore, specifyTypeActionProvider: ISpecifyTypeActionProvider) =
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
                let visibilityActionsProvider = thh.BulbActionsProvider

                { new IAdornmentDataModel with
                    override x.ContextMenuTitle = null
                    override x.ContextMenuItems =
                        [|
                            // First-class context items
                            let specifyTypeAction = specifyTypeActionProvider.TryCreateSpecifyTypeAction(thh.SemanticNode)
                            if isNotNull specifyTypeAction then
                                yield specifyTypeAction

                            if isNotNull visibilityActionsProvider then
                                yield! visibilityActionsProvider.CreateChangeVisibilityBulbMenuItems(settingsStore, thh)

                            // Second-class context items
                            yield IntraTextAdornmentDataModelHelper.CreateTurnOffAllInlayHintsBulbMenuItem(settingsStore)
                            yield IntraTextAdornmentDataModelHelper.CreateConfigureBulbMenuItem(nameof(FSharpTypeHintsOptionsPage))
                        |]
                    override x.ExecuteNavigation _ = ()
                    override x.SelectionRange = Nullable()
                    override x.Data = data }
            | _ -> null
