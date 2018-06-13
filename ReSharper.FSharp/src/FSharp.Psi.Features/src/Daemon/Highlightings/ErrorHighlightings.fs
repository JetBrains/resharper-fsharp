namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Highlightings

open System
open JetBrains.Annotations
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Daemon

[<AbstractClass>]
type FSharpErrorHighlightingBase(message, range: DocumentRange) = 
    interface IHighlighting with
        member x.ToolTip = message
        member x.ErrorStripeToolTip = message
        member x.IsValid() = range.IsValid()
        member x.CalculateRange() = range


[<StaticSeverityHighlighting(Severity.ERROR, HighlightingGroupIds.IdentifierHighlightingsGroup, 
                             AttributeId = HighlightingAttributeIds.ERROR_ATTRIBUTE, 
                             OverlapResolve = OverlapResolveKind.NONE)>]
type ErrorHighlighting(message, range) = 
    inherit FSharpErrorHighlightingBase(message, range)


[<StaticSeverityHighlighting(Severity.WARNING, HighlightingGroupIds.IdentifierHighlightingsGroup, 
                             AttributeId = HighlightingAttributeIds.WARNING_ATTRIBUTE, 
                             OverlapResolve = OverlapResolveKind.NONE)>]
type WarningHighlighting(message, range) = 
    inherit FSharpErrorHighlightingBase(message, range)


[<StaticSeverityHighlighting(Severity.ERROR, HighlightingGroupIds.IdentifierHighlightingsGroup, 
                             AttributeId = HighlightingAttributeIds.UNRESOLVED_ERROR_ATTRIBUTE, 
                             OverlapResolve = OverlapResolveKind.NONE)>]
type UnresolvedHighlighting(message, range) = 
    inherit FSharpErrorHighlightingBase(message, range)

[<StaticSeverityHighlighting(Severity.WARNING, HighlightingGroupIds.IdentifierHighlightingsGroup, 
                             AttributeId = HighlightingAttributeIds.DEADCODE_ATTRIBUTE, 
                             OverlapResolve = OverlapResolveKind.NONE)>]
type UnusedHighlighting(message, range) = 
    inherit FSharpErrorHighlightingBase(message, range)

[<StaticSeverityHighlighting(Severity.INFO, HighlightingGroupIds.IdentifierHighlightingsGroup,
                             AttributeId = HighlightingAttributeIds.DEADCODE_ATTRIBUTE,
                             OverlapResolve = OverlapResolveKind.NONE)>]
type DeadCodeHighlighting(range) =
    interface IHighlighting with
        member x.IsValid() = true
        member x.CalculateRange() = range
        member x.ToolTip = String.Empty
        member x.ErrorStripeToolTip = String.Empty
