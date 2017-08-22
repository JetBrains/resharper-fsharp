using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages;

namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Highlightings
{
  [DaemonTooltipProvider(typeof(FSharpIdentifierTooltipProvider))]
  [StaticSeverityHighlighting(Severity.INFO, HighlightingGroupIds.IdentifierHighlightingsGroup,
    OverlapResolve = OverlapResolveKind.NONE, ShowToolTipInStatusBar = false)]
  public class FSharpIdentifierHighlighting : ICustomAttributeIdHighlighting
  {
    private readonly DocumentRange myRange;

    public FSharpIdentifierHighlighting(string attributeId, DocumentRange range)
    {
      AttributeId = attributeId;
      myRange = range;
    }

    public string AttributeId { get; }
    public virtual string ToolTip => string.Empty;
    public string ErrorStripeToolTip => string.Empty;

    public bool IsValid()
    {
      return true;
    }

    public DocumentRange CalculateRange()
    {
      return myRange;
    }
  }
}