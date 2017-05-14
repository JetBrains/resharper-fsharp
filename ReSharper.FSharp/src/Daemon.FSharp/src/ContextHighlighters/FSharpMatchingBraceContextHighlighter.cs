using System;
using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Daemon.CaretDependentFeatures;
using JetBrains.ReSharper.Feature.Services.Contexts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.FSharp;
using JetBrains.ReSharper.Psi.FSharp.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Daemon.FSharp.ContextHighlighters
{
  [ContainsContextConsumer]
  public class FSharpMatchingBraceContextHighlighter : MatchingBraceContextHighlighterBase<FSharpLanguage>
  {
    [CanBeNull, AsyncContextConsumer]
    public static Action ProcessDataContext([NotNull] Lifetime lifetime,
      [NotNull, ContextKey(typeof(ContextHighlighterPsiFileView.ContextKey))]
      IPsiDocumentRangeView psiDocumentRangeView, [NotNull] InvisibleBraceHintManager invisibleBraceHintManager,
      [NotNull] MatchingBraceSuggester matchingBraceSuggester,
      [NotNull] HighlightingProlongedLifetime prolongedLifetime)
    {
      var highlighter = new FSharpMatchingBraceContextHighlighter();
      return highlighter.ProcessDataContextImpl(
        lifetime, prolongedLifetime, psiDocumentRangeView, invisibleBraceHintManager, matchingBraceSuggester);
    }

    protected override void TryHighlightToLeft(MatchingHighlightingsConsumer consumer, ITokenNode selectedToken,
      TreeOffset treeOffset)
    {
      var selectedTokenType = selectedToken.GetTokenType();
      if (IsRightBracket(selectedTokenType))
      {
        ITokenNode matchedNode;
        if (FindMatchingLeftBracket(selectedToken, out matchedNode))
          consumer.ConsumeMatchingBracesHighlighting(selectedToken.GetDocumentRange(), matchedNode.GetDocumentRange(),
            false);
        else
          consumer.ConsumeHighlighting(HighlightingAttributeIds.UNMATCHED_BRACE, selectedToken.GetDocumentRange());
      }
    }

    protected override void TryHighlightToRight(MatchingHighlightingsConsumer consumer, ITokenNode selectedToken,
      TreeOffset treeOffset)
    {
      var selectedTokenType = selectedToken.GetTokenType();
      if (IsLeftBracket(selectedTokenType))
      {
        ITokenNode matched;
        if (FindMatchingRightBracket(selectedToken, out matched))
          consumer.ConsumeMatchingBracesHighlighting(selectedToken.GetDocumentRange(), matched.GetDocumentRange(),
            false);
        else
          consumer.ConsumeHighlighting(HighlightingAttributeIds.UNMATCHED_BRACE, selectedToken.GetDocumentRange());
      }
    }

    protected override bool IsLeftBracket(TokenNodeType tokenType)
    {
      return FSharpTokenType.LeftBraces[tokenType];
    }

    protected override bool IsRightBracket(TokenNodeType tokenType)
    {
      return FSharpTokenType.RightBraces[tokenType];
    }

    protected override bool Match(TokenNodeType token1, TokenNodeType token2)
    {
      if (token1 == FSharpTokenType.LPAREN) return token2 == FSharpTokenType.RPAREN;
      if (token1 == FSharpTokenType.LBRACE) return token2 == FSharpTokenType.RBRACE;
      if (token1 == FSharpTokenType.LBRACK) return token2 == FSharpTokenType.RBRACK;
      if (token1 == FSharpTokenType.LQUOTE) return token2 == FSharpTokenType.RQUOTE;
      if (token1 == FSharpTokenType.LBRACK_BAR) return token2 == FSharpTokenType.BAR_RBRACK;
      if (token1 == FSharpTokenType.LBRACK_LESS) return token2 == FSharpTokenType.GREATER_RBRACK;
      if (token1 == FSharpTokenType.LQUOTE_TYPED) return token2 == FSharpTokenType.RQUOTE_TYPED;

      if (token1 == FSharpTokenType.RPAREN) return token2 == FSharpTokenType.LPAREN;
      if (token1 == FSharpTokenType.RBRACE) return token2 == FSharpTokenType.LBRACE;
      if (token1 == FSharpTokenType.RBRACK) return token2 == FSharpTokenType.LBRACK;
      if (token1 == FSharpTokenType.RQUOTE) return token2 == FSharpTokenType.LQUOTE;
      if (token1 == FSharpTokenType.BAR_RBRACK) return token2 == FSharpTokenType.LBRACK_BAR;
      if (token1 == FSharpTokenType.GREATER_RBRACK) return token2 == FSharpTokenType.LBRACK_LESS;
      if (token1 == FSharpTokenType.RQUOTE_TYPED) return token2 == FSharpTokenType.LQUOTE_TYPED;

      return false;
    }
  }
}