using System;
using JetBrains.Annotations;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Daemon.CaretDependentFeatures;
using JetBrains.ReSharper.Feature.Services.Contexts;
using JetBrains.ReSharper.Plugins.FSharp.Psi;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.DataContext;

namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.ContextHighlighters
{
  [ContainsContextConsumer]
  public class
    FSharpTypeParametersMatchingBracesContextHighlighter : ContainingBracesContextHighlighterBase<FSharpLanguage>
  {
    [CanBeNull, AsyncContextConsumer]
    public static Action ProcessDataContext(
      Lifetime lifetime,
      [NotNull, ContextKey(typeof(ContextHighlighterPsiFileView.ContextKey))]
      IPsiDocumentRangeView psiDocumentRangeView,
      [NotNull] InvisibleBraceHintManager invisibleBraceHintManager,
      [NotNull] MatchingBraceSuggester matchingBraceSuggester,
      [NotNull] HighlightingProlongedLifetime prolongedLifetime)
    {
      var highlighter = new FSharpTypeParametersMatchingBracesContextHighlighter();
      return highlighter.ProcessDataContextImpl(
        lifetime, prolongedLifetime, psiDocumentRangeView, invisibleBraceHintManager, matchingBraceSuggester);
    }

    protected override void CollectHighlightings(IPsiView psiView, MatchingHighlightingsConsumer consumer)
    {
      TryConsumeHighlighting<ITypeParameterOfTypeList>(psiView, consumer, _ => _.LAngle, _ => _.RAngle);
      TryConsumeHighlighting<ITypeParameterOfMethodList>(psiView, consumer, _ => _.LAngle, _ => _.RAngle);

      TryConsumeHighlighting<ITypeArgumentList>(psiView, consumer, _ => _.LAngle, _ => _.RAngle);
    }
  }
}
