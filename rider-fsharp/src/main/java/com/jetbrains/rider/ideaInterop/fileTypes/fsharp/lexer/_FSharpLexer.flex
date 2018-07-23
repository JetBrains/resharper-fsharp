package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.lexer;

import com.intellij.psi.tree.IElementType;
import com.intellij.lexer.*;

import static com.intellij.psi.TokenType.BAD_CHARACTER;
import static com.intellij.psi.TokenType.WHITE_SPACE;
import static com.jetbrains.rider.ideaInterop.fileTypes.fsharp.FSharpElementTypes.*;

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
%}

%{
    // for sharing rules with ReSharper
    private IElementType makeToken(IElementType type) {
        return type;
    }

    private void initBlockComment() {
        yybegin(IN_BLOCK_COMMENT);
        zzNestedCommentLevel++;
    }

    private IElementType fillBlockComment(IElementType tokenType) {
        yybegin(YYINITIAL);
        zzNestedCommentLevel = 0;
        return makeToken(tokenType);
    }
%}

%state IN_BLOCK_COMMENT
%state STRING_IN_COMMENT
%state VERBATIM_STRING_IN_COMMENT
%state TRIPLE_QUOTED_STRING_IN_COMMENT

WHITE_SPACE=" "+

NEWLINE=\n|\r\n
END_OF_LINE_COMMENT=\/\/[^\n\r]*
IF_DERECTIVE="#if"{WHITE_SPACE}+{IDENT_TEXT}
LETTER_CHAR=\p{Lu}|\p{Ll}|\p{Lt}|\p{Lm}|\p{Lo}|\p{Nl}
DIGIT=\p{Nd}
IDENT_START_CHAR={LETTER_CHAR}|_
CONNECTING_CHAR=\p{Pc}
COMBINING_CHAR=\p{Mn}|\p{Mc}
FORMATTING_CHAR=\p{Cf}
IDENT_CHAR={LETTER_CHAR}|{CONNECTING_CHAR}|{COMBINING_CHAR}|{FORMATTING_CHAR}|{DIGIT}|['_]
IDENT_TEXT={IDENT_START_CHAR}({IDENT_CHAR}*)
IDENT={IDENT_TEXT}|``([^`\n\r\t]|`[^`\n\r\t])+``
IDENT_KEYWORD="abstract"|"and"|"as"|"assert"|"base"|"begin"|"class"|"default"|"delegate"|"do"|"done"|
    "downcast"|"downto"|"elif"|"else"|"end"|"exception"|"extern"|"false"|"finally"|"for"|
    "fun"|"function"|"global"|"if"|"in"|"inherit"|"inline"|"interface"|"internal"|"lazy"|"let"|
    "match"|"member"|"module"|"mutable"|"namespace"|"new"|"null"|"of"|"open"|"or"|
    "override"|"private"|"public"|"rec"|"return"|"sig"|"static"|"struct"|"then"|"to"|
    "true"|"try"|"type"|"upcast"|"use"|"val"|"void"|"when"|"while"|"with"|"yield"
RESERVED_IDENT_KEYWORD=
    "atomic"|"break"|"checked"|"component"|"const"|"constraint"|"constructor"|
    "continue"|"eager"|"fixed"|"fori"|"functor"|"include"|
    "measure"|"method"|"mixin"|"object"|"parallel"|"params"|"process"|"protected"|"pure"|
    "recursive"|"sealed"|"tailcall"|"trait"|"virtual"|"volatile"
RESERVED_IDENT_FORMATS={IDENT_TEXT}([!#])
ESCAPE_CHAR=[\\][\\\"\'afvntbr]
NON_ESCAPE_CHARS=[\\][^ntbr\\\"\']
SIMPLE_CHAR_CHAR=[^\t\b\n\r\\\"\']
SIMPLE_STRING_CHAR={SIMPLE_CHAR_CHAR}|[\'\\]|{NEWLINE}
SIMPLE_CHAR={SIMPLE_STRING_CHAR}|\"
UNICODEGRAPH_SHORT=\\u{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}
UNICODEGRAPH_LONG =\\U{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}
                      {HEXDIGIT}{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}
TRIGRAPH=\\{DIGIT}{DIGIT}{DIGIT}
CHAR_CHAR ={SIMPLE_CHAR_CHAR}|{ESCAPE_CHAR}|{TRIGRAPH}|{UNICODEGRAPH_SHORT}
STRING_CHAR=
    {SIMPLE_STRING_CHAR}|{ESCAPE_CHAR}|{NON_ESCAPE_CHARS}|
    {TRIGRAPH}|{UNICODEGRAPH_SHORT}|{UNICODEGRAPH_LONG}
CHAR='{CHAR_CHAR}'
UNFINISHED_STRING=\"{STRING_CHAR}*
STRING={UNFINISHED_STRING}\"
VERBATIM_STRING_CHAR={SIMPLE_STRING_CHAR}|{NON_ESCAPE_CHARS}|{NEWLINE}|\\|\"\"
UNFINISHED_VERBATIM_STRING=@\"{VERBATIM_STRING_CHAR}*
VERBATIM_STRING={UNFINISHED_VERBATIM_STRING}\"
BYTECHAR='{SIMPLE_OR_ESCAPE_CHAR}'B
BYTEARRAY=\"{STRING_CHAR}*\"B
VERBATIM_BYTEARRAY=@\"{VERBATIM_STRING_CHAR}*\"B
SIMPLE_OR_ESCAPE_CHAR={ESCAPE_CHAR}|{SIMPLE_CHAR}
UNFINISHED_TRIPLE_QUOTED_STRING=\"\"\"{SIMPLE_OR_ESCAPE_CHAR}*
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
LBRACK="["
RBRACK="]"
LBRACK_LESS="[<"
GREATER_RBRACK=">]"
LBRACK_BAR="[|"
BAR_RBRACK="|]"
LBRACE="{"
RBRACE="}"
QUOTE="'"
HASH="#"
COLON_QMARK_GREATER=":?>"
COLON_QMARK=":?"
COLON_GREATER=":>"
DOT_DOT=".."
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

RESERVED_SYMBOLIC_SEQUENCE=[~']

FIRST_OP_CHAR=[!%&*+-./<=>@\^|~]
OP_CHAR={FIRST_OP_CHAR}|[?]
QUOTE_OP_LEFT="<@"|"<@@"
QUOTE_OP_RIGHT="@>"|"@@>"
SYMBOLIC_OP=[?]|"?<-"|{FIRST_OP_CHAR}{OP_CHAR}*|{QUOTE_OP_LEFT}|{QUOTE_OP_RIGHT}

HEXDIGIT={DIGIT} | [A-F]| [a-f]
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
<YYINITIAL> {

  {WHITE_SPACE}                         { return makeToken(WHITE_SPACE); }
  {NEWLINE}                             { return makeToken(NEWLINE); }

  "(*"                                  { initBlockComment(); }
  {END_OF_LINE_COMMENT}                 { return makeToken(END_OF_LINE_COMMENT); }

  {IF_DERECTIVE}                        { return makeToken(IF_DERECTIVE); }
  "#else"                               { return makeToken(ELSE_DERECTIVE); }
  "#endif"                              { return makeToken(ENDIF_DERECTIVE); }

  {QUOTE_OP_LEFT}                       { return makeToken(QUOTE_OP_LEFT); }
  {QUOTE_OP_RIGHT}                      { return makeToken(QUOTE_OP_RIGHT); }
  {LET_BANG}                            { return makeToken(LET_BANG); }
  {USE_BANG}                            { return makeToken(USE_BANG); }
  {DO_BANG }                            { return makeToken(DO_BANG); }
  {YIELD_BANG}                          { return makeToken(YIELD_BANG); }
  {RETURN_BANG}                         { return makeToken(RETURN_BANG); }
  {BAR}                                 { return makeToken(BAR); }
  {RARROW}                              { return makeToken(RARROW); }
  {LARROW}                              { return makeToken(LARROW); }
  {DOT}                                 { return makeToken(DOT); }
  {COLON}                               { return makeToken(COLON); }
  {LPAREN}                              { return makeToken(LPAREN); }
  {RPAREN}                              { return makeToken(RPAREN); }
  {LBRACK}                              { return makeToken(LBRACK); }
  {RBRACK}                              { return makeToken(RBRACK); }
  {LBRACK_LESS}                         { return makeToken(LBRACK_LESS); }
  {GREATER_RBRACK}                      { return makeToken(GREATER_RBRACK); }
  {LBRACK_BAR}                          { return makeToken(LBRACK_BAR); }
  {BAR_RBRACK}                          { return makeToken(BAR_RBRACK); }
  {LBRACE}                              { return makeToken(LBRACE); }
  {RBRACE}                              { return makeToken(RBRACE); }
  {QUOTE}                               { return makeToken(QUOTE); }
  {HASH}                                { return makeToken(HASH); }
  {COLON_QMARK_GREATER}                 { return makeToken(COLON_QMARK_GREATER); }
  {COLON_QMARK}                         { return makeToken(COLON_QMARK); }
  {COLON_GREATER}                       { return makeToken(COLON_GREATER); }
  {DOT_DOT}                             { return makeToken(DOT_DOT); }
  {COLON_COLON}                         { return makeToken(COLON_COLON); }
  {COLON_EQUALS}                        { return makeToken(COLON_EQUALS); }
  {SEMICOLON_SEMICOLON}                 { return makeToken(SEMICOLON_SEMICOLON); }
  {SEMICOLON}                           { return makeToken(SEMICOLON); }
  {EQUALS}                              { return makeToken(EQUALS); }
  {UNDERSCORE}                          { return makeToken(UNDERSCORE); }
  {QMARK}                               { return makeToken(QMARK); }
  {QMARK_QMARK}                         { return makeToken(QMARK_QMARK); }
  {LPAREN_STAR_RPAREN}                  { return makeToken(LPAREN_STAR_RPAREN); }
  {MINUS}                               { return makeToken(MINUS); }
  {PLUS}                                { return makeToken(PLUS); }

  {KEYWORD_STRING_SOURCE_DIRECTORY}     { return makeToken(KEYWORD_STRING_SOURCE_DIRECTORY); }
  {KEYWORD_STRING_SOURCE_FILE}          { return makeToken(KEYWORD_STRING_SOURCE_FILE); }
  {KEYWORD_STRING_LINE}                 { return makeToken(KEYWORD_STRING_LINE); }

  {RESERVED_IDENT_KEYWORD}              { return makeToken(RESERVED_IDENT_KEYWORD); }
  {IDENT_KEYWORD}                       { return makeToken(IDENT_KEYWORD); }
  {RESERVED_IDENT_FORMATS}              { return makeToken(RESERVED_IDENT_FORMATS); }
  {IDENT}                               { return makeToken(IDENT); }

  {SBYTE}                               { return makeToken(SBYTE); }
  {BYTE}                                { return makeToken(BYTE); }
  {INT16}                               { return makeToken(INT16); }
  {UINT16}                              { return makeToken(UINT16); }
  {INT32}                               { return makeToken(INT32); }
  {UINT32}                              { return makeToken(UINT32); }
  {NATIVEINT}                           { return makeToken(NATIVEINT); }
  {UNATIVEINT}                          { return makeToken(UNATIVEINT); }
  {INT64}                               { return makeToken(INT64); }
  {UINT64}                              { return makeToken(UINT64); }
  {FLOAT}                               { return makeToken(FLOAT); }
  {IEEE32}                              { return makeToken(IEEE32); }
  {IEEE64}                              { return makeToken(IEEE64); }
  {BIGNUM}                              { return makeToken(BIGNUM); }
  {DECIMAL}                             { return makeToken(DECIMAL); }
  {INT}                                 { return makeToken(INT32); }

  {CHAR}                                { return makeToken(CHAR); }
  {STRING}                              { return makeToken(STRING); }
  {UNFINISHED_STRING}                   { return makeToken(UNFINISHED_STRING); }
  {VERBATIM_STRING}                     { return makeToken(VERBATIM_STRING); }
  {UNFINISHED_VERBATIM_STRING}          { return makeToken(UNFINISHED_VERBATIM_STRING); }
  {TRIPLE_QUOTED_STRING}                { return makeToken(TRIPLE_QUOTED_STRING); }
  {UNFINISHED_TRIPLE_QUOTED_STRING}     { return makeToken(UNFINISHED_TRIPLE_QUOTED_STRING); }
  {BYTECHAR}                            { return makeToken(BYTECHAR); }
  {BYTEARRAY}                           { return makeToken(BYTEARRAY); }
  {VERBATIM_BYTEARRAY}                  { return makeToken(VERBATIM_BYTEARRAY); }

  {RESERVED_SYMBOLIC_SEQUENCE}          { return makeToken(RESERVED_SYMBOLIC_SEQUENCE); }
  {SYMBOLIC_OP}                         { return makeToken(SYMBOLIC_OP); }
  {INTDOTDOT}                           { return makeToken(INTDOTDOT); }
  {RESERVED_LITERAL_FORMATS}            { return makeToken(RESERVED_LITERAL_FORMATS); }
  {LINE_DIRECTIVE}                      { return makeToken(LINE_DIRECTIVE); }
}

<STRING_IN_COMMENT> {
  {STRING}                              { yybegin(IN_BLOCK_COMMENT); }
  {UNFINISHED_STRING}                   { return fillBlockComment(UNFINISHED_STRING_IN_COMMENT); }
}

<VERBATIM_STRING_IN_COMMENT> {
  {VERBATIM_STRING}                     { yybegin(IN_BLOCK_COMMENT); }
  {UNFINISHED_VERBATIM_STRING}          { return fillBlockComment(UNFINISHED_VERBATIM_STRING_IN_COMMENT); }
}

<TRIPLE_QUOTED_STRING_IN_COMMENT> {
  {TRIPLE_QUOTED_STRING}                { yybegin(IN_BLOCK_COMMENT); }
  {UNFINISHED_TRIPLE_QUOTED_STRING}     { return fillBlockComment(UNFINISHED_TRIPLE_QUOTED_STRING_IN_COMMENT); }
}

<IN_BLOCK_COMMENT> {
  "(*"                                  { zzNestedCommentLevel++; }
  "*)"                                  { if (--zzNestedCommentLevel == 0) return fillBlockComment(BLOCK_COMMENT); }
  "(*)"                                 { }
  \"                                    { yypushback(1); yybegin(STRING_IN_COMMENT); }
  @\"                                   { yypushback(2); yybegin(VERBATIM_STRING_IN_COMMENT); }
  \"\"\"                                { yypushback(3); yybegin(TRIPLE_QUOTED_STRING_IN_COMMENT); }
  {STRING_CHAR}                         { }
}

[^] { return makeToken(BAD_CHARACTER); }
