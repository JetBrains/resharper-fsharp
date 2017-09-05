using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Highlightings
{
  [StaticSeverityHighlighting(Severity.INFO, HighlightingGroupIds.IdentifierHighlightingsGroup,
    AttributeId = HighlightingAttributeIds.DEADCODE_ATTRIBUTE, OverlapResolve = OverlapResolveKind.DEADCODE)]
  public class DeadCodeHighlighting : IHighlighting
  {
    private readonly DocumentRange myRange;
    public DeadCodeHighlighting(DocumentRange range) => myRange = range;
    public bool IsValid() => true;
    public DocumentRange CalculateRange() => myRange;
    public string ToolTip => string.Empty;
    public string ErrorStripeToolTip => string.Empty;
  }
}