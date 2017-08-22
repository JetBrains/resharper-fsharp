using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Highlightings
{
  [StaticSeverityHighlighting(Severity.ERROR, HighlightingGroupIds.IdentifierHighlightingsGroup,
    AttributeId = HighlightingAttributeIds.ERROR_ATTRIBUTE, OverlapResolve = OverlapResolveKind.ERROR)]
  public class ErrorHighlighting : ErrorOrWarningHighlightingBase
  {
    public ErrorHighlighting([NotNull] string message, DocumentRange range) : base(message, range)
    {
    }
  }
}