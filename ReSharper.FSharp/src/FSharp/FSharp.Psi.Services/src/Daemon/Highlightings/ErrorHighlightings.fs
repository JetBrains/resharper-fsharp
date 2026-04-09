namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings

open System
open JetBrains.DocumentModel
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Feature.Services.Daemon.Attributes

[<RegisterStaticHighlightingsGroup("F# Errors", true)>]
type FSharpErrors() =
    class end

[<AbstractClass>]
type FSharpErrorHighlightingBase(message, range: DocumentRange) =
    interface IHighlighting with
        member x.ToolTip = message
        member x.ErrorStripeToolTip = message
        member x.IsValid() = range.IsValid()
        member x.CalculateRange() = range

[<Sealed>]
[<ConfigurableSeverityHighlighting("", Languages = "F#", OverlapResolve = OverlapResolveKind.NONE)>]
type FcsDiagnosticHighlighting(message: string, range: DocumentRange, compilerId, severity, defaultSeverity) =
    let title =
        // TODO: provide a title from FCS?
        if message.Length > 30 then message.Substring(0, 30) + "..."
        else message

    interface IHighlighting with
        member x.ToolTip = message
        member x.ErrorStripeToolTip = message
        member x.IsValid() = range.IsValid()
        member x.CalculateRange() = range

    interface ICustomSeverityHighlighting with
        member this.Severity = severity

    interface ICustomHighlightingWithConfigurableSeverityItem with
        member this.ConfigurableSeverityItem =
            ConfigurableSeverityItem(compilerId, null, HighlightingGroupIds.CompilerWarnings,
                                     title, null, defaultSeverity,
                                     compilerIds = compilerId)

    interface ICustomCompilerIdHighlighting with
        member this.CompilerId = compilerId
        member this.Title = title


[<StaticSeverityHighlighting(Severity.ERROR, typeof<HighlightingGroupIds.IdentifierHighlightings>,
                             AttributeId = AnalysisHighlightingAttributeIds.ERROR,
                             OverlapResolve = OverlapResolveKind.NONE)>]
type ErrorHighlighting(message, range) =
    inherit FSharpErrorHighlightingBase(message, range)


[<StaticSeverityHighlighting(Severity.ERROR, typeof<HighlightingGroupIds.IdentifierHighlightings>,
                             AttributeId = AnalysisHighlightingAttributeIds.UNRESOLVED_ERROR,
                             OverlapResolve = OverlapResolveKind.NONE)>]
type UnresolvedHighlighting(message, range) =
    inherit FSharpErrorHighlightingBase(message, range)


[<StaticSeverityHighlighting(Severity.WARNING, typeof<HighlightingGroupIds.IdentifierHighlightings>,
                             AttributeId = AnalysisHighlightingAttributeIds.DEADCODE,
                             OverlapResolve = OverlapResolveKind.NONE)>]
type UnusedHighlighting(message, range) =
    inherit FSharpErrorHighlightingBase(message, range)


[<StaticSeverityHighlighting(Severity.ERROR, typeof<HighlightingGroupIds.IdentifierHighlightings>,
                             AttributeId = AnalysisHighlightingAttributeIds.ERROR,
                             OverlapResolve = OverlapResolveKind.NONE)>]
type UseKeywordIllegalInPrimaryCtor(message, range) =
    inherit FSharpErrorHighlightingBase(message, range)


[<StaticSeverityHighlighting(Severity.INFO, typeof<HighlightingGroupIds.IdentifierHighlightings>,
                             AttributeId = AnalysisHighlightingAttributeIds.DEADCODE,
                             OverlapResolve = OverlapResolveKind.NONE)>]
type DeadCodeHighlighting(range) =
    interface IHighlighting with
        member x.IsValid() = true
        member x.CalculateRange() = range
        member x.ToolTip = String.Empty
        member x.ErrorStripeToolTip = String.Empty
