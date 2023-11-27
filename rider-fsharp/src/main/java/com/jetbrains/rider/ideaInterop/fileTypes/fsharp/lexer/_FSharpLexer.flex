package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer;

import com.intellij.psi.tree.IElementType;
import com.intellij.lexer.*;

import java.util.Stack;

import static com.intellij.psi.TokenType.BAD_CHARACTER;
import static com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer.FSharpTokenType.*;
%%

%{
  public _FSharpLexer() {
    this((java.io.Reader)null);
  }
%}

%public
%class _FSharpLexer
%implements FlexLexer
%function advance
%type IElementType
%eofval{
  if(yystate() == IN_BLOCK_COMMENT || yystate() == IN_BLOCK_COMMENT_FROM_LINE) {
    return FillBlockComment();
  }
  else
    return MakeToken(null);
%eofval}
%unicode

%{

  public int myNestedCommentLevel;
  public int myParenLevel;
  public int myTokenLength;
  public int myBrackLevel;

  public Stack<FSharpLexerInterpolatedStringState> myInterpolatedStringStates = new Stack<>();

%}

%{

  // for sharing rules with ReSharper
  private IElementType MakeToken(IElementType type) {
    return type;
  }

  private IElementType FindKeywordByCurrentToken() {
    return FSharpKeywordsMap.findKeyword(zzBuffer, zzStartRead, zzMarkedPos);
  }

  private int level() {
    return myParenLevel + myBrackLevel;
  }

  private void StartInterpolatedString(FSharpInterpolatedStringKind kind, Integer delimiterLength)
  {
    FSharpLexerContext previousContext = new FSharpLexerContext(
      yystate(),
      myParenLevel,
      myBrackLevel,
      myNestedCommentLevel
    );

    FSharpLexerInterpolatedStringState interpolatedStringState = new FSharpLexerInterpolatedStringState(
      kind,
      delimiterLength,
      previousContext,
      new Stack<>()
    );
    myInterpolatedStringStates.push(interpolatedStringState);
  }

  public IElementType StartRawInterpolatedString()
  {
    var index = zzMarkedPos;
    zzCurrentPos = zzStartRead;
    var dollarCount = ConsumeCharSequence('$', null);

    StartInterpolatedString(FSharpInterpolatedStringKind.Raw, dollarCount);

    zzCurrentPos = index;

    return ContinueRawInterpolatedString(dollarCount, true);
  }

  private int ConsumeCharSequence(char ch, Integer max)
  {
    var start = zzCurrentPos;

    while (zzCurrentPos < zzEndRead && zzBuffer.charAt(zzCurrentPos) == ch && (max == null || zzCurrentPos - start < max))
      zzCurrentPos++;

    return zzCurrentPos - start;
  }

  private IElementType ContinueRawInterpolatedString(int dollarCount, boolean isStart)
  {
    while (zzCurrentPos < zzEndRead)
    {
      if (zzCurrentPos == zzEndRead)
      {
        return MakeRawStringToken(FSharpTokenType.UNFINISHED_RAW_INTERPOLATED_STRING);
      }

      var ch = zzBuffer.charAt(zzCurrentPos);
      if (ch == '{')
      {
        var braceCount = ConsumeCharSequence('{', null);
        if (braceCount >= dollarCount)
        {
          var tokenType = isStart
                  ? FSharpTokenType.RAW_INTERPOLATED_STRING_START
                  : FSharpTokenType.RAW_INTERPOLATED_STRING_MIDDLE;
          return MakeRawStringToken(tokenType);
        }

        continue;
      }

      if (ch == '\"')
      {
        var quoteCount = ConsumeCharSequence('\"', 3);
        if (quoteCount == 3)
        {
          var tokenType = isStart
                  ? FSharpTokenType.RAW_INTERPOLATED_STRING
                  : FSharpTokenType.RAW_INTERPOLATED_STRING_END;

          myInterpolatedStringStates.pop();
          return MakeRawStringToken(tokenType);
        }

        continue;
      }

      zzCurrentPos++;
    }

    return MakeRawStringToken(FSharpTokenType.UNFINISHED_RAW_INTERPOLATED_STRING);
  }

  IElementType MakeRawStringToken(IElementType tokenType)
  {
    zzMarkedPos = zzCurrentPos;
    return MakeToken(tokenType);
  }

  private void FinishInterpolatedString()
  {
    assert !myInterpolatedStringStates.isEmpty();

    FSharpLexerInterpolatedStringState interpolatedStringState = myInterpolatedStringStates.peek();
    FSharpLexerContext prevContext = interpolatedStringState.getPreviousLexerContext();

    zzLexicalState = prevContext.getLexerState();
    myBrackLevel = prevContext.getBrackLevel();
    myParenLevel = prevContext.getParenLevel();
    myNestedCommentLevel = prevContext.getNestedCommentLevel();

    myInterpolatedStringStates.pop();
  }

  private void PushInterpolatedStringItem(InterpolatedStringStackItem item)
  {
    if (!myInterpolatedStringStates.empty())
    {
      FSharpLexerInterpolatedStringState state = myInterpolatedStringStates.peek();
      state.getInterpolatedStringStack().push(item);
    }
  }

  private IElementType PopInterpolatedStringItem(InterpolatedStringStackItem item)
  {
    if (myInterpolatedStringStates.empty())
      return MakeToken(RBRACE);

    FSharpLexerInterpolatedStringState state = myInterpolatedStringStates.peek();
    if (state.getKind() == FSharpInterpolatedStringKind.Raw && state.getDelimiterLength() != null && item == InterpolatedStringStackItem.Brace)
    {
      Integer delimiterLength = state.getDelimiterLength();
      zzCurrentPos = zzStartRead;
      var braceCount = ConsumeCharSequence('}', null);
      if (braceCount < delimiterLength)
      {
        zzCurrentPos = zzMarkedPos = zzStartRead + 1;
        return MakeToken(RBRACE);
      }

      return ContinueRawInterpolatedString(delimiterLength, false);
    }


    if (state.getInterpolatedStringStack().empty() && item == InterpolatedStringStackItem.Brace)
    {
      yypushback(1);
      yybegin(ToState(myInterpolatedStringStates.peek()));
      Clear();

      return null;
    }

    if (state.getInterpolatedStringStack().peek() == item)
      state.getInterpolatedStringStack().pop();

    return MakeToken(RBRACE);
  }

  public static int ToState(FSharpLexerInterpolatedStringState interpolatedStringState)
  {
    switch (interpolatedStringState.getKind())
    {
      case Regular: return ISR;
      case Verbatim: return ISV;
      case TripleQuote: return ISTQ;
      default: return LINE; // todo: check this
    }
  }

  public boolean isRestartableState() {
    return myInterpolatedStringStates.isEmpty() &&
           myNestedCommentLevel == 0 &&
           myBrackLevel == 0;
  }

  private void InitBlockComment() {
    if (yystate() == LINE) {
      yybegin(IN_BLOCK_COMMENT_FROM_LINE);
    } else {
       yybegin(IN_BLOCK_COMMENT);
    }
    myNestedCommentLevel++;
  }

  private void InitStringInClockComment()
  {
    yybegin(zzLexicalState == IN_BLOCK_COMMENT ? STRING_IN_COMMENT : STRING_IN_COMMENT_FROM_LINE);
  }

  private void FinishStringInClockComment()
  {
    yybegin(zzLexicalState == STRING_IN_COMMENT ? IN_BLOCK_COMMENT : IN_BLOCK_COMMENT_FROM_LINE);
  }


  private IElementType InitIdent() {
    IElementType keyword = FindKeywordByCurrentToken();
    return MakeToken(keyword != null ? keyword : IDENT);
  }

  private IElementType IdentInTypeApp() {
    IElementType keyword = FindKeywordByCurrentToken();
    if (keyword != null) {
        return MakeToken(keyword);
    }
    return MakeToken(IDENT);
  }

  private IElementType IdentInInitTypeApp() {
    IElementType keyword = FindKeywordByCurrentToken();
    if (keyword != null) {
        yybegin(LINE);
        return MakeToken(keyword);
    }
    return MakeToken(IDENT);
  }

  private IElementType FillBlockComment() {
    if (yystate() == IN_BLOCK_COMMENT_FROM_LINE) {
      yybegin(LINE);
    } else {
      RiseFromParenLevel(0);
    }
    myNestedCommentLevel = 0;
    return MakeToken(BLOCK_COMMENT);
  }

  private void CheckGreatRBrack(int state, int finalState) {
    if (myBrackLevel > 0) {
      myBrackLevel--;
      yybegin(SYMBOLIC_OPERATOR);
    }
    else {
      InitSmashAdjacent(state, finalState);
    }
  }

  private void InitSmashAdjacent(int state, int finalState) {
    myParenLevel--;
    if (level() <= 0) {
      yybegin(finalState);
    }
    else {
      yybegin(state);
    }
  }

  private void DeepInto()
  {
    if (level() > 1 && yystate() == INIT_TYPE_APP)
      yybegin(TYPE_APP);
  }

  private void DeepIntoParenLevel() {
    myParenLevel++;
    DeepInto();
  }

  private void DeepIntoBrackLevel() {
    myBrackLevel++;
    DeepInto();
  }

  private void RiseFromParenLevel(int n) {
    myParenLevel -= n;
    if (level() > 1) {
      yybegin(TYPE_APP);
    } else if (level() <= 0) {
      yybegin(LINE);
    } else {
      yybegin(INIT_TYPE_APP);
    }
  }

  private void InitTypeApp()
  {
    if (yytext().charAt(yylength() - 1) == '/')
      yypushback(2);
    else {
      yypushback(1);
    }
    myParenLevel = 0;
    myBrackLevel = 0;
    yybegin(INIT_TYPE_APP);
  }

  private void AdjacentTypeCloseOp()
  {
    myParenLevel -= yylength();
    yypushback(yylength());
    if (level() > 0) {
      yybegin(SYMBOLIC_OPERATOR);
    }
    else {
      yybegin(GREATER_OP_SYMBOLIC_OP);
    }
  }

  private void ExitSmash(int state) {
    if (yystate() == state) {
      yybegin(LINE);
    } else {
      RiseFromParenLevel(0);
    }
  }

  private void InitSmash(int initState, int anotherState) {
    if (yystate() == LINE) {
      yybegin(initState);
    } else {
      yybegin(anotherState);
    }
  }

  private void ExitGreaterOp()
  {
    if (yystate() == GREATER_OP) {
      RiseFromParenLevel(0);
    } else {
      yybegin(SYMBOLIC_OPERATOR);
    }
  }

  private void Clear() {
  }
  private void InitTokenLength() {
    myTokenLength = 0;
  }
  private void IncreaseTokenLength(int n)
  {
    myTokenLength += n;
  }
%}

%include ../../../../../../../../../../build/backend-lexer-sources/Unicode.lex

// Unfortunately, this rule can not be shared with the backend.
OP_CHAR=([!%&*+\-./<=>@\^|~\?])

%include ../../../../../../../../../../build/backend-lexer-sources/FSharpRules.lex
