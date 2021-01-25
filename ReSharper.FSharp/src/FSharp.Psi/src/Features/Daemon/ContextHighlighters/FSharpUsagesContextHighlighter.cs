using System;
using System.Collections.Generic;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Daemon.CaretDependentFeatures;
using JetBrains.ReSharper.Daemon.CSharp.ContextHighlighters;
using JetBrains.ReSharper.Feature.Services.Contexts;
using JetBrains.ReSharper.Feature.Services.Daemon.Attributes;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Daemon.ContextHighlighters
{
  [ContainsContextConsumer]
  public class FSharpUsagesContextHighlighter : ContextHighlighterBase
  {
    private const string OpName = "FSharpUsagesContextHighlighter";

    private const string HighlightingId = GeneralHighlightingAttributeIds.USAGE_OF_ELEMENT_UNDER_CURSOR;

    [CanBeNull, AsyncContextConsumer]
    public static Action ProcessContext(
      Lifetime lifetime, [NotNull] HighlightingProlongedLifetime prolongedLifetime,
      [NotNull, ContextKey(typeof(ContextHighlighterPsiFileView.ContextKey))]
      IPsiDocumentRangeView psiDocumentRangeView,
      [NotNull] UsagesContextHighlighterAvailabilityComponent contextHighlighterAvailability)
    {
      var psiView = psiDocumentRangeView.View<FSharpLanguage>();

      foreach (var psiSourceFile in psiView.SortedSourceFiles)
        if (!contextHighlighterAvailability.IsAvailable(psiSourceFile))
          return null;

      return new FSharpUsagesContextHighlighter().GetDataProcessAction(prolongedLifetime, psiDocumentRangeView);
    }

    protected override void CollectHighlightings(IPsiDocumentRangeView psiDocumentRangeView,
      HighlightingsConsumer consumer)
    {
      var psiView = psiDocumentRangeView.View<FSharpLanguage>();
      var document = psiDocumentRangeView.DocumentRangeFromMainDocument.Document;
      var token = psiView.GetSelectedTreeNode<FSharpIdentifierToken>();

      if (token == null)
      {
        var wildPat = psiView.GetSelectedTreeNode<IWildPat>();
        if (wildPat != null)
          consumer.ConsumeHighlighting(HighlightingId, wildPat.GetDocumentRange());

        return;
      }

      // todo: type parameters: t<$caret$type> or t<'$caret$ttype>

      var fsFile = psiView.GetSelectedTreeNode<IFSharpFile>();
      var sourceFile = fsFile?.GetSourceFile();
      if (sourceFile == null)
        return;

      var symbol = fsFile.GetSymbol(token.GetTreeStartOffset().Offset);
      if (symbol == null)
        return;

      var checkResults =
        fsFile.CheckerService.TryGetStaleCheckResults(sourceFile, OpName)?.Value ??
        fsFile.GetParseAndCheckResults(true, OpName)?.Value.CheckResults;

      var ranges = new HashSet<DocumentRange>();
      AddUsagesRanges(symbol, ranges, checkResults, document, fsFile);

      if (symbol is FSharpMemberOrFunctionOrValue mfv && mfv.IsConstructor &&
          mfv.DeclaringEntity?.Value is FSharpEntity entity)
        AddUsagesRanges(entity, ranges, checkResults, document, fsFile);

      foreach (var range in ranges)
        consumer.ConsumeHighlighting(HighlightingId, range);
    }

    private static void AddUsagesRanges(FSharpSymbol symbol, HashSet<DocumentRange> ranges,
      FSharpCheckFileResults checkResults, IDocument document, IFSharpFile fsFile)
    {
      var isActivePatternCase = symbol is FSharpActivePatternCase;
      var isGreaterOp =
        symbol is FSharpMemberOrFunctionOrValue mfv && mfv.LogicalName == StandardOperatorNames.GreaterThan;

      var symbolUsages = checkResults?.GetUsesOfSymbolInFile(symbol, null);
      if (symbolUsages == null)
        return;

      foreach (var symbolUse in symbolUsages)
      {
        var treeOffset = document.GetTreeEndOffset(symbolUse.RangeAlternate);
        var usageToken = fsFile.FindTokenAt(treeOffset - 1);
        if (usageToken == null)
          continue;

        if (isActivePatternCase && symbolUse.IsFromDefinition)
        {
          if (!(symbolUse.Symbol is FSharpActivePatternCase useSymbol))
            continue;

          if (useSymbol.DeclarationLocation.Equals(symbolUse.RangeAlternate))
          {
            var caseDeclaration = usageToken.GetContainingNode<IActivePatternId>()?.Cases[useSymbol.Index];
            if (caseDeclaration != null)
            {
              ranges.Add(caseDeclaration.GetDocumentRange());
              continue;
            }
          }
        }

        if (!(usageToken is FSharpIdentifierToken identToken))
          continue;

        var tokenType = identToken.GetTokenType();

        if ((tokenType == FSharpTokenType.GREATER || tokenType == FSharpTokenType.GREATER_RBRACK) && !isGreaterOp)
          continue; // found usage of generic symbol with specified type parameter

        ranges.Add(identToken.GetDocumentRange());
      }
    }
  }
}
