using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.FSharp;
using JetBrains.ReSharper.Psi.FSharp.Parsing;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.Extension;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Feature.Services.FSharp.CodeCompletion
{
  [Language(typeof(FSharpLanguage))]
  public class FSharpLookupItemsProvider : FSharpItemsProviderBase
  {
    protected override bool AddLookupItems(FSharpCodeCompletionContext context, GroupedItemsCollector collector)
    {
      var completionContext = context.BasicContext;
      var fsFile = completionContext.File as IFSharpFile;
      Assertion.AssertNotNull(fsFile, "fsFile != null");

      if (fsFile.ParseResults == null)
        return true;

      var completions = GetFSharpCompletions(context, fsFile);
      if (completions == null || completions.IsEmpty)
        return true;

      foreach (var overloadsGroup in completions)
      {
        if (overloadsGroup.IsEmpty)
          continue;

        var symbol = overloadsGroup.Head.Symbol;
        bool isEscaped;
        var lookupText = GetLookupText(symbol, IsInAttributeList(fsFile, context), out isEscaped);
        var lookupItem = new FSharpLookupItem(lookupText, symbol.GetIconId(), isEscaped, symbol.IsParam());
        lookupItem.InitializeRanges(GetDefaultRanges(context), context.BasicContext);
        collector.Add(lookupItem);
      }

      return true;
    }

    private static bool IsInAttributeList([NotNull] IFSharpFile fsFile, [NotNull] FSharpCodeCompletionContext context)
    {
      if (context.TokenAtCaret?.GetNextMeaningfulToken(true)?.GetTokenType() == FSharpTokenType.GREATER_RBRACK)
        return true;

      var fsCompletionContext = UntypedParseImpl.TryGetCompletionContext(context.Coords.GetPos(),
        OptionModule.OfObj(fsFile.ParseResults), context.LineText);
      return fsCompletionContext != null && fsCompletionContext.Value.IsAttributeApplication;
    }

    [NotNull]
    private static string GetLookupText([NotNull] FSharpSymbol symbol, bool isAttrApplication, out bool isEscaped)
    {
      var entity = symbol as FSharpEntity;
      if (entity != null && !entity.IsUnresolved && entity.IsAttributeType && isAttrApplication)
      {
        isEscaped = false;
        return entity.CompiledName.SubstringBeforeLast("Attribute");
      }
      var mfv = symbol as FSharpMemberOrFunctionOrValue;
      if (mfv != null && PrettyNaming.IsOperatorName(symbol.DisplayName))
      {
        isEscaped = false;
        return mfv.CompiledName;
      }

      return FSharpNamesUtil.RemoveParens(symbol.DisplayName, out isEscaped);
    }

    [CanBeNull]
    private FSharpList<FSharpList<FSharpSymbolUse>> GetFSharpCompletions([NotNull] FSharpCodeCompletionContext context,
      [NotNull] IFSharpFile fsFile)
    {
      var completionContext = context.BasicContext;
      var document = completionContext.Document;
      var parseResults = new FSharpOption<FSharpParseFileResults>(fsFile.ParseResults);
      var qualifiers = context.Names.Item1;
      var partialName = context.Names.Item2;

      var checkResults = fsFile.GetCheckResults();
      if (checkResults == null)
        return null;

      var coords = context.Coords;
      var getCompletionsAsync = checkResults.GetDeclarationListSymbols(
        parseResults,
        (int) coords.Line + 1,
        (int) coords.Column,
        document.GetLineText(coords.Line),
        qualifiers,
        partialName,
        hasTextChangedSinceLastTypecheck: null);

      return FSharpAsync.RunSynchronously(getCompletionsAsync, null, null);
    }
  }
}