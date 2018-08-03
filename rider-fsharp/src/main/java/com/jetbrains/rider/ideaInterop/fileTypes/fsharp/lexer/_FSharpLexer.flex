package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer;

import com.intellij.psi.tree.IElementType;
import com.intellij.lexer.*;

import static com.intellij.psi.TokenType.BAD_CHARACTER;
import static com.intellij.psi.TokenType.WHITE_SPACE;
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
%eof{
  return;
%eof}
%eofval{
  if(yystate() == IN_BLOCK_COMMENT) {
    return fillBlockComment(UNFINISHED_BLOCK_COMMENT);
  }
  else
    return makeToken(null);
%eofval}
%unicode

%{
  private int zzNestedCommentLevel = 0;
  private int zzParenLevel = 0;
%}

%{
  // for sharing rules with ReSharper
  private IElementType makeToken(IElementType type) {
    return type;
  }

  private IElementType FindKeywordByCurrentToken() {
    return FSharpKeywordsMap.findKeyword(zzBuffer, zzStartRead, zzMarkedPos);
  }

  private void initBlockComment() {
    if (yystate() == LINE) {
      yybegin(IN_BLOCK_COMMENT_FROM_LINE);
    } else {
       yybegin(IN_BLOCK_COMMENT);
    }
    zzNestedCommentLevel++;
  }

  private IElementType initIdent() {
    IElementType keyword = FindKeywordByCurrentToken();
    return makeToken(keyword != null ? keyword : IDENT);
  }

  private IElementType identInTypeApp() {
    IElementType keyword = FindKeywordByCurrentToken();
    if (keyword != null) {
        return makeToken(keyword);
    }
    return makeToken(IDENT);
  }

  private IElementType identInInitTypeApp() {
    IElementType keyword = FindKeywordByCurrentToken();
    if (keyword != null) {
        yybegin(LINE);
        return makeToken(keyword);
    }
    return makeToken(IDENT);
  }

  private IElementType fillBlockComment(IElementType tokenType) {
    yybegin(LINE);
    zzNestedCommentLevel = 0;
    return makeToken(tokenType);
  }

  private void initSmashAdjacent(int state, int finalState) {
    if (--zzParenLevel <= 0) {
      yybegin(finalState);
    }
    else {
      yybegin(state);
    }
  }

  private void deepIntoParenLevel() {
    if (++zzParenLevel > 1 && yystate() == INIT_ADJACENT_TYAPP)
      yybegin(ADJACENT_TYAPP);
  }

  private void riseFromParenLevel(int n) {
    zzParenLevel -= n;
    if (zzParenLevel > 1) {
      yybegin(ADJACENT_TYAPP);
    } else if (zzParenLevel <= 0) {
      yybegin(LINE);
    } else {
      yybegin(INIT_ADJACENT_TYAPP);
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
    yybegin(INIT_ADJACENT_TYAPP);
  }

  private void adjacentTypeCloseOp()
  {
    zzParenLevel -= yylength();
    yypushback(yylength());
    if (zzParenLevel > 0) {
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
      riseFromParenLevel(0);
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
      riseFromParenLevel(0);
    } else {
      yybegin(SYMBOLIC_OPERATOR);
    }
  }
%}

%state IN_BLOCK_COMMENT
%state IN_BLOCK_COMMENT_FROM_LINE
%state STRING_IN_COMMENT
%state SMASH_INT_DOT_DOT
%state SMASH_INT_DOT_DOT_FROM_LINE
%state SMASH_RQUOTE_DOT
%state SMASH_RQUOTE_DOT_FROM_LINE
%state SMASH_ADJACENT_LESS_OP
%state SMASH_ADJACENT_GREATER_BAR_RBRACK
%state SMASH_ADJACENT_GREATER_RBRACK
%state SMASH_ADJACENT_GREATER_BAR_RBRACK_FIN
%state SMASH_ADJACENT_GREATER_RBRACK_FIN
%state ADJACENT_TYPE_CLOSE_OP
%state INIT_ADJACENT_TYAPP
%state ADJACENT_TYAPP
%state SYMBOLIC_OPERATOR
%state GREATER_OP
%state GREATER_OP_SYMBOLIC_OP
%state PRE_LESS_OP
%state LINE

%state PPSHARP
%state PPSYMBOL
%state PPDIRECTIVE

WHITE_SPACE=" "+
TAB="\t"+
ANYWHITE={WHITE_SPACE}|{TAB}

NEWLINE=\n|\r\n
END_OF_LINE_COMMENT=\/\/[^\n\r]*
SHEBANG="#!"[^\n\r]*
LETTER_CHAR=\p{Lu}|\p{Ll}|\p{Lt}|\p{Lm}|\p{Lo}|\p{Nl}
DIGIT=\p{Nd}
IDENT_START_CHAR={LETTER_CHAR}|_
CONNECTING_CHAR=\p{Pc}
COMBINING_CHAR=\p{Mn}|\p{Mc}
FORMATTING_CHAR=\p{Cf}
IDENT_CHAR={LETTER_CHAR}|{CONNECTING_CHAR}|{COMBINING_CHAR}|{FORMATTING_CHAR}|{DIGIT}|['_]
IDENT_TEXT={IDENT_START_CHAR}({IDENT_CHAR}*)
IDENT={IDENT_TEXT}|``([^`\n\r\t]|`[^`\n\r\t])+``
RESERVED_IDENT_KEYWORD=
    "atomic"|"break"|"checked"|"component"|"const"|"constraint"|"constructor"|
    "continue"|"eager"|"fixed"|"fori"|"functor"|"include"|
    "measure"|"method"|"mixin"|"object"|"parallel"|"params"|"process"|"protected"|"pure"|
    "recursive"|"sealed"|"tailcall"|"trait"|"virtual"|"volatile"
RESERVED_IDENT_FORMATS={IDENT_TEXT}([!#])
ESCAPE_CHAR=[\\][\\\"\'afvntbr]
NON_ESCAPE_CHARS=[\\][^afvntbr\\\"\']
SIMPLE_CHARACTER=[^\t\b\n\r\\\'\"]
SIMPLE_STRING_CHAR=[^\"]
UNICODEGRAPH_SHORT=\\u{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}
UNICODEGRAPH_LONG =\\U{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}
TRIGRAPH=\\{DIGIT}{DIGIT}{DIGIT}
CHARACTER ={SIMPLE_CHARACTER}|{ESCAPE_CHAR}|{TRIGRAPH}|{UNICODEGRAPH_SHORT}|\"
STRING_CHAR={SIMPLE_CHARACTER}|{ESCAPE_CHAR}|{NON_ESCAPE_CHARS}|{TRIGRAPH}|{UNICODEGRAPH_SHORT}|{UNICODEGRAPH_LONG}|"'"|{NEWLINE}
CHARACTER_LITERAL='{CHARACTER}'
UNFINISHED_STRING=\"{STRING_CHAR}*
STRING={UNFINISHED_STRING}\"
VERBATIM_STRING_CHAR={SIMPLE_STRING_CHAR}|{NON_ESCAPE_CHARS}|\"\"|\\|{NEWLINE}
UNFINISHED_VERBATIM_STRING=@\"{VERBATIM_STRING_CHAR}*
VERBATIM_STRING={UNFINISHED_VERBATIM_STRING}\"
BYTECHAR='{SIMPLE_OR_ESCAPE_CHAR}'B
BYTEARRAY=\"{STRING_CHAR}*\"B
VERBATIM_BYTEARRAY=@\"{VERBATIM_STRING_CHAR}*\"B
SIMPLE_OR_ESCAPE_CHAR={ESCAPE_CHAR}|{SIMPLE_CHARACTER}
TRIPLE_QUOTED_STRING_CHAR={SIMPLE_STRING_CHAR}|{NON_ESCAPE_CHARS}|{NEWLINE}|\\
UNFINISHED_TRIPLE_QUOTED_STRING=\"\"\"((\"?){TRIPLE_QUOTED_STRING_CHAR}|\"\"{TRIPLE_QUOTED_STRING_CHAR})*(\"|\"\")?
TRIPLE_QUOTED_STRING={UNFINISHED_TRIPLE_QUOTED_STRING}\"\"\"

LET_BANG="let!"
USE_BANG="use!"
DO_BANG="do!"
YIELD_BANG="yield!"
RETURN_BANG="return!"
BAR="|"
RARROW="->"
LARROW="<-"
DOT="."
COLON=":"
LPAREN="("
RPAREN=")"
STAR="*"
LBRACK="["
RBRACK="]"
LESS="<"
GREATER=">"
LBRACK_LESS="[<"
GREATER_RBRACK=">]"
GREATER_BAR_RBRACK=">|]"
LBRACK_BAR="[|"
BAR_RBRACK="|]"
LBRACE="{"
RBRACE="}"
QUOTE="'"
HASH="#"
COLON_QMARK_GREATER=":?>"
COLON_QMARK=":?"
COLON_GREATER=":>"
DOT_DOT=\.\.
COLON_COLON="::"
COLON_EQUALS=":="
SEMICOLON_SEMICOLON=";;"
SEMICOLON=";"
EQUALS="="
UNDERSCORE="_"
QMARK="?"
QMARK_QMARK="??"
LPAREN_STAR_RPAREN="(*)"
MINUS="-"
PLUS="+"
DOLLAR="$"
PERCENT="%"
PERCENT_PERCENT="%%"
AMP="&"
AMP_AMP="&&"
COMMA=","

RESERVED_SYMBOLIC_SEQUENCE=[~']

OP_CHAR=[!%&*+-./<=>@\^|~?]
BAD_OP_CHAR={OP_CHAR}|[$:]
QUOTE_OP_LEFT="<@"|"<@@"
QUOTE_OP_RIGHT="@>"|"@@>"
SYMBOLIC_OP=[?]|"?<-"|{OP_CHAR}+|{QUOTE_OP_LEFT}|{QUOTE_OP_RIGHT}
BAD_SYMBOLIC_OP=[?]|"?<-"|{BAD_OP_CHAR}+|{QUOTE_OP_LEFT}|{QUOTE_OP_RIGHT}

LESS_OP="</"|{LESS}

HEXDIGIT={DIGIT}|[A-F]|[a-f]
OCTALDIGIT=[0-7]
BITDIGIT=[0-1]
INT={DIGIT}+
XINT=0(x|X)({HEXDIGIT}+|{OCTALDIGIT}+|{BITDIGIT}+)
SBYTE=({INT}|{XINT})y
BYTE=({INT}|{XINT})uy
INT16=({INT}|{XINT})s
UINT16=({INT}|{XINT})us
INT32=({INT}|{XINT})l
UINT32=({INT}|{XINT})ul?
NATIVEINT=({INT}|{XINT})n
UNATIVEINT=({INT}|{XINT})un
INT64=({INT}|{XINT})L
UINT64=({INT}|{XINT})[Uu]L
IEEE32={FLOAT}[Ff]|{XINT}lf
IEEE64={FLOAT}|{XINT}LF
BIGNUM={INT}[QRZING]
DECIMAL=({FLOAT}|{INT})[Mm]
FLOAT={DIGIT}+\.{DIGIT}*|{DIGIT}+(\.{DIGIT}*)?[Ee][+-]?{DIGIT}+
RESERVED_LITERAL_FORMATS=({XINT}|{IEEE32}|{IEEE64}){IDENT_CHAR}+

KEYWORD_STRING_SOURCE_DIRECTORY="__SOURCE_DIRECTORY__"
KEYWORD_STRING_SOURCE_FILE="__SOURCE_FILE__"
KEYWORD_STRING_LINE="__LINE__"

PP_COMPILER_DIRECTIVE={HASH}("if"|"else"|"endif")
PP_DIRECTIVE={ANYWHITE}*{HASH}({IDENT}|{ANYWHITE}*[0-9]+)
PP_CONDITIONAL_SYMBOL={IDENT}

%%
<YYINITIAL> [^]                                          { yypushback(1); yybegin(LINE);}
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> {
  {LESS}                                                 { deepIntoParenLevel(); return makeToken(LESS); }
  {LPAREN}                                               { deepIntoParenLevel(); return makeToken(LPAREN); }
  {LBRACK}                                               { deepIntoParenLevel(); return makeToken(LBRACK); }
  {LBRACK_LESS}                                          { deepIntoParenLevel(); return makeToken(LBRACK_LESS); }
  {GREATER}+                                             { riseFromParenLevel(yylength()); yypushback(yylength()); yybegin(GREATER_OP); }
  "</"                                                   { deepIntoParenLevel(); yypushback(2); yybegin(SMASH_ADJACENT_LESS_OP);}
  {RPAREN}                                               { riseFromParenLevel(1); return makeToken(RPAREN); }
  {RBRACK}                                               { riseFromParenLevel(1); return makeToken(RBRACK); }
  {GREATER_RBRACK}                                       { yypushback(yylength()); initSmashAdjacent(SMASH_ADJACENT_GREATER_RBRACK, SMASH_ADJACENT_GREATER_RBRACK_FIN); }
  {GREATER_BAR_RBRACK}                                   { yypushback(yylength()); initSmashAdjacent(SMASH_ADJACENT_GREATER_BAR_RBRACK, SMASH_ADJACENT_GREATER_BAR_RBRACK_FIN); }
  {GREATER}+{BAD_SYMBOLIC_OP}                            { yypushback(yylength()); yybegin(ADJACENT_TYPE_CLOSE_OP); }
  "default"                                              |
  "struct"                                               |
  "null"                                                 |
  "delegate"                                             |
  "and"                                                  |
  "when"                                                 |
  "global"                                               |
  "const"                                                |
  "true"                                                 |
  "false"                                                { return initIdent(); }
  "^"                                                    |
  "^-"                                                   |
  "/"                                                    { return makeToken(SYMBOLIC_OP); }
}

<INIT_ADJACENT_TYAPP> {IDENT}                            { return identInInitTypeApp(); }
<ADJACENT_TYAPP> {IDENT}                                 { return identInTypeApp(); }

<ADJACENT_TYPE_CLOSE_OP> {
  {GREATER}+                                             { adjacentTypeCloseOp(); }
}

<GREATER_OP,GREATER_OP_SYMBOLIC_OP> {
  {GREATER}                                              { return makeToken(GREATER); }
  [^]                                                    { yypushback(1); exitGreaterOp(); }
}

<SYMBOLIC_OPERATOR> {
  {QUOTE_OP_LEFT}                                        { riseFromParenLevel(0); return makeToken(QUOTE_OP_LEFT); }
  {QUOTE_OP_RIGHT}                                       { riseFromParenLevel(0); return makeToken(QUOTE_OP_RIGHT); }
  "@>."|"@@>."                                           { yypushback(yylength()); yybegin(SMASH_RQUOTE_DOT); }
  {BAR}                                                  { riseFromParenLevel(0); return makeToken(BAR); }
  {LARROW}                                               { riseFromParenLevel(0); return makeToken(LARROW); }
  {LPAREN}                                               { riseFromParenLevel(0); return makeToken(LPAREN); }
  {RPAREN}                                               { riseFromParenLevel(0); return makeToken(RPAREN); }
  {LBRACK}                                               { riseFromParenLevel(0); return makeToken(LBRACK); }
  {RBRACK}                                               { riseFromParenLevel(0); return makeToken(RBRACK); }
  {LBRACK_LESS}                                          { riseFromParenLevel(0); return makeToken(LBRACK_LESS); }
  {GREATER_RBRACK}                                       { riseFromParenLevel(0); return makeToken(GREATER_RBRACK); }
  {LBRACK_BAR}                                           { riseFromParenLevel(0); return makeToken(LBRACK_BAR); }
  {LESS}                                                 { riseFromParenLevel(0); return makeToken(LESS); }
  {GREATER}                                              { riseFromParenLevel(0); return makeToken(GREATER); }
  {BAR_RBRACK}                                           { riseFromParenLevel(0); return makeToken(BAR_RBRACK); }
  {LBRACE}                                               { riseFromParenLevel(0); return makeToken(LBRACE); }
  {RBRACE}                                               { riseFromParenLevel(0); return makeToken(RBRACE); }
  {GREATER_BAR_RBRACK}                                   { riseFromParenLevel(0); return makeToken(GREATER_BAR_RBRACK); }
  {COLON_QMARK_GREATER}                                  { riseFromParenLevel(0); return makeToken(COLON_QMARK_GREATER); }
  {COLON_QMARK}                                          { riseFromParenLevel(0); return makeToken(COLON_QMARK); }
  {COLON_COLON}                                          { riseFromParenLevel(0); return makeToken(COLON_COLON); }
  {COLON_EQUALS}                                         { riseFromParenLevel(0); return makeToken(COLON_EQUALS); }
  {SEMICOLON_SEMICOLON}                                  { riseFromParenLevel(0); return makeToken(SEMICOLON_SEMICOLON); }
  {SEMICOLON}                                            { riseFromParenLevel(0); return makeToken(SEMICOLON); }
  {QMARK}                                                { riseFromParenLevel(0); return makeToken(QMARK); }
  {QMARK_QMARK}                                          { riseFromParenLevel(0); return makeToken(QMARK_QMARK); }
  {LPAREN_STAR_RPAREN}                                   { riseFromParenLevel(0); return makeToken(LPAREN_STAR_RPAREN); }
  {PLUS}                                                 { riseFromParenLevel(0); return makeToken(PLUS); }
  {DOLLAR}                                               { riseFromParenLevel(0); return makeToken(DOLLAR); }
  {PERCENT}                                              { riseFromParenLevel(0); return makeToken(PERCENT); }
  {PERCENT_PERCENT}                                      { riseFromParenLevel(0); return makeToken(PERCENT_PERCENT); }
  {AMP}                                                  { riseFromParenLevel(0); return makeToken(AMP); }
  {AMP_AMP}                                              { riseFromParenLevel(0); return makeToken(AMP_AMP); }
  {RARROW}                                               { riseFromParenLevel(0); return makeToken(RARROW); }
  {DOT}                                                  { riseFromParenLevel(0); return makeToken(DOT); }
  {COLON}                                                { riseFromParenLevel(0); return makeToken(COLON); }
  {STAR}                                                 { riseFromParenLevel(0); return makeToken(STAR); }
  {QUOTE}                                                { riseFromParenLevel(0); return makeToken(QUOTE); }
  {COLON_GREATER}                                        { riseFromParenLevel(0); return makeToken(COLON_GREATER); }
  {DOT_DOT}                                              { riseFromParenLevel(0); return makeToken(DOT_DOT); }
  {EQUALS}                                               { riseFromParenLevel(0); return makeToken(EQUALS); }
  {UNDERSCORE}                                           { riseFromParenLevel(0); return makeToken(UNDERSCORE); }
  {MINUS}                                                { riseFromParenLevel(0); return makeToken(MINUS); }
  {COMMA}                                                { riseFromParenLevel(0); return makeToken(COMMA); }
  {SYMBOLIC_OP}                                          { riseFromParenLevel(0); return makeToken(SYMBOLIC_OP); }
  {BAD_SYMBOLIC_OP}                                      { riseFromParenLevel(0); return makeToken(BAD_SYMBOLIC_OP); }
}

<LINE, ADJACENT_TYAPP> {
  <INIT_ADJACENT_TYAPP> {
    {TAB}                                                { return makeToken(BAD_TAB); }
    {WHITE_SPACE}                                        { return makeToken(WHITE_SPACE); }
    {NEWLINE}                                            { yybegin(YYINITIAL); return makeToken(NEWLINE); }
  }

  "(*"                                                   { initBlockComment(); }
  {END_OF_LINE_COMMENT}                                  { return makeToken(END_OF_LINE_COMMENT); }
  {SHEBANG}                                              { return makeToken(SHEBANG); }
  {HASH}"light"                                          { return makeToken(PP_LIGHT); }
  {PP_COMPILER_DIRECTIVE}                                { yypushback(yylength()); yybegin(PPSHARP); }
  ^{PP_DIRECTIVE}                                        { yypushback(yylength()); yybegin(PPDIRECTIVE); }

  {QUOTE_OP_LEFT}                                        { return makeToken(QUOTE_OP_LEFT); }
  {QUOTE_OP_RIGHT}                                       { return makeToken(QUOTE_OP_RIGHT); }
// Rule for smash RQUOTE_DOT.
// https://github.com/Microsoft/visualfsharp/blob/master/src/fsharp/LexFilter.fs#L2151
  "@>."|"@@>."                                           { yypushback(yylength()); initSmash(SMASH_RQUOTE_DOT_FROM_LINE, SMASH_RQUOTE_DOT); }
  {LET_BANG}                                             { return makeToken(LET_BANG); }
  {USE_BANG}                                             { return makeToken(USE_BANG); }
  {DO_BANG }                                             { return makeToken(DO_BANG); }
  {YIELD_BANG}                                           { return makeToken(YIELD_BANG); }
  {RETURN_BANG}                                          { return makeToken(RETURN_BANG); }
  {BAR}                                                  { return makeToken(BAR); }
  {LARROW}                                               { return makeToken(LARROW); }
  {LPAREN}                                               { return makeToken(LPAREN); }
  {RPAREN}                                               { return makeToken(RPAREN); }
  {LBRACK}                                               { return makeToken(LBRACK); }
  {RBRACK}                                               { return makeToken(RBRACK); }
  {LBRACK_LESS}                                          { return makeToken(LBRACK_LESS); }
  {GREATER_RBRACK}                                       { return makeToken(GREATER_RBRACK); }
  {LBRACK_BAR}                                           { return makeToken(LBRACK_BAR); }
  {LESS}                                                 { return makeToken(LESS); }
  {GREATER}                                              { return makeToken(GREATER); }
  {BAR_RBRACK}                                           { return makeToken(BAR_RBRACK); }
  {LBRACE}                                               { return makeToken(LBRACE); }
  {RBRACE}                                               { return makeToken(RBRACE); }
  {GREATER_BAR_RBRACK}                                   { return makeToken(GREATER_BAR_RBRACK); }
  {COLON_QMARK_GREATER}                                  { return makeToken(COLON_QMARK_GREATER); }
  {COLON_QMARK}                                          { return makeToken(COLON_QMARK); }
  {COLON_COLON}                                          { return makeToken(COLON_COLON); }
  {COLON_EQUALS}                                         { return makeToken(COLON_EQUALS); }
  {SEMICOLON_SEMICOLON}                                  { return makeToken(SEMICOLON_SEMICOLON); }
  {SEMICOLON}                                            { return makeToken(SEMICOLON); }
  {QMARK}                                                { return makeToken(QMARK); }
  {QMARK_QMARK}                                          { return makeToken(QMARK_QMARK); }
  {LPAREN_STAR_RPAREN}                                   { return makeToken(LPAREN_STAR_RPAREN); }
  {PLUS}                                                 { return makeToken(PLUS); }
  {DOLLAR}                                               { return makeToken(DOLLAR); }
  {PERCENT}                                              { return makeToken(PERCENT); }
  {PERCENT_PERCENT}                                      { return makeToken(PERCENT_PERCENT); }
  {AMP}                                                  { return makeToken(AMP); }
  {AMP_AMP}                                              { return makeToken(AMP_AMP); }
  <INIT_ADJACENT_TYAPP> {
    {RARROW}                                             { return makeToken(RARROW); }
    {DOT}                                                { return makeToken(DOT); }
    {COLON}                                              { return makeToken(COLON); }
    {STAR}                                               { return makeToken(STAR); }
    {QUOTE}                                              { return makeToken(QUOTE); }
    {HASH}                                               { return makeToken(HASH); }
    {COLON_GREATER}                                      { return makeToken(COLON_GREATER); }
    {DOT_DOT}                                            { return makeToken(DOT_DOT); }
    {EQUALS}                                             { return makeToken(EQUALS); }
    {UNDERSCORE}                                         { return makeToken(UNDERSCORE); }
    {MINUS}                                              { return makeToken(MINUS); }
    {COMMA}                                              { return makeToken(COMMA); }
  }

  <INIT_ADJACENT_TYAPP> {
    {KEYWORD_STRING_SOURCE_DIRECTORY}                    { return makeToken(KEYWORD_STRING_SOURCE_DIRECTORY); }
    {KEYWORD_STRING_SOURCE_FILE}                         { return makeToken(KEYWORD_STRING_SOURCE_FILE); }
    {KEYWORD_STRING_LINE}                                { return makeToken(KEYWORD_STRING_LINE); }
  }

  {RESERVED_IDENT_KEYWORD}                               { return makeToken(RESERVED_IDENT_KEYWORD); }
  {RESERVED_IDENT_FORMATS}                               { return makeToken(RESERVED_IDENT_FORMATS); }
}

// Rule for smahimg type apply.
// https://github.com/Microsoft/visualfsharp/blob/173513e/src/fsharp/LexFilter.fs#L2131
<LINE> {
  "delegate"{LESS_OP}                                    { initAdjacentTypeApp(); return makeToken(DELEGATE); }
  {IDENT}{LESS_OP}                                       { initAdjacentTypeApp(); return identInTypeApp(); }
  {IEEE32}{LESS_OP}                                      { initAdjacentTypeApp(); return makeToken(IEEE32); }
  {IEEE64}{LESS_OP}                                      { initAdjacentTypeApp(); return makeToken(IEEE64); }
  {DECIMAL}{LESS_OP}                                     { initAdjacentTypeApp(); return makeToken(DECIMAL); }
  {BYTE}{LESS_OP}                                        { initAdjacentTypeApp(); return makeToken(BYTE); }
  {INT16}{LESS_OP}                                       { initAdjacentTypeApp(); return makeToken(INT16); }
  {INT32}{LESS_OP}                                       { initAdjacentTypeApp(); return makeToken(INT32); }
  {INT64}{LESS_OP}                                       { initAdjacentTypeApp(); return makeToken(INT64); }
  {SBYTE}{LESS_OP}                                       { initAdjacentTypeApp(); return makeToken(SBYTE); }
  {UINT16}{LESS_OP}                                      { initAdjacentTypeApp(); return makeToken(UINT16); }
  {UINT32}{LESS_OP}                                      { initAdjacentTypeApp(); return makeToken(UINT32); }
  {UINT64}{LESS_OP}                                      { initAdjacentTypeApp(); return makeToken(UINT64); }
  {BIGNUM}{LESS_OP}                                      { initAdjacentTypeApp(); return makeToken(BIGNUM); }
  {NATIVEINT}{LESS_OP}                                   { initAdjacentTypeApp(); return makeToken(NATIVEINT); }
  "delegate"{LESS_OP}{BAD_SYMBOLIC_OP}                   { yypushback(yylength()); yybegin(PRE_LESS_OP); }
  {IDENT}{LESS_OP}{BAD_SYMBOLIC_OP}                      { yypushback(yylength()); yybegin(PRE_LESS_OP); }
  {IEEE32}{LESS_OP}{BAD_SYMBOLIC_OP}                     { yypushback(yylength()); yybegin(PRE_LESS_OP); }
  {IEEE64}{LESS_OP}{BAD_SYMBOLIC_OP}                     { yypushback(yylength()); yybegin(PRE_LESS_OP); }
  {DECIMAL}{LESS_OP}{BAD_SYMBOLIC_OP}                    { yypushback(yylength()); yybegin(PRE_LESS_OP); }
  {BYTE}{LESS_OP}{BAD_SYMBOLIC_OP}                       { yypushback(yylength()); yybegin(PRE_LESS_OP); }
  {INT16}{LESS_OP}{BAD_SYMBOLIC_OP}                      { yypushback(yylength()); yybegin(PRE_LESS_OP); }
  {INT32}{LESS_OP}{BAD_SYMBOLIC_OP}                      { yypushback(yylength()); yybegin(PRE_LESS_OP); }
  {INT64}{LESS_OP}{BAD_SYMBOLIC_OP}                      { yypushback(yylength()); yybegin(PRE_LESS_OP); }
  {SBYTE}{LESS_OP}{BAD_SYMBOLIC_OP}                      { yypushback(yylength()); yybegin(PRE_LESS_OP); }
  {UINT16}{LESS_OP}{BAD_SYMBOLIC_OP}                     { yypushback(yylength()); yybegin(PRE_LESS_OP); }
  {UINT32}{LESS_OP}{BAD_SYMBOLIC_OP}                     { yypushback(yylength()); yybegin(PRE_LESS_OP); }
  {UINT64}{LESS_OP}{BAD_SYMBOLIC_OP}                     { yypushback(yylength()); yybegin(PRE_LESS_OP); }
  {BIGNUM}{LESS_OP}{BAD_SYMBOLIC_OP}                     { yypushback(yylength()); yybegin(PRE_LESS_OP); }
  {NATIVEINT}{LESS_OP}{BAD_SYMBOLIC_OP}                  { yypushback(yylength()); yybegin(PRE_LESS_OP); }
}

<PRE_LESS_OP> {
  "delegate"                                             { yybegin(LINE); return makeToken(DELEGATE); }
  {IDENT}                                                { yybegin(LINE); return identInTypeApp(); }
  {IEEE32}                                               { yybegin(LINE); return makeToken(IEEE32); }
  {IEEE64}                                               { yybegin(LINE); return makeToken(IEEE64); }
  {DECIMAL}                                              { yybegin(LINE); return makeToken(DECIMAL); }
  {BYTE}                                                 { yybegin(LINE); return makeToken(BYTE); }
  {INT16}                                                { yybegin(LINE); return makeToken(INT16); }
  {INT32}                                                { yybegin(LINE); return makeToken(INT32); }
  {INT64}                                                { yybegin(LINE); return makeToken(INT64); }
  {SBYTE}                                                { yybegin(LINE); return makeToken(SBYTE); }
  {UINT16}                                               { yybegin(LINE); return makeToken(UINT16); }
  {UINT32}                                               { yybegin(LINE); return makeToken(UINT32); }
  {UINT64}                                               { yybegin(LINE); return makeToken(UINT64); }
  {BIGNUM}                                               { yybegin(LINE); return makeToken(BIGNUM); }
  {NATIVEINT}                                            { yybegin(LINE); return makeToken(NATIVEINT); }
}

<LINE, ADJACENT_TYAPP> {
  <INIT_ADJACENT_TYAPP> {
    {IDENT}                                              { return initIdent(); }
    {SBYTE}                                              { return makeToken(SBYTE); }
    {BYTE}                                               { return makeToken(BYTE); }
    {INT16}                                              { return makeToken(INT16); }
    {UINT16}                                             { return makeToken(UINT16); }
    {XINT}|{INT}                                         { return makeToken(INT32); }
    {INT32}                                              { return makeToken(INT32); }
    {UINT32}                                             { return makeToken(UINT32); }
    {NATIVEINT}                                          { return makeToken(NATIVEINT); }
    {UNATIVEINT}                                         { return makeToken(UNATIVEINT); }
    {INT64}                                              { return makeToken(INT64); }
    {UINT64}                                             { return makeToken(UINT64); }
// Rule for smah INT_DOT_DOT.
// https://github.com/Microsoft/visualfsharp/blob/173513e/src/fsharp/LexFilter.fs#L2142
    {INT}\.\.                                            { yypushback(yylength()); initSmash(SMASH_INT_DOT_DOT_FROM_LINE, SMASH_INT_DOT_DOT); }
    {IEEE32}                                             { return makeToken(IEEE32); }
    {IEEE64}                                             { return makeToken(IEEE64); }
    {BIGNUM}                                             { return makeToken(BIGNUM); }
    {DECIMAL}                                            { return makeToken(DECIMAL); }

    {CHARACTER_LITERAL}                                  { return makeToken(CHARACTER_LITERAL); }
    {STRING}                                             { return makeToken(STRING); }
    {UNFINISHED_STRING}                                  { return makeToken(UNFINISHED_STRING); }
    {VERBATIM_STRING}                                    { return makeToken(VERBATIM_STRING); }
    {UNFINISHED_VERBATIM_STRING}                         { return makeToken(UNFINISHED_VERBATIM_STRING); }
    {TRIPLE_QUOTED_STRING}                               { return makeToken(TRIPLE_QUOTED_STRING); }
    {UNFINISHED_TRIPLE_QUOTED_STRING}                    { return makeToken(UNFINISHED_TRIPLE_QUOTED_STRING); }
    {BYTEARRAY}                                          { return makeToken(BYTEARRAY); }
    {VERBATIM_BYTEARRAY}                                 { return makeToken(VERBATIM_BYTEARRAY); }
    {BYTECHAR}                                           { return makeToken(BYTECHAR); }
  }

  {RESERVED_SYMBOLIC_SEQUENCE}                           { return makeToken(RESERVED_SYMBOLIC_SEQUENCE); }
  {RESERVED_LITERAL_FORMATS}                             { return makeToken(RESERVED_LITERAL_FORMATS); }
  {SYMBOLIC_OP}                                          { return makeToken(SYMBOLIC_OP); }
  {BAD_SYMBOLIC_OP}                                      { return makeToken(BAD_SYMBOLIC_OP); }
}

<SMASH_INT_DOT_DOT,
 SMASH_INT_DOT_DOT_FROM_LINE> {
  {INT}                                                  { return makeToken(INT32); }
  \.\.                                                   { exitSmash(SMASH_INT_DOT_DOT_FROM_LINE); return makeToken(DOT_DOT); }
}

<SMASH_RQUOTE_DOT,
 SMASH_RQUOTE_DOT_FROM_LINE> {
  "@>"                                                   |
  "@@>"                                                  { return makeToken(QUOTE_OP_RIGHT); }
  \.                                                     { exitSmash(SMASH_RQUOTE_DOT_FROM_LINE); return makeToken(DOT); }
}

<STRING_IN_COMMENT> {
  {STRING}                                               |
  {VERBATIM_STRING}                                      |
  {TRIPLE_QUOTED_STRING}                                 { yybegin(IN_BLOCK_COMMENT); }
  {UNFINISHED_STRING}                                    { return fillBlockComment(UNFINISHED_STRING_IN_COMMENT); }
  {UNFINISHED_VERBATIM_STRING}                           { return fillBlockComment(UNFINISHED_VERBATIM_STRING_IN_COMMENT); }
  {UNFINISHED_TRIPLE_QUOTED_STRING}                      { return fillBlockComment(UNFINISHED_TRIPLE_QUOTED_STRING_IN_COMMENT); }
}

<IN_BLOCK_COMMENT,
 IN_BLOCK_COMMENT_FROM_LINE> {
  "(*"                                                   { zzNestedCommentLevel++; }
  "*)"                                                   { if (--zzNestedCommentLevel == 0) return fillBlockComment(BLOCK_COMMENT); }
  \"                                                     |
  @\"                                                    |
  \"\"\"                                                 { yypushback(yylength()); yybegin(STRING_IN_COMMENT); }
  [^(\"@*]+                                              |
  [^]|"(*)"                                              { }
}

<SMASH_ADJACENT_LESS_OP> {
  {LESS}                                                 { return makeToken(LESS); }
  "/"                                                    { riseFromParenLevel(0); return makeToken(SYMBOLIC_OP); }
}

<SMASH_ADJACENT_GREATER_BAR_RBRACK,
 SMASH_ADJACENT_GREATER_BAR_RBRACK_FIN> {
  {GREATER}                                              { return makeToken(GREATER); }
  {BAR_RBRACK}                                           { exitSmash(SMASH_ADJACENT_GREATER_BAR_RBRACK_FIN); return makeToken(RBRACK); }
}

<SMASH_ADJACENT_GREATER_RBRACK,
 SMASH_ADJACENT_GREATER_RBRACK_FIN> {
  {GREATER}                                              { return makeToken(GREATER); }
  {RBRACK}                                               { exitSmash(SMASH_ADJACENT_GREATER_RBRACK_FIN); return makeToken(RBRACK); }
}

// No need rule for ADJACENT_PREFIX rule.
// https://github.com/Microsoft/visualfsharp/blob/master/src/fsharp/LexFilter.fs#L2157

<PPSHARP, PPSYMBOL, PPDIRECTIVE> {WHITE_SPACE}           { return makeToken (WHITE_SPACE); }

<PPDIRECTIVE> {
  {HASH}("l"|"load")                                     { yybegin(LINE); return makeToken(PP_LOAD); }
  {HASH}("r"|"reference")                                { yybegin(LINE); return makeToken(PP_REFERENCE); }
  {HASH}("line"|{ANYWHITE}*[0-9]+)                       { yybegin(LINE); return makeToken(PP_LINE); }
  {HASH}"help"                                           { yybegin(LINE); return makeToken(PP_HELP); }
  {HASH}"quit"                                           { yybegin(LINE); return makeToken(PP_QUIT); }
  {HASH}("light"|"indent")                               { yybegin(LINE); return makeToken(PP_LIGHT); }
  {HASH}"time"                                           { yybegin(LINE); return makeToken(PP_TIME); }
  {HASH}"I"                                              { yybegin(LINE); return makeToken(PP_I); }
  {HASH}"nowarn"                                         { yybegin(LINE); return makeToken(PP_NOWARN); }
  {HASH}"if"{IDENT}                                      { yypushback(yylength()); yybegin(PPSHARP); }
  {HASH}"else"{IDENT}                                    { yypushback(yylength()); yybegin(PPSHARP); }
  {HASH}"endif"{IDENT}                                   { yypushback(yylength()); yybegin(PPSHARP); }
  {HASH}{IDENT}                                          { yybegin(LINE); return makeToken(PP_DIRECTIVE); }
}

<PPSHARP> {
  {HASH}"if"                                             { yybegin(PPSYMBOL); return makeToken(PP_IF_SECTION); }
  {HASH}"else"                                           { yybegin(PPSYMBOL); return makeToken(PP_ELSE_SECTION); }
  {HASH}"endif"                                          { yybegin(PPSYMBOL); return makeToken(PP_ENDIF); }
}

<PPSYMBOL> {
  "||"                                                   { return makeToken(PP_OR); }
  "&&"                                                   { return makeToken(PP_AND); }
  "!"                                                    { return makeToken(PP_NOT); }
  "("                                                    { return makeToken(PP_LPAR); }
  ")"                                                    { return makeToken(PP_RPAR); }
  {PP_CONDITIONAL_SYMBOL}                                { return makeToken(PP_CONDITIONAL_SYMBOL); }
}

<PPSHARP, PPSYMBOL> {
  {END_OF_LINE_COMMENT}                                  { return makeToken(END_OF_LINE_COMMENT); }
}

<PPSHARP, PPSYMBOL> {NEWLINE}                            { yybegin(YYINITIAL); return makeToken(NEWLINE); }
<PPSYMBOL> .                                             { return makeToken(PP_BAD_CHARACTER); }

[^]                                                      { return makeToken(BAD_CHARACTER); }
