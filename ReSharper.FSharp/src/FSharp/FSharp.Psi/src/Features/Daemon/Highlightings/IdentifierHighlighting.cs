using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.Tooltips;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Descriptions;
using JetBrains.ReSharper.Feature.Services.UI;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.Highlightings
{
  [DaemonTooltipProvider(typeof(FSharpIdentifierTooltipProvider))]
  [StaticSeverityHighlighting(Severity.INFO, typeof(HighlightingGroupIds.IdentifierHighlightings),
    OverlapResolve = OverlapResolveKind.NONE, ShowToolTipInStatusBar = false)]
  public class FSharpIdentifierHighlighting : ICustomAttributeIdHighlighting, IHighlightingWithFeatureStatisticsKey
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

    public int? GetStatisticsKey() => null;
  }

  [SolutionComponent]
  internal class FSharpIdentifierTooltipProvider : IdentifierTooltipProvider<FSharpLanguage>
  {
    public FSharpIdentifierTooltipProvider(Lifetime lifetime, ISolution solution,
      IDeclaredElementDescriptionPresenter presenter, DeclaredElementPresenterTextStylesService textStylesService,
      IIdentifierTooltipSuppressor identifierTooltipSuppressor,
      [CanBeNull] DeclaredElementPresenterTextStyles textStyles = null)
      : base(lifetime, solution, presenter, textStylesService, identifierTooltipSuppressor, textStyles)
    {
    }
  }
}