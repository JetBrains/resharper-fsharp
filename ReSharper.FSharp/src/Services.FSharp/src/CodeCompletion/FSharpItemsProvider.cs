using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.FSharp;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using Microsoft.VisualStudio.FSharp.LanguageService;

namespace JetBrains.ReSharper.Feature.Services.FSharp.CodeCompletion
{
  [Language(typeof(FSharpLanguage))]
  public class FSharpItemsProvider : ItemsProviderOfSpecificContext<FSharpCodeCompletionContext>
  {
    protected override bool AddLookupItems(FSharpCodeCompletionContext context, GroupedItemsCollector collector)
    {
      var completionContext = context.BasicContext;
      var fsFile = completionContext.File as IFSharpFile;
      Assertion.AssertNotNull(fsFile, "fsFile != null");

      if (fsFile.ParseResults == null)
        return false;

      var completions = GetFSharpCompletions(completionContext, fsFile);
      if (completions == null)
        return false;

      foreach (var overloadsGroup in completions)
      {
        if (overloadsGroup.IsEmpty)
          continue;

        var symbol = overloadsGroup.Head.Symbol;
        if (symbol.DisplayName.Contains(' '))
          continue;

        var lookupItem = new TextLookupItem(symbol.DisplayName);
        lookupItem.InitializeRanges(GetDefaultRanges(context), context.BasicContext);
        collector.Add(lookupItem);
      }

      return true;
    }

    protected override TextLookupRanges GetDefaultRanges(FSharpCodeCompletionContext context)
    {
      return context.Ranges;
    }

    [CanBeNull]
    private FSharpList<FSharpList<FSharpSymbolUse>> GetFSharpCompletions(
      [NotNull] CodeCompletionContext completionContext,
      [NotNull] IFSharpFile fsFile)
    {
      var document = completionContext.Document;
      var caretOffset = completionContext.CaretTreeOffset.Offset;
      var coords = document.GetCoordsByOffset(caretOffset);
      var parseResults = new FSharpOption<FSharpParseFileResults>(fsFile.ParseResults);
      var checkResults = fsFile.GetCheckResults();
      if (checkResults == null)
        return null;

      var names = QuickParse.GetPartialLongNameEx(document.GetLineText(coords.Line), (int) coords.Column - 1);
      var getCompletionsAsync = checkResults.GetDeclarationListSymbols(
        parseResults,
        (int) coords.Line + 1,
        (int) coords.Column,
        document.GetLineText(coords.Line),
        qualifyingNames: names.Item1,
        partialName: names.Item2,
        hasTextChangedSinceLastTypecheck: null);

      return FSharpAsync.RunSynchronously(getCompletionsAsync, null, null);
    }
  }
}