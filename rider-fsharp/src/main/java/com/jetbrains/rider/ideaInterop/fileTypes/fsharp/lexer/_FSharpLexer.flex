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
  private int zzSavedState = 0;
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
    saveState();
    yybegin(IN_BLOCK_COMMENT);
    zzNestedCommentLevel++;
  }

  private IElementType initIdent() {
    IElementType keyword = FindKeywordByCurrentToken();
    return makeToken(keyword != null ? keyword : IDENT);
  }

  private IElementType initIdentInTypeApp() {
    IElementType keyword = FindKeywordByCurrentToken();
    if (keyword != null) {
        toSavedStateAndErase();
        return makeToken(keyword);
    }
    return makeToken(IDENT);
  }

  private IElementType fillBlockComment(IElementType tokenType) {
    toSavedStateAndErase();
    zzNestedCommentLevel = 0;
    return makeToken(tokenType);
  }

  private void initSmashAdjacent(int state, int finalState) {
    if (--zzParenLevel <= 0) {
      yybegin(finalState);
    }
    else {
      saveState();
      yybegin(state);
    }
  }

  private IElementType exitSmashAdjacent(int state) {
    if (yystate() == state) {
      toSavedStateAndErase();
    }
    else {
      yybegin(YYINITIAL);
    }
    return makeToken(RBRACK);
  }

  private void saveState() {
    zzSavedState = yystate();
  }
  private void toSavedStateAndErase()
  {
    yybegin(zzSavedState);
    zzSavedState = YYINITIAL;
  }

  private void deepIntoParenLevel() {
    if (++zzParenLevel > 1 && yystate() == INIT_ADJACENT_TYAPP)
      yybegin(ADJACENT_TYAPP);
  }

  private void riseFromParenLevel(int n) {
    zzParenLevel -= n;
    if (zzParenLevel <= 1)
      yybegin(INIT_ADJACENT_TYAPP);
    if (zzParenLevel <= 0)
      yybegin(YYINITIAL);
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
    if (zzParenLevel != 0) {
      yybegin(SYMBOLIC_OPERATOR);
    }
    else {
      yybegin(GREATER_OPERATOR);
    }
  }
%}

%state IN_BLOCK_COMMENT
%state STRING_IN_COMMENT
%state VERBATIM_STRING_IN_COMMENT
%state TRIPLE_QUOTED_STRING_IN_COMMENT
%state INT_DOT_DOT
%state RQUOTE_DOT
%state CHAR_LITERAL
%state SMASH_ADJACENT_LESS_OP
%state SMASH_ADJACENT_GREATER_BAR_RBRACK
%state SMASH_ADJACENT_GREATER_RBRACK
%state SMASH_ADJACENT_GREATER_BAR_RBRACK_FIN
%state SMASH_ADJACENT_GREATER_RBRACK_FIN
%state SMASH_ADJACENT_INFIX_COMPARE_OP
%state ADJACENT_TYPE_CLOSE_OP
%state INIT_ADJACENT_TYAPP
%state ADJACENT_TYAPP
%state SYMBOLIC_OPERATOR
%state GREATER_OPERATOR

WHITE_SPACE=" "+

