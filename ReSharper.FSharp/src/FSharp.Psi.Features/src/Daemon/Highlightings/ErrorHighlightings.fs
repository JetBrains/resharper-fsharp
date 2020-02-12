namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings

open System
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Feature.Services.Daemon.Attributes

[<AbstractClass>]
type FSharpErrorHighlightingBase(message, range: DocumentRange) = 
    interface IHighlighting with
        member x.ToolTip = message
        member x.ErrorStripeToolTip = message
        member x.IsValid() = range.IsValid()
        member x.CalculateRange() = range


[<StaticSeverityHighlighting(Severity.ERROR, HighlightingGroupIds.IdentifierHighlightingsGroup,
                             AttributeId = AnalysisHighlightingAttributeIds.ERROR,
                             OverlapResolve = OverlapResolveKind.NONE)>]
type ErrorHighlighting(message, range) =
    inherit FSharpErrorHighlightingBase(message, range)


[<StaticSeverityHighlighting(Severity.WARNING, HighlightingGroupIds.IdentifierHighlightingsGroup,
                             AttributeId = AnalysisHighlightingAttributeIds.WARNING,
                             OverlapResolve = OverlapResolveKind.NONE)>]
type WarningHighlighting(message, range) =
    inherit FSharpErrorHighlightingBase(message, range)


[<StaticSeverityHighlighting(Severity.ERROR, HighlightingGroupIds.IdentifierHighlightingsGroup,
                             AttributeId = AnalysisHighlightingAttributeIds.UNRESOLVED_ERROR,
                             OverlapResolve = OverlapResolveKind.NONE)>]
type UnresolvedHighlighting(message, range) =
    inherit FSharpErrorHighlightingBase(message, range)


[<StaticSeverityHighlighting(Severity.WARNING, HighlightingGroupIds.IdentifierHighlightingsGroup, 
                             AttributeId = AnalysisHighlightingAttributeIds.DEADCODE,
                             OverlapResolve = OverlapResolveKind.NONE)>]
type UnusedHighlighting(message, range) = 
    inherit FSharpErrorHighlightingBase(message, range)


[<StaticSeverityHighlighting(Severity.ERROR, HighlightingGroupIds.IdentifierHighlightingsGroup,
                             AttributeId = AnalysisHighlightingAttributeIds.ERROR,
                             OverlapResolve = OverlapResolveKind.NONE)>]
type UseKeywordIllegalInPrimaryCtor(message, range) =
    inherit FSharpErrorHighlightingBase(message, range)


[<StaticSeverityHighlighting(Severity.INFO, HighlightingGroupIds.IdentifierHighlightingsGroup,
                             AttributeId = AnalysisHighlightingAttributeIds.DEADCODE,
                             OverlapResolve = OverlapResolveKind.NONE)>]
type DeadCodeHighlighting(range) =
    interface IHighlighting with
        member x.IsValid() = true
        member x.CalculateRange() = range
        member x.ToolTip = String.Empty
        member x.ErrorStripeToolTip = String.Empty
