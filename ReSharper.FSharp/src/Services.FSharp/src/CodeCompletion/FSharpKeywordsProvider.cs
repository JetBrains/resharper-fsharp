using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.FSharp;
using JetBrains.ReSharper.Psi.FSharp.Parsing;
using JetBrains.ReSharper.Psi.Resources;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Feature.Services.FSharp.CodeCompletion
{
  [Language(typeof(FSharpLanguage))]
  public class FSharpKeywordsProvider : FSharpItemsProviderBase
  {
    // todo: reuse FCS keywords list when possible
    private static readonly string[] Keywords =
    {
      "abstract", "and", "as", "assert", "asr", "begin", "class", "const", "default", "delegate", "do", "done",
      "downcast", "downto", "elif", "else", "end", "exception", "extern", "false", "finally", "fixed", "for", "fun",
      "function", "global", "if", "in", "inherit", "inline", "interface", "internal", "land", "land", "lazy", "let",
      "lor", "lsl", "lxor", "match", "member", "mod", "module", "mutable", "namespace", "new", "null", "of", "open",
      "or", "override", "private", "public", "rec", "return", "sig", "static", "struct", "then", "to", "true", "try",
      "type", "upcast", "use", "val", "void", "when", "while", "with", "yield"
    };


    protected override bool IsAvailable(FSharpCodeCompletionContext context)
    {
      return true;
    }

    protected override bool AddLookupItems(FSharpCodeCompletionContext context, GroupedItemsCollector collector)
    {
      if (!context.ShouldComplete)
        return false;

      var tokenType = context.TokenAtCaret?.GetTokenType();
      var tokenBeforeType = context.TokenBeforeCaret?.GetTokenType();
      if (tokenBeforeType == FSharpTokenType.LINE_COMMENT ||
          tokenBeforeType == FSharpTokenType.DEAD_CODE || tokenType == FSharpTokenType.DEAD_CODE)
        return false;

      if (context.TokenBeforeCaret?.GetTokenType() == FSharpTokenType.DOT)
        return false;

      if (context.TokenBeforeCaret == context.TokenAtCaret && tokenBeforeType != null &&
          (tokenBeforeType.IsComment || tokenBeforeType.IsStringLiteral || tokenBeforeType.IsConstantLiteral))
        return false;

      if (!context.Names.Item1.IsEmpty)
        return false;

      var tokenBeforeCaret = context.BasicContext.File.FindTokenAt(context.CaretOffset - 1);
      if (tokenBeforeCaret?.GetTokenType() == FSharpTokenType.DOT)
        return false;

      foreach (var keyword in Keywords)
      {
        var lookupItem = new TextLookupItem(keyword, PsiSymbolsThemedIcons.Keyword.Id);
        lookupItem.InitializeRanges(GetDefaultRanges(context), context.BasicContext);
        collector.Add(lookupItem);
      }

      return true;
    }
  }
}