NEWLINE=\n|\r\n
END_OF_LINE_COMMENT=\/\/[^\n\r]*
IF_DIRECTIVE="#if"{WHITE_SPACE}+{IDENT_TEXT}
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
SIMPLE_CHAR_CHAR=[^\t\b\n\r\\]
SIMPLE_STRING_CHAR=[^\"]
SIMPLE_CHAR={SIMPLE_STRING_CHAR}|\"
UNICODEGRAPH_SHORT=\\u{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}
UNICODEGRAPH_LONG =\\U{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}
                      {HEXDIGIT}{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}
TRIGRAPH=\\{DIGIT}{DIGIT}{DIGIT}
CHAR_CHAR ={SIMPLE_CHAR_CHAR}|{ESCAPE_CHAR}|{TRIGRAPH}|{UNICODEGRAPH_SHORT}|\"
STRING_CHAR=
    {SIMPLE_STRING_CHAR}|{ESCAPE_CHAR}|{NON_ESCAPE_CHARS}|
    {TRIGRAPH}|{UNICODEGRAPH_SHORT}|{UNICODEGRAPH_LONG}|{NEWLINE}
CHAR='{CHAR_CHAR}'
UNFINISHED_STRING=\"{STRING_CHAR}*
STRING={UNFINISHED_STRING}\"
VERBATIM_STRING_CHAR={SIMPLE_STRING_CHAR}|{NON_ESCAPE_CHARS}|\"\"
UNFINISHED_VERBATIM_STRING=@\"{VERBATIM_STRING_CHAR}*
VERBATIM_STRING={UNFINISHED_VERBATIM_STRING}\"
BYTECHAR='{SIMPLE_OR_ESCAPE_CHAR}'B
BYTEARRAY=\"{STRING_CHAR}*\"B
VERBATIM_BYTEARRAY=@\"{VERBATIM_STRING_CHAR}*\"B
SIMPLE_OR_ESCAPE_CHAR={ESCAPE_CHAR}|{SIMPLE_CHAR}
UNFINISHED_TRIPLE_QUOTED_STRING=\"\"\"((\"?){STRING_CHAR}|\"\"{STRING_CHAR})*
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
INTDOTDOT={INT}\.\.
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
QUOTE_OP_RIGHT_DOT="@>."|"@@>."
SYMBOLIC_OP=[?]|"?<-"|{OP_CHAR}+|{QUOTE_OP_LEFT}|{QUOTE_OP_RIGHT}
BAD_SYMBOLIC_OP=[?]|"?<-"|{BAD_OP_CHAR}+|{QUOTE_OP_LEFT}|{QUOTE_OP_RIGHT}

INFIX_OR_PREFIX_OP="+"|"-"|"+."|"-."|"%"|"&"|"&&"
LESS_OP="</"|{LESS}
INFIX_COMPARE_OP=([.?])*("="|"!="|"<"|">")(OP_CHAR)*

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
INTDOTDOT={INT}\.\.
RESERVED_LITERAL_FORMATS=({XINT}|{IEEE32}|{IEEE64}){IDENT_CHAR}+
LINE_DIRECTIVE=#{INT}|#{INT}{STRING}|#{INT}{VERBATIM_STRING}|#line{INT}|#line{INT}{STRING}|#line{INT}{VERBATIM_STRING}

KEYWORD_STRING_SOURCE_DIRECTORY="__SOURCE_DIRECTORY__"
KEYWORD_STRING_SOURCE_FILE="__SOURCE_FILE__"
KEYWORD_STRING_LINE="__LINE__"

%%
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> {
  {LESS}                                                 { deepIntoParenLevel(); return makeToken(LESS); }
  {LPAREN}                                               { deepIntoParenLevel(); return makeToken(LPAREN); }
  {LBRACK}                                               { deepIntoParenLevel(); return makeToken(LBRACK); }
  {LBRACK_LESS}                                          { deepIntoParenLevel(); return makeToken(LBRACK_LESS); }
  {GREATER}                                              { riseFromParenLevel(1); return makeToken(GREATER); }
  "</"                                                   { deepIntoParenLevel(); yypushback(2); saveState(); yybegin(SMASH_ADJACENT_LESS_OP);}
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
  {IDENT}                                                { return initIdentInTypeApp(); }
}

<ADJACENT_TYPE_CLOSE_OP> {
  {GREATER}+                                             { adjacentTypeCloseOp(); }
}

<GREATER_OPERATOR> {
  {GREATER}                                              { return makeToken(GREATER); }
  [^]                                                    { yypushback(1); riseFromParenLevel(0);}
}

<SYMBOLIC_OPERATOR> {
  {QUOTE_OP_LEFT}                                        { riseFromParenLevel(0); return makeToken(QUOTE_OP_LEFT); }
  {QUOTE_OP_RIGHT}                                       { riseFromParenLevel(0); return makeToken(QUOTE_OP_RIGHT); }
  {QUOTE_OP_RIGHT_DOT}                                   { riseFromParenLevel(0); yypushback(yylength()); yybegin(RQUOTE_DOT); }
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
  {HASH}                                                 { riseFromParenLevel(0); return makeToken(HASH); }
  {COLON_GREATER}                                        { riseFromParenLevel(0); return makeToken(COLON_GREATER); }
  {DOT_DOT}                                              { riseFromParenLevel(0); return makeToken(DOT_DOT); }
  {EQUALS}                                               { riseFromParenLevel(0); return makeToken(EQUALS); }
  {UNDERSCORE}                                           { riseFromParenLevel(0); return makeToken(UNDERSCORE); }
  {MINUS}                                                { riseFromParenLevel(0); return makeToken(MINUS); }
  {COMMA}                                                { riseFromParenLevel(0); return makeToken(COMMA); }
  {SYMBOLIC_OP}                                          { riseFromParenLevel(0); return makeToken(SYMBOLIC_OP); }
  {BAD_SYMBOLIC_OP}                                      { riseFromParenLevel(0); return makeToken(BAD_SYMBOLIC_OP); }
}

<YYINITIAL, ADJACENT_TYAPP> {
  <INIT_ADJACENT_TYAPP> {
    {WHITE_SPACE}                                        { return makeToken(WHITE_SPACE); }
    {NEWLINE}                                            { return makeToken(NEWLINE); }
  }

  "(*"                                                   { initBlockComment(); }
  {END_OF_LINE_COMMENT}                                  { return makeToken(END_OF_LINE_COMMENT); }

  {IF_DIRECTIVE}                                         { return makeToken(IF_DIRECTIVE); }
  "#else"                                                { return makeToken(ELSE_DIRECTIVE); }
  "#endif"                                               { return makeToken(ENDIF_DIRECTIVE); }

  {QUOTE_OP_LEFT}                                        { return makeToken(QUOTE_OP_LEFT); }
  {QUOTE_OP_RIGHT}                                       { return makeToken(QUOTE_OP_RIGHT); }
  {QUOTE_OP_RIGHT_DOT}                                   { saveState(); yypushback(yylength()); yybegin(RQUOTE_DOT); }
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

<YYINITIAL> {
  "delegate"{LESS_OP}                                    { initAdjacentTypeApp(); return makeToken(DELEGATE); }
  {IDENT}{LESS_OP}                                       { initAdjacentTypeApp(); return initIdentInTypeApp(); }
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
}

<YYINITIAL, ADJACENT_TYAPP> {
  <INIT_ADJACENT_TYAPP> {
    {IDENT}                                              { return initIdent(); }
    {SBYTE}                                              { return makeToken(SBYTE); }
    {BYTE}                                               { return makeToken(BYTE); }
    {INT16}                                              { return makeToken(INT16); }
    {UINT16}                                             { return makeToken(UINT16); }
    {INTDOTDOT}                                          { saveState(); yypushback(yylength()); yybegin(INT_DOT_DOT); }
    {XINT}|{INT}                                         { return makeToken(INT32); }
    {INT32}                                              { return makeToken(INT32); }
    {UINT32}                                             { return makeToken(UINT32); }
    {NATIVEINT}                                          { return makeToken(NATIVEINT); }
    {UNATIVEINT}                                         { return makeToken(UNATIVEINT); }
    {INT64}                                              { return makeToken(INT64); }
    {UINT64}                                             { return makeToken(UINT64); }
    {IEEE32}                                             { return makeToken(IEEE32); }
    {IEEE64}                                             { return makeToken(IEEE64); }
    {BIGNUM}                                             { return makeToken(BIGNUM); }
    {DECIMAL}                                            { return makeToken(DECIMAL); }

    {CHAR}                                               { return makeToken(CHAR); }
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
  {LINE_DIRECTIVE}                                       { return makeToken(LINE_DIRECTIVE); }
}

<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> {
  [^]                                                    { zzParenLevel = 0; yypushback(1); yybegin(YYINITIAL); }
}

<INT_DOT_DOT> {
  {INT}                                                  { return makeToken(INT32); }
  \.\.                                                   { toSavedStateAndErase(); return makeToken(DOT_DOT); }
}

<RQUOTE_DOT> {
    "@>"                                                 |
    "@@>"                                                { return makeToken(QUOTE_OP_RIGHT); }
    \.                                                   { toSavedStateAndErase(); return makeToken(DOT); }
}

<STRING_IN_COMMENT> {
  {STRING}                                               { yybegin(IN_BLOCK_COMMENT); }
  {UNFINISHED_STRING}                                    { return fillBlockComment(UNFINISHED_STRING_IN_COMMENT); }
}

<VERBATIM_STRING_IN_COMMENT> {
  {VERBATIM_STRING}                                      { yybegin(IN_BLOCK_COMMENT); }
  {UNFINISHED_VERBATIM_STRING}                           { return fillBlockComment(UNFINISHED_VERBATIM_STRING_IN_COMMENT); }
}

<TRIPLE_QUOTED_STRING_IN_COMMENT> {
  {TRIPLE_QUOTED_STRING}                                 { yybegin(IN_BLOCK_COMMENT); }
  {UNFINISHED_TRIPLE_QUOTED_STRING}                      { return fillBlockComment(UNFINISHED_TRIPLE_QUOTED_STRING_IN_COMMENT); }
}

<IN_BLOCK_COMMENT> {
  "(*"                                                   { zzNestedCommentLevel++; }
  "*)"                                                   { if (--zzNestedCommentLevel == 0) return fillBlockComment(BLOCK_COMMENT); }
  "(*)"                                                  { }
  \"                                                     { yypushback(1); yybegin(STRING_IN_COMMENT); }
  @\"                                                    { yypushback(2); yybegin(VERBATIM_STRING_IN_COMMENT); }
  \"\"\"                                                 { yypushback(3); yybegin(TRIPLE_QUOTED_STRING_IN_COMMENT); }
  {STRING_CHAR}                                          { }
}

<SMASH_ADJACENT_LESS_OP> {
  {LESS}                                                 { return makeToken(LESS); }
  "/"                                                    { toSavedStateAndErase(); return makeToken(SYMBOLIC_OP); }
}

<SMASH_ADJACENT_GREATER_BAR_RBRACK,
 SMASH_ADJACENT_GREATER_BAR_RBRACK_FIN> {
  {GREATER}                                              { return makeToken(GREATER); }
  {BAR_RBRACK}                                           { return exitSmashAdjacent(SMASH_ADJACENT_GREATER_BAR_RBRACK); }
}

<SMASH_ADJACENT_GREATER_RBRACK,
 SMASH_ADJACENT_GREATER_RBRACK_FIN> {
  {GREATER}                                              { return makeToken(GREATER); }
  {RBRACK}                                               { return exitSmashAdjacent(SMASH_ADJACENT_GREATER_RBRACK); }
}

[^]                                                      { return makeToken(BAD_CHARACTER); }
