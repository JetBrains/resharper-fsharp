using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
{
  public interface IFSharpIdentifierTooltipProvider
  {
  }

  [DaemonTooltipProvider(typeof(IFSharpIdentifierTooltipProvider))]
  [StaticSeverityHighlighting(Severity.INFO, typeof(HighlightingGroupIds.IdentifierHighlightings),
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
    public bool IsValid() => true;
    public DocumentRange CalculateRange() => myRange;
  }
}