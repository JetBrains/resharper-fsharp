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
  private int myNestedCommentLevel = 0;
  private int myParenLevel = 0;
  private int myTokenLength;
  private int myBrackLevel = 0;

  private Stack<FSharpLexerInterpolatedStringState> myInterpolatedStringStates =
        new Stack<FSharpLexerInterpolatedStringState>();
%}

%{
  // for sharing rules with ReSharper
  private IElementType MakeToken(IElementType type) {
    return type;
  }

  private IElementType FindKeywordByCurrentToken() {
    return FSharpKeywordsMap.findKeyword(zzBuffer, zzStartRead, zzMarkedPos);
  }

  private IElementType FindReservedKeywordByCurrentToken() {
    return FSharpReservedKeywordsMap.findKeyword(zzBuffer, zzStartRead, zzMarkedPos);
  }

  private int level() {
    return myParenLevel + myBrackLevel;
  }

  private void StartInterpolatedString(FSharpInterpolatedStringKind kind)
  {
    FSharpLexerContext previousContext = new FSharpLexerContext(
      yystate(),
      myParenLevel,
      myBrackLevel,
      myNestedCommentLevel
    );

    FSharpLexerInterpolatedStringState interpolatedStringState = new FSharpLexerInterpolatedStringState(
      kind,
      previousContext,
      new Stack<InterpolatedStringStackItem>()
    );
    myInterpolatedStringStates.push(interpolatedStringState);
  }

  private void FinishInterpolatedString()
  {
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

  private boolean PopInterpolatedStringItem(InterpolatedStringStackItem item)
  {
    if (myInterpolatedStringStates.empty()) return false;
    FSharpLexerInterpolatedStringState state = myInterpolatedStringStates.peek();

    if (state.getInterpolatedStringStack().empty() && item == InterpolatedStringStackItem.Brace)
    {
      yypushback(1);
      yybegin(ToState(myInterpolatedStringStates.peek()));
      Clear();

      return true;
    }

    if (state.getInterpolatedStringStack().peek() == item)
      state.getInterpolatedStringStack().pop();

    return false;
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
    // use if you need to separate the reserved keyword
    // IElementType reservedKeyword = FindReservedKeywordByCurrentToken();
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
