package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer;

import com.intellij.psi.tree.IElementType;
import com.intellij.lexer.*;

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
    return FillBlockComment(UNFINISHED_BLOCK_COMMENT);
  }
  else
    return MakeToken(null);
%eofval}
%unicode

%{
  private int zzNestedCommentLevel = 0;
  private int zzParenLevel = 0;
  private int zzTokenLength;
  private int zzBrackLevel = 0;
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

  private int zzLevel() {
    return zzParenLevel + zzBrackLevel;
  }

  private void initBlockComment() {
    if (yystate() == LINE) {
      yybegin(IN_BLOCK_COMMENT_FROM_LINE);
    } else {
       yybegin(IN_BLOCK_COMMENT);
    }
    zzNestedCommentLevel++;
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

  private IElementType FillBlockComment(IElementType tokenType) {
    if (yystate() == IN_BLOCK_COMMENT_FROM_LINE) {
      yybegin(LINE);
    } else {
      RiseFromParenLevel(0);
    }
    zzNestedCommentLevel = 0;
    return MakeToken(tokenType);
  }

  private void CheckGreatRBrack(int state, int finalState) {
    if (zzBrackLevel > 0) {
      zzBrackLevel--;
      yybegin(SYMBOLIC_OPERATOR);
    }
    else {
      InitSmashAdjacent(state, finalState);
    }
  }

  private void InitSmashAdjacent(int state, int finalState) {
    zzParenLevel--;
    if (zzLevel() <= 0) {
      yybegin(finalState);
    }
    else {
      yybegin(state);
    }
  }

  private void DeepInto()
  {
    if (zzLevel() > 1 && yystate() == INIT_ADJACENT_TYPE_APP)
      yybegin(ADJACENT_TYPE_APP);
  }

  private void DeepIntoParenLevel() {
    zzParenLevel++;
    DeepInto();
  }

  private void DeepIntoBrackLevel() {
    zzBrackLevel++;
    DeepInto();
  }

  private void RiseFromParenLevel(int n) {
    zzParenLevel -= n;
    if (zzLevel() > 1) {
      yybegin(ADJACENT_TYPE_APP);
    } else if (zzLevel() <= 0) {
      yybegin(LINE);
    } else {
      yybegin(INIT_ADJACENT_TYPE_APP);
    }
  }

  private void initAdjacentTypeApp()
  {
    if (yytext().charAt(yylength() - 1) == '/')
      yypushback(2);
    else {
      yypushback(1);
    }
    zzParenLevel = 0;
    zzBrackLevel = 0;
    yybegin(INIT_ADJACENT_TYPE_APP);
  }

  private void adjacentTypeCloseOp()
  {
    zzParenLevel -= yylength();
    yypushback(yylength());
    if (zzLevel() > 0) {
      yybegin(SYMBOLIC_OPERATOR);
    }
    else {
      yybegin(GREATER_OP_SYMBOLIC_OP);
    }
  }

  private void exitSmash(int state) {
    if (yystate() == state) {
      yybegin(LINE);
    } else {
      RiseFromParenLevel(0);
    }
  }

  private void initSmash(int initState, int anotherState) {
    if (yystate() == LINE) {
      yybegin(initState);
    } else {
      yybegin(anotherState);
    }
  }

  private void exitGreaterOp()
  {
    if (yystate() == GREATER_OP) {
      RiseFromParenLevel(0);
    } else {
      yybegin(SYMBOLIC_OPERATOR);
    }
  }

  private void clear() {
  }
  private void initTokenLength() {
    zzTokenLength = 0;
  }
  private void increaseTokenLength(int n)
  {
    zzTokenLength += n;
  }
%}

%include ../../../../../../../../../../build/backend-lexer-sources/Unicode.lex

// Unfortunately, this rule cannote be shared with the backend
OP_CHAR=([!%&*+\-./<=>@\^|~\?])

%include ../../../../../../../../../../build/backend-lexer-sources/FSharpRules.lex
