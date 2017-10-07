using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Plugins.FSharp.Common.Util;
using JetBrains.ReSharper.Plugins.FSharp.Psi;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.Extension;
using JetBrains.Util.Logging;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp.Services.Cs.CodeCompletion
{
  [Language(typeof(FSharpLanguage))]
  public class FSharpLookupItemsProvider : FSharpItemsProviderBase
  {
    protected override bool AddLookupItems(FSharpCodeCompletionContext context, IItemsCollector collector)
    {
      if (!context.ShouldComplete)
        return false;

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

        var symbolUse = overloadsGroup.Head;
        var symbol = symbolUse.Symbol;

        var lookupText = GetLookupText(symbol, IsInAttributeList(context), out var isEscaped);
        var lookupItem = new FSharpLookupItem(lookupText, symbol.GetIconId(), isEscaped);
        lookupItem.InitializeRanges(GetDefaultRanges(context), context.BasicContext);

        try
        {
          var returnType = FSharpSymbolUtil.GetReturnType(symbol);
          if (returnType != null)
            lookupItem.DisplayTypeName = returnType.Value.Format(symbolUse.DisplayContext);
        }
        catch
        {
          // ignored
        }


        collector.Add(lookupItem);
      }

      return true;
    }

    private static bool IsInAttributeList([NotNull] FSharpCodeCompletionContext context)
    {
      if (context.TokenAtCaret?.GetNextMeaningfulToken(true)?.GetTokenType() == FSharpTokenType.GREATER_RBRACK)
        return true;

      var fsCompletionContext = UntypedParseImpl.TryGetCompletionContext(context.Coords.GetPos(),
        context.ParseResults, context.LineText);
      return fsCompletionContext != null && fsCompletionContext.Value.IsAttributeApplication;
    }

    [NotNull]
    private static string GetLookupText([NotNull] FSharpSymbol symbol, bool isAttrApplication, out bool isEscaped)
    {
      if (symbol is FSharpEntity entity && !entity.IsUnresolved && isAttrApplication)
      {
        try
        {
          if (entity.IsAttributeType)
          {
            isEscaped = false;
            return entity.CompiledName.GetAttributeShortName();
          }
        }
        catch
        {
          // ignored
        }
      }
      if (symbol is FSharpMemberOrFunctionOrValue mfv && PrettyNaming.IsOperatorName(symbol.DisplayName))
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
      var qualifiers = context.Names.Item1;
      var partialName = context.Names.Item2;

      var checkResults = fsFile.GetParseAndCheckResults(true)?.Value.CheckResults;
      if (checkResults == null)
        return null;

      var coords = context.Coords;
      var getCompletionsAsync = checkResults.GetDeclarationListSymbols(
        context.ParseResults,
        (int) coords.Line + 1,
        (int) coords.Column,
        document.GetLineText(coords.Line),
        qualifiers,
        partialName,
        hasTextChangedSinceLastTypecheck: null,
        userOpName: FSharpOption<string>.None);

      try
      {
        return getCompletionsAsync.RunAsTask();
      }
      catch (Exception e)
      {
        Logger.LogMessage(LoggingLevel.WARN, "Error while getting symbol at location: {0}: {1}",
          context.BasicContext.SourceFile.GetLocation().FullPath, coords);
        Logger.LogExceptionSilently(e);
        return null;
      }
    }
  }
}