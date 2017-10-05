namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Highlightings

open JetBrains.Annotations
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Daemon

[<AbstractClass>]
type ErrorOrWarningHighlightingBase(message, range: DocumentRange) = 
    interface IHighlighting with
        member x.ToolTip = message
        member x.ErrorStripeToolTip = message
        member x.IsValid() = range.IsValid()
        member x.CalculateRange() = range


[<StaticSeverityHighlighting(Severity.ERROR, HighlightingGroupIds.IdentifierHighlightingsGroup, 
                             AttributeId = HighlightingAttributeIds.ERROR_ATTRIBUTE, 
                             OverlapResolve = OverlapResolveKind.ERROR)>]
type ErrorHighlighting(message, range) = 
    inherit ErrorOrWarningHighlightingBase(message, range)


[<StaticSeverityHighlighting(Severity.WARNING, HighlightingGroupIds.IdentifierHighlightingsGroup, 
                             AttributeId = HighlightingAttributeIds.WARNING_ATTRIBUTE, 
                             OverlapResolve = OverlapResolveKind.WARNING)>]
type WarningHighlighting(message, range) = 
    inherit ErrorOrWarningHighlightingBase(message, range)


[<StaticSeverityHighlighting(Severity.ERROR, HighlightingGroupIds.IdentifierHighlightingsGroup, 
                             AttributeId = HighlightingAttributeIds.UNRESOLVED_ERROR_ATTRIBUTE, 
                             OverlapResolve = OverlapResolveKind.UNRESOLVED_ERROR)>]
type UnresolvedHighlighting(message, range) = 
    inherit ErrorOrWarningHighlightingBase(message, range)
