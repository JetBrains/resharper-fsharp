using JetBrains.Application.Parts;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.Tooltips;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon.Attributes;
using JetBrains.ReSharper.Feature.Services.Descriptions;
using JetBrains.ReSharper.Feature.Services.UI;
using JetBrains.ReSharper.Psi;
using JetBrains.UI.RichText;
using static JetBrains.ReSharper.Psi.DeclaredElementPresentationPartKind;

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

  [SolutionComponent(InstantiationEx.LegacyDefault)]
  internal class FSharpIdentifierTooltipProvider : IdentifierTooltipProvider<FSharpLanguage>
  {
    public FSharpIdentifierTooltipProvider(Lifetime lifetime, ISolution solution,
      IDeclaredElementDescriptionPresenter presenter, DeclaredElementPresenterTextStylesService textStylesService,
      IIdentifierTooltipSuppressor identifierTooltipSuppressor)
      : base(lifetime, solution, presenter, textStylesService, identifierTooltipSuppressor, Styles)
    {
    }

    private static DeclaredElementPresenterTextStyles Styles { get; } = new DeclaredElementPresenterTextStyles
    {
      [Keyword] = new TextStyle(FSharpHighlightingAttributeIdsModule.Keyword),
      [Namespace] = new TextStyle(FSharpHighlightingAttributeIdsModule.Namespace),
      [Type] = new TextStyle(FSharpHighlightingAttributeIdsModule.Class),
      [TypeUnresolved] = new TextStyle(AnalysisHighlightingAttributeIds.UNRESOLVED_ERROR),
      [Method] = new TextStyle(FSharpHighlightingAttributeIdsModule.Method),
      [SignOperator] = new TextStyle(FSharpHighlightingAttributeIdsModule.Operator),
      [LocalVariable] = new TextStyle(FSharpHighlightingAttributeIdsModule.Value),
      [LocalFunction] = new TextStyle(FSharpHighlightingAttributeIdsModule.Function),
      [Field] = new TextStyle(FSharpHighlightingAttributeIdsModule.Field),
      [Property] = new TextStyle(FSharpHighlightingAttributeIdsModule.Property),
      [Event] = new TextStyle(FSharpHighlightingAttributeIdsModule.Event),
      [Constructor] = new TextStyle(FSharpHighlightingAttributeIdsModule.Class),
      [String] = new TextStyle(FSharpHighlightingAttributeIdsModule.String),
      [Number] = new TextStyle(FSharpHighlightingAttributeIdsModule.Number),
      [Comment] = new TextStyle(FSharpHighlightingAttributeIdsModule.LineComment),
    }.Freeze();
  }
}
