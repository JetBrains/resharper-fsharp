namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.ContextHighlighters

open JetBrains.Annotations
open JetBrains.ReSharper.Daemon.CaretDependentFeatures
open JetBrains.ReSharper.Feature.Services.Contexts
open JetBrains.ReSharper.Feature.Services.Daemon
open JetBrains.ReSharper.Plugins.FSharp.Psi
open JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
open JetBrains.ReSharper.Psi.Parsing
open JetBrains.ReSharper.Psi.Tree

[<ContainsContextConsumer>]
type FSharpMatchingBraceContextHighlighter() =
    inherit MatchingBraceContextHighlighterBase<FSharpLanguage>()

    let highlight (isMatching, matched: ITokenNode) (consumer: MatchingHighlightingsConsumer) (selected: ITokenNode) =
        match isMatching, matched with
        | true, matched ->
            consumer.ConsumeMatchingBracesHighlighting(selected.GetDocumentRange(), matched.GetDocumentRange())
        | _ ->
            consumer.ConsumeHighlighting(HighlightingAttributeIds.UNMATCHED_BRACE, selected.GetDocumentRange())

    [<AsyncContextConsumer>]
    static member ProcessDataContext(lifetime,
                                     [<ContextKey(typeof<ContextHighlighterPsiFileView.ContextKey>)>] documentRangeView,
                                     invisibleBraceHintManager, matchingBraceSuggester, prolongedLifetime) =

          let highlighter = FSharpMatchingBraceContextHighlighter()
          highlighter.ProcessDataContextImpl(lifetime, prolongedLifetime, documentRangeView, invisibleBraceHintManager,
                                             matchingBraceSuggester)

    override x.IsLeftBracket(tokenType: TokenNodeType) = FSharpTokenType.LeftBraces.[tokenType]
    override x.IsRightBracket(tokenType: TokenNodeType) = FSharpTokenType.RightBraces.[tokenType]

    override x.TryHighlightToLeft(consumer, selectedToken, _) =
        if x.IsRightBracket(selectedToken.GetTokenType()) then
            highlight (x.FindMatchingLeftBracket(selectedToken)) consumer selectedToken

    override x.TryHighlightToRight(consumer, selectedToken, _) =
        if x.IsLeftBracket(selectedToken.GetTokenType()) then
            highlight (x.FindMatchingRightBracket(selectedToken)) consumer selectedToken

    override x.Match(token1, token2) =
        if token1 = FSharpTokenType.LPAREN then token2 = FSharpTokenType.RPAREN else
        if token1 = FSharpTokenType.LBRACE then token2 = FSharpTokenType.RBRACE else
        if token1 = FSharpTokenType.LBRACK then token2 = FSharpTokenType.RBRACK else
        if token1 = FSharpTokenType.LQUOTE then token2 = FSharpTokenType.RQUOTE else
        if token1 = FSharpTokenType.LBRACK_BAR then token2 = FSharpTokenType.BAR_RBRACK else
        if token1 = FSharpTokenType.LBRACK_LESS then token2 = FSharpTokenType.GREATER_RBRACK else
        if token1 = FSharpTokenType.LQUOTE_TYPED then token2 = FSharpTokenType.RQUOTE_TYPED else
    
        if token1 = FSharpTokenType.RPAREN then token2 = FSharpTokenType.LPAREN else
        if token1 = FSharpTokenType.RBRACE then token2 = FSharpTokenType.LBRACE else
        if token1 = FSharpTokenType.RBRACK then token2 = FSharpTokenType.LBRACK else
        if token1 = FSharpTokenType.RQUOTE then token2 = FSharpTokenType.LQUOTE else
        if token1 = FSharpTokenType.BAR_RBRACK then token2 = FSharpTokenType.LBRACK_BAR else
        if token1 = FSharpTokenType.GREATER_RBRACK then token2 = FSharpTokenType.LBRACK_LESS else
        if token1 = FSharpTokenType.RQUOTE_TYPED then token2 = FSharpTokenType.LQUOTE_TYPED else

        false
