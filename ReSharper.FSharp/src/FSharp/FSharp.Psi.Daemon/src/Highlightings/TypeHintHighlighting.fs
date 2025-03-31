namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings

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
open JetBrains.ReSharper.Plugins.FSharp.Intentions
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Common.ActionUtils
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Options
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Resources
open JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon.Resources
open JetBrains.TextControl.DocumentMarkup.Adornments
open JetBrains.UI.RichText

[<DaemonAdornmentProvider(typeof<TypeHintAdornmentProvider>)>]
[<StaticSeverityHighlighting(Severity.INFO,
     typeof<HighlightingGroupIds.IntraTextAdornments>,
     AttributeId = AnalysisHighlightingAttributeIds.PARAMETER_NAME_HINT,
     OverlapResolve = OverlapResolveKind.NONE,
     ShowToolTipInStatusBar = false)>]
type TypeHintHighlighting private (typeString: string, owner: ITreeNode, range: DocumentRange,
                                   pushToHintMode: PushToHintMode, bulbActionsProvider: IInlayHintBulbActionsProvider,
                                   suffix) =
    let text = RichText(": " + typeString + suffix)

    new (typeNameString: string, range: DocumentRange) =
        TypeHintHighlighting(typeNameString, null, range, PushToHintMode.Default, null, "")

    new (typeNameString: string, binding: IBinding, pushToHintMode: PushToHintMode, bulbActionsProvider: IInlayHintBulbActionsProvider) =
        let range =
            if binding.HasParameters then binding.EqualsToken.GetDocumentRange().StartOffsetRange()
            else binding.HeadPattern.GetDocumentRange().EndOffsetRange()

        TypeHintHighlighting(typeNameString, binding, range, pushToHintMode, bulbActionsProvider, " ")

    new (typeNameString: string, memberDecl: IMemberDeclaration, pushToHintMode: PushToHintMode, bulbActionsProvider: IInlayHintBulbActionsProvider) =
        let range =
            if memberDecl.ParameterPatternsEnumerable.IsEmpty() then
                memberDecl.NameIdentifier.GetDocumentRange().EndOffsetRange()
            else memberDecl.EqualsToken.GetDocumentRange().StartOffsetRange()

        TypeHintHighlighting(typeNameString, memberDecl, range, pushToHintMode, bulbActionsProvider, " ")

    new (typeNameString: string, pat: IFSharpPattern, pushToHintMode: PushToHintMode, bulbActionsProvider: IInlayHintBulbActionsProvider) =
        let range = pat.GetDocumentRange().EndOffsetRange()
        TypeHintHighlighting(typeNameString, pat, range, pushToHintMode, bulbActionsProvider, "")

    interface IHighlighting with
        member x.ToolTip = null
        member x.ErrorStripeToolTip = null
        member x.IsValid() = x.IsValid()
        member x.CalculateRange() = range

    interface IInlayHintHighlighting

    interface IHighlightingWithTestOutput with
        member x.TestOutput = text.Text

    member x.Text = text
    member x.TypeText = typeString
    member x.PushToHintMode = pushToHintMode
    member x.BulbActionsProvider = bulbActionsProvider
    member x.Owner = owner
    member x.IsValid() = not text.IsEmpty && range.IsEmpty

and [<SolutionComponent(Instantiation.DemandAnyThreadSafe)>]
    TypeHintAdornmentProvider(settingsStore: ISettingsStore, specifyTypeActionProvider: ISpecifyTypeActionProvider) =

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
                let visibilityActionsProvider = thh.BulbActionsProvider

                { new IAdornmentDataModel with
                    override x.ContextMenuTitle = null
                    override x.ContextMenuItems =
                        [|
                            // First-class context items
                            let specifyTypeAction = specifyTypeActionProvider.TryCreateSpecifyTypeAction(thh.Owner)
                            if isNotNull specifyTypeAction then
                                yield specifyTypeAction

                            yield createCopyToClipboardBulbItem thh highlighter

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
