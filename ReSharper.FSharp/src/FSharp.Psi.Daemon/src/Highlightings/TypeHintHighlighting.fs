namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings

open System
open JetBrains.Application.BuildScript.Application.Zones
open JetBrains.DocumentModel
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.Daemon.Attributes
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Resources.Shell
open JetBrains.TextControl
open JetBrains.TextControl.DocumentMarkup.Adornments
open JetBrains.UI.RichText

[<DaemonAdornmentProvider(typeof<TypeHintAdornmentProvider>)>]
[<StaticSeverityHighlighting(Severity.INFO,
     typeof<HighlightingGroupIds.IntraTextAdornments>,
     AttributeId = AnalysisHighlightingAttributeIds.PARAMETER_NAME_HINT,
     OverlapResolve = OverlapResolveKind.NONE,
     ShowToolTipInStatusBar = false)>]
[<ZoneMarker(typeof<IDocumentModelZone>, typeof<PsiFeaturesImplZone>)>]
type TypeHintHighlighting(typeNameString: string, range: DocumentRange) =
    let text = RichText(": " + typeNameString)

    interface IHighlighting with
        member x.ToolTip = null
        member x.ErrorStripeToolTip = null
        member x.IsValid() = x.IsValid()
        member x.CalculateRange() = range

    interface IHighlightingWithTestOutput with
        member x.TestOutput = text.Text

    member x.Text = text
    member x.IsValid() = not text.IsEmpty && range.IsEmpty

and [<SolutionComponent>] [<ZoneMarker(typeof<IProjectModelZone>, typeof<ITextControlsZone>, typeof<PsiFeaturesImplZone>)>] TypeHintAdornmentProvider() =
    interface IHighlighterAdornmentProvider with
        member x.IsValid(highlighter) =
            match highlighter.UserData with
            | :? TypeHintHighlighting as dm -> dm.IsValid()
            | _ -> false

        member x.CreateDataModel(highlighter) =
            match highlighter.UserData with
            | :? TypeHintHighlighting as thh ->
                let data =
                    AdornmentData(thh.Text, null, enum 0, AdornmentPlacement.DefaultBeforeThisChar,
                        PushToHintMode.Default)
                
                { new IAdornmentDataModel with
                    override x.ContextMenuTitle = null
                    override x.ContextMenuItems = null
                    override x.ExecuteNavigation _ = ()
                    override x.SelectionRange = Nullable()
                    override x.Data = data }
            | _ -> null
