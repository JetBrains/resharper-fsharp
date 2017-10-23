using System;
using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon.CaretDependentFeatures;
using JetBrains.ReSharper.Daemon.CSharp.ContextHighlighters;
using JetBrains.ReSharper.Feature.Services.Contexts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.FSharp.Psi;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.ContextHighlighters
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

    protected override void CollectHighlightings(IPsiDocumentRangeView documentView, HighlightingsConsumer consumer)
    {
      var psiView = documentView.View<FSharpLanguage>();
      var token = psiView.GetSelectedTreeNode<FSharpIdentifierToken>();
      if (token == null)
        return;

      // todo: type parameters: t<$caret$type> or t<'$caret$ttype>
      // todo: namespaces, use R# search?

      var file = psiView.GetSelectedTreeNode<IFSharpFile>();
      var checkResults = file?.GetParseAndCheckResults(true)?.Value.CheckResults;
      if (checkResults == null)
        return;

      var symbol = FSharpSymbolsUtil.TryFindFSharpSymbol(file, token.GetText(), token.GetTreeEndOffset().Offset);
      if (symbol == null)
        return;

      var symbolUsages = checkResults.GetUsesOfSymbolInFile(symbol).RunAsTask();
      foreach (var symbolUse in symbolUsages ?? EmptyArray<FSharpSymbolUse>.Instance)
      {
        var treeOffset = documentView.DocumentRangeFromMainDocument.Document.GetTreeEndOffset(symbolUse.RangeAlternate);
        var usageToken = file.FindTokenAt(treeOffset - 1) as FSharpIdentifierToken;
        if (usageToken == null)
          continue;

        var tokenType = usageToken.GetTokenType();
        if ((tokenType == FSharpTokenType.GREATER || tokenType == FSharpTokenType.GREATER_RBRACK)
            && !symbol.IsOpGreaterThan())
          continue; // found usage of generic symbol with specified type parameter

        consumer.ConsumeHighlighting(HighlightingId, usageToken.GetDocumentRange());
      }
    }
  }
}