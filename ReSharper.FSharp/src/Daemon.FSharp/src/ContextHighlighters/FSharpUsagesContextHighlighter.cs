using System;
using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Daemon.CaretDependentFeatures;
using JetBrains.ReSharper.Daemon.CSharp.ContextHighlighters;
using JetBrains.ReSharper.Feature.Services.Contexts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.FSharp;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Parsing;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Control;

namespace JetBrains.ReSharper.Daemon.FSharp.ContextHighlighters
{
  [ContainsContextConsumer]
  public class FSharpUsagesContextHighlighter : ContextHighlighterBase
  {
    private const string HighlightingId = HighlightingAttributeIds.USAGE_OF_ELEMENT_UNDER_CURSOR;

    [CanBeNull, AsyncContextConsumer]
    public static Action ProcessContext(
      [NotNull] Lifetime lifetime, [NotNull] HighlightingProlongedLifetime prolongedLifetime,
      [NotNull, ContextKey(typeof(ContextHighlighterPsiFileView.ContextKey))]
      IPsiDocumentRangeView psiDocumentRangeView,
      [NotNull] UsagesContextHighlighterAvailabilityComponent contextHighlighterAvailability)
    {
      var psiView = psiDocumentRangeView.View<FSharpLanguage>();

      foreach (var psiSourceFile in psiView.SortedSourceFiles)
        if (!contextHighlighterAvailability.IsAvailable(psiSourceFile)) return null;

      return new FSharpUsagesContextHighlighter().GetDataProcessAction(prolongedLifetime, psiDocumentRangeView);
    }

    protected override void CollectHighlightings(IPsiDocumentRangeView psiDocumentRangeView,
      HighlightingsConsumer consumer)
    {
      var psiView = psiDocumentRangeView.View<FSharpLanguage>();
      var file = psiView.GetSelectedTreeNode<IFSharpFile>();
      if (file == null || !file.IsChecked)
        return;


      var document = psiDocumentRangeView.DocumentRangeFromMainDocument.Document;
      var token = psiView.GetSelectedTreeNode<FSharpIdentifierToken>();
      if (token == null)
        return;

      // todo: type parameters t<$caret$type> or t<'$caret$ttype>
      // todo: namespaces, use R# search?

      var checkResults = file.GetCheckResults();
      if (checkResults == null)
        return;

      var symbol = FSharpSymbolsUtil.TryFindFSharpSymbol(file, token.GetText(), token.GetTreeEndOffset().Offset);
      if (symbol == null)
        return;

      var symbolUsages = FSharpAsync.RunSynchronously(checkResults.GetUsesOfSymbolInFile(symbol), null, null);
      foreach (var symbolUse in symbolUsages)
      {
        var treeOffset = document.GetTreeEndOffset(symbolUse.RangeAlternate);
        var usageToken = file.FindTokenAt(treeOffset - 1) as FSharpIdentifierToken;
        if (usageToken == null)
          continue;

        var tokenType = usageToken.GetTokenType();
        if ((tokenType == FSharpTokenType.GREATER || tokenType == FSharpTokenType.GREATER_RBRACK)
            && !FSharpSymbolsUtil.IsOpGreaterThan(symbol))
          continue; // found usage of generic symbol with specified type parameter

        consumer.ConsumeHighlighting(HighlightingId, usageToken.GetDocumentRange());
      }
    }
  }
}