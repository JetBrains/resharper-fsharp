using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Highlightings
{
  [StaticSeverityHighlighting(Severity.WARNING, HighlightingGroupIds.IdentifierHighlightingsGroup,
    AttributeId = HighlightingAttributeIds.WARNING_ATTRIBUTE, OverlapResolve = OverlapResolveKind.WARNING)]
  public class WarningHighlighting : ErrorOrWarningHighlightingBase
  {
    public WarningHighlighting([NotNull] string message, DocumentRange range) : base(message, range)
    {
    }
  }
}