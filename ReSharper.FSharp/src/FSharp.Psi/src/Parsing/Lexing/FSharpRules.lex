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
%state BAD_PPSHARP
%state PPSYMBOL
%state PPDIRECTIVE

WHITESPACE=((" ")+)
TAB=((\t)+)
ANYWHITE=({WHITESPACE}|{TAB})

NEW_LINE=(\n|\r\n)
LINE_COMMENT=(\/\/([^\n\r])*)
SHEBANG=("#!"([^\n\r])*)
LETTER_CHAR=({UNICODE_LU}|{UNICODE_LL}|{UNICODE_LT}|{UNICODE_LM}|{UNICODE_LO}|{UNICODE_NL})
DIGIT=({UNICODE_ND})
IDENT_START_CHAR=({LETTER_CHAR}|"_")
CONNECTING_CHAR=({UNICODE_PC})
COMBINING_CHAR=({UNICODE_MN}|{UNICODE_MC})
FORMATTING_CHAR=({UNICODE_CF})
IDENT_CHAR=({LETTER_CHAR}|{CONNECTING_CHAR}|{COMBINING_CHAR}|{FORMATTING_CHAR}|{DIGIT}|['_])
TAIL_IDENT=(({IDENT_CHAR})+)
IDENT_TEXT=(({IDENT_START_CHAR})({TAIL_IDENT})?)
IDENT=({IDENT_TEXT}|("``"(([^`\n\r\t]|("`"[^`\n\r\t]))+)"``"))
RESERVED_IDENT_FORMATS=({IDENT_TEXT}([!#]))
ESCAPE_CHAR=[\\][\\\"\'afvntbr]
NON_ESCAPE_CHARS=[\\][^afvntbr\\\"\']
SIMPLE_CHARACTER=[^\t\b\n\r\\\'\"]
SIMPLE_STRING_CHAR=[^\"\\]
HEXGRAPH_SHORT=\\x{HEXDIGIT}{HEXDIGIT}
UNICODEGRAPH_SHORT=\\u{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}
UNICODEGRAPH_LONG=\\U{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}{HEXDIGIT}
TRIGRAPH=\\{DIGIT}{DIGIT}{DIGIT}
CHARACTER=({SIMPLE_CHARACTER}|{ESCAPE_CHAR}|{TRIGRAPH}|{UNICODEGRAPH_SHORT}|{HEXGRAPH_SHORT}|{UNICODEGRAPH_LONG}|\"|\')
STRING_CHAR=({SIMPLE_STRING_CHAR}|{ESCAPE_CHAR}|{NON_ESCAPE_CHARS}|{TRIGRAPH}|{UNICODEGRAPH_SHORT}|{UNICODEGRAPH_LONG}|\')
CHARACTER_LITERAL=(\'{CHARACTER}\')
UNFINISHED_STRING=(\"({STRING_CHAR})*)
STRING=({UNFINISHED_STRING}\")
VERBATIM_STRING_CHAR=({SIMPLE_STRING_CHAR}|{NON_ESCAPE_CHARS}|\"\"|\\)
UNFINISHED_VERBATIM_STRING=(@\"({VERBATIM_STRING_CHAR})*)
VERBATIM_STRING=({UNFINISHED_VERBATIM_STRING}\")
BYTECHAR=(\'({SIMPLE_OR_ESCAPE_CHAR}|{TRIGRAPH}|{UNICODEGRAPH_SHORT})\'B)
BYTEARRAY=(\"({STRING_CHAR})*\"B)
VERBATIM_BYTEARRAY=(@\"({VERBATIM_STRING_CHAR})*\"B)
SIMPLE_OR_ESCAPE_CHAR=({ESCAPE_CHAR}|{SIMPLE_CHARACTER})
TRIPLE_QUOTED_STRING_CHAR=({SIMPLE_STRING_CHAR}|{NON_ESCAPE_CHARS}|{NEW_LINE}|\\)
UNFINISHED_TRIPLE_QUOTED_STRING=(\"\"\"((\"?){TRIPLE_QUOTED_STRING_CHAR}|\"\"{TRIPLE_QUOTED_STRING_CHAR})*(\"|\"\")?)
TRIPLE_QUOTED_STRING=({UNFINISHED_TRIPLE_QUOTED_STRING}\"\"\")

LET_BANG="let!"
USE_BANG="use!"
DO_BANG="do!"
YIELD_BANG="yield!"
RETURN_BANG="return!"
MATCH_BANG="match!"
AND_BANG="and!"
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
LBRACE_BAR="{|"
BAR_RBRACK="|]"
BAR_RBRACE="|}"
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

RESERVED_SYMBOLIC_SEQUENCE=[~`]

BAD_OP_CHAR=({OP_CHAR}|([$:]))
LQUOTE_TYPED="<@"
RQUOTE_TYPED="@>"
LQUOTE_UNTYPED="<@@"
RQUOTE_UNTYPED="@@>"
QUOTE_OP_RIGHT=({RQUOTE_TYPED}|{RQUOTE_UNTYPED})
QUOTE_OP_LEFT=({LQUOTE_TYPED}|{LQUOTE_UNTYPED})
SYMBOLIC_OP=(([\?])|("?<-")|(({OP_CHAR})+)|{QUOTE_OP_LEFT}|{QUOTE_OP_RIGHT})
BAD_SYMBOLIC_OP=(([\?])|("?<-")|(({BAD_OP_CHAR})+)|{QUOTE_OP_LEFT}|{QUOTE_OP_RIGHT})

LESS_OP=(("</")|{LESS})

SEPARATOR="_"
HEXDIGIT=({DIGIT}|[A-F]|[a-f])
OCTALDIGIT=[0-7]
BITDIGIT=[0-1]
INT=({DIGIT}(({DIGIT}|{SEPARATOR})*{DIGIT})?)
XINT=(0(((x|X){HEXDIGIT}+)|((o|O){OCTALDIGIT}+)|((b|B){BITDIGIT}+)))
SBYTE=(({INT}|{XINT})y)
BYTE=(({INT}|{XINT})uy)
INT16=(({INT}|{XINT})s)
UINT16=(({INT}|{XINT})us)
INT32=(({INT}|{XINT})l)
UINT32=(({INT}|{XINT})u(l)?)
NATIVEINT=(({INT}|{XINT})n)
UNATIVEINT=(({INT}|{XINT})un)
INT64=(({INT}|{XINT})L)
UINT64=(({INT}|{XINT})[Uu]L)
IEEE32=({FLOAT}[Ff]|{XINT}lf)
IEEE64=({FLOAT}|{XINT}LF)
BIGNUM=({INT}[QRZING])
DECIMAL=(({FLOAT}|{INT})[Mm])
FLOATP=({DIGIT}(({DIGIT}|{SEPARATOR})*{DIGIT})?\.({DIGIT}(({DIGIT}|{SEPARATOR})*{DIGIT})?)?)
FLOATE=({DIGIT}(({DIGIT}|{SEPARATOR})*{DIGIT})?(\.({DIGIT}(({DIGIT}|{SEPARATOR})*{DIGIT})?)?)?[eE][+\-]?{DIGIT}(({DIGIT}|{SEPARATOR})*{DIGIT})?)
FLOAT=({FLOATP}|{FLOATE})
RESERVED_LITERAL_FORMATS=(({INT}|{XINT}|{FLOAT})({TAIL_IDENT}))

KEYWORD_STRING_SOURCE_DIRECTORY="__SOURCE_DIRECTORY__"
KEYWORD_STRING_SOURCE_FILE="__SOURCE_FILE__"
KEYWORD_STRING_LINE="__LINE__"

PP_COMPILER_DIRECTIVE=({HASH}("if"|"else"|"endif"))
PP_BAD_COMPILER_DIRECTIVE=({HASH}("if"|"else"|"endif"){TAIL_IDENT})
PP_DIRECTIVE=(({ANYWHITE})*({HASH}({IDENT}|({ANYWHITE})*([0-9])+)))
PP_CONDITIONAL_SYMBOL={IDENT}

%%

<YYINITIAL> {PP_DIRECTIVE} { yypushback(yylength()); yybegin(PPDIRECTIVE); clear(); break; }
<YYINITIAL> [^]            { yypushback(1); yybegin(LINE); clear(); break; }

<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> {LESS}                        { deepIntoParenLevel(); return makeToken(LESS); }
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> {LPAREN}                      { deepIntoParenLevel(); return makeToken(LPAREN); }
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> {LBRACK}                      { deepIntoParenLevel(); return makeToken(LBRACK); }
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> {LBRACK_LESS}                 { deepIntoBrackLevel(); return makeToken(LBRACK_LESS); }
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> ({GREATER})+                  { riseFromParenLevel(yylength()); yypushback(yylength()); yybegin(GREATER_OP); clear(); break; }
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> "</"                          { deepIntoParenLevel(); yypushback(2); yybegin(SMASH_ADJACENT_LESS_OP); clear(); break;}
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> {RPAREN}                      { riseFromParenLevel(1); return makeToken(RPAREN); }
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> {RBRACK}                      { riseFromParenLevel(1); return makeToken(RBRACK); }
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> {GREATER_RBRACK}              { yypushback(yylength()); checkGreatRBrack(SMASH_ADJACENT_GREATER_RBRACK, SMASH_ADJACENT_GREATER_RBRACK_FIN); clear(); break; }
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> {GREATER_BAR_RBRACK}          { yypushback(yylength()); initSmashAdjacent(SMASH_ADJACENT_GREATER_BAR_RBRACK, SMASH_ADJACENT_GREATER_BAR_RBRACK_FIN); clear(); break; }
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> ({GREATER})+{BAD_SYMBOLIC_OP} { yypushback(yylength()); yybegin(ADJACENT_TYPE_CLOSE_OP); clear(); break; }

<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> "default"  { return initIdent(); }
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> "struct"   { return initIdent(); }
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> "null"     { return initIdent(); }
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> "delegate" { return initIdent(); }
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> "and"      { return initIdent(); }
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> "when"     { return initIdent(); }
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> "new"      { return initIdent(); }
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> "global"   { return initIdent(); }
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> "const"    { return initIdent(); }
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> "true"     { return initIdent(); }
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> "false"    { return initIdent(); }
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> "^"        { return makeToken(SYMBOLIC_OP); }
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> "^-"       { return makeToken(SYMBOLIC_OP); }
<INIT_ADJACENT_TYAPP, ADJACENT_TYAPP> "/"        { return makeToken(SYMBOLIC_OP); }

<INIT_ADJACENT_TYAPP> {IDENT} { return identInInitTypeApp(); }
<ADJACENT_TYAPP> {IDENT}      { return identInTypeApp(); }

<ADJACENT_TYPE_CLOSE_OP> ({GREATER})+ { adjacentTypeCloseOp(); clear(); break; }

<GREATER_OP, GREATER_OP_SYMBOLIC_OP> {GREATER} { return makeToken(GREATER); }
<GREATER_OP, GREATER_OP_SYMBOLIC_OP> [^]       { yypushback(1); exitGreaterOp(); clear(); break; }

<SYMBOLIC_OPERATOR> {LQUOTE_TYPED}        { riseFromParenLevel(0); return makeToken(LQUOTE_TYPED); }
<SYMBOLIC_OPERATOR> {RQUOTE_TYPED}        { riseFromParenLevel(0); return makeToken(RQUOTE_TYPED); }
<SYMBOLIC_OPERATOR> {LQUOTE_UNTYPED}      { riseFromParenLevel(0); return makeToken(LQUOTE_UNTYPED); }
<SYMBOLIC_OPERATOR> {RQUOTE_UNTYPED}      { riseFromParenLevel(0); return makeToken(RQUOTE_UNTYPED); }
<SYMBOLIC_OPERATOR> "@>."|"@@>."          { yypushback(yylength()); yybegin(SMASH_RQUOTE_DOT); clear(); break; }
<SYMBOLIC_OPERATOR> {BAR}                 { riseFromParenLevel(0); return makeToken(BAR); }
<SYMBOLIC_OPERATOR> {LARROW}              { riseFromParenLevel(0); return makeToken(LARROW); }
<SYMBOLIC_OPERATOR> {LPAREN}              { riseFromParenLevel(0); return makeToken(LPAREN); }
<SYMBOLIC_OPERATOR> {RPAREN}              { riseFromParenLevel(0); return makeToken(RPAREN); }
<SYMBOLIC_OPERATOR> {LBRACK}              { riseFromParenLevel(0); return makeToken(LBRACK); }
<SYMBOLIC_OPERATOR> {RBRACK}              { riseFromParenLevel(0); return makeToken(RBRACK); }
<SYMBOLIC_OPERATOR> {LBRACK_LESS}         { riseFromParenLevel(0); return makeToken(LBRACK_LESS); }
<SYMBOLIC_OPERATOR> {GREATER_RBRACK}      { riseFromParenLevel(0); return makeToken(GREATER_RBRACK); }
<SYMBOLIC_OPERATOR> {LBRACK_BAR}          { riseFromParenLevel(0); return makeToken(LBRACK_BAR); }
<SYMBOLIC_OPERATOR> {LBRACE_BAR}          { riseFromParenLevel(0); return makeToken(LBRACE_BAR); }
<SYMBOLIC_OPERATOR> {LESS}                { riseFromParenLevel(0); return makeToken(LESS); }
<SYMBOLIC_OPERATOR> {GREATER}             { riseFromParenLevel(0); return makeToken(GREATER); }
<SYMBOLIC_OPERATOR> {BAR_RBRACK}          { riseFromParenLevel(0); return makeToken(BAR_RBRACK); }
<SYMBOLIC_OPERATOR> {BAR_RBRACE}          { riseFromParenLevel(0); return makeToken(BAR_RBRACE); }
<SYMBOLIC_OPERATOR> {LBRACE}              { riseFromParenLevel(0); return makeToken(LBRACE); }
<SYMBOLIC_OPERATOR> {RBRACE}              { riseFromParenLevel(0); return makeToken(RBRACE); }
<SYMBOLIC_OPERATOR> {GREATER_BAR_RBRACK}  { riseFromParenLevel(0); return makeToken(GREATER_BAR_RBRACK); }
<SYMBOLIC_OPERATOR> {COLON_QMARK_GREATER} { riseFromParenLevel(0); return makeToken(COLON_QMARK_GREATER); }
<SYMBOLIC_OPERATOR> {COLON_QMARK}         { riseFromParenLevel(0); return makeToken(COLON_QMARK); }
<SYMBOLIC_OPERATOR> {COLON_COLON}         { riseFromParenLevel(0); return makeToken(COLON_COLON); }
<SYMBOLIC_OPERATOR> {COLON_EQUALS}        { riseFromParenLevel(0); return makeToken(COLON_EQUALS); }
<SYMBOLIC_OPERATOR> {SEMICOLON_SEMICOLON} { riseFromParenLevel(0); return makeToken(SEMICOLON_SEMICOLON); }
<SYMBOLIC_OPERATOR> {SEMICOLON}           { riseFromParenLevel(0); return makeToken(SEMICOLON); }
<SYMBOLIC_OPERATOR> {QMARK}               { riseFromParenLevel(0); return makeToken(QMARK); }
<SYMBOLIC_OPERATOR> {QMARK_QMARK}         { riseFromParenLevel(0); return makeToken(QMARK_QMARK); }
<SYMBOLIC_OPERATOR> {LPAREN_STAR_RPAREN}  { riseFromParenLevel(0); return makeToken(LPAREN_STAR_RPAREN); }
<SYMBOLIC_OPERATOR> {PLUS}                { riseFromParenLevel(0); return makeToken(PLUS); }
<SYMBOLIC_OPERATOR> {DOLLAR}              { riseFromParenLevel(0); return makeToken(DOLLAR); }
<SYMBOLIC_OPERATOR> {PERCENT}             { riseFromParenLevel(0); return makeToken(PERCENT); }
<SYMBOLIC_OPERATOR> {PERCENT_PERCENT}     { riseFromParenLevel(0); return makeToken(PERCENT_PERCENT); }
<SYMBOLIC_OPERATOR> {AMP}                 { riseFromParenLevel(0); return makeToken(AMP); }
<SYMBOLIC_OPERATOR> {AMP_AMP}             { riseFromParenLevel(0); return makeToken(AMP_AMP); }
<SYMBOLIC_OPERATOR> {RARROW}              { riseFromParenLevel(0); return makeToken(RARROW); }
<SYMBOLIC_OPERATOR> {DOT}                 { riseFromParenLevel(0); return makeToken(DOT); }
<SYMBOLIC_OPERATOR> {COLON}               { riseFromParenLevel(0); return makeToken(COLON); }
<SYMBOLIC_OPERATOR> {STAR}                { riseFromParenLevel(0); return makeToken(STAR); }
<SYMBOLIC_OPERATOR> {QUOTE}               { riseFromParenLevel(0); return makeToken(QUOTE); }
<SYMBOLIC_OPERATOR> {COLON_GREATER}       { riseFromParenLevel(0); return makeToken(COLON_GREATER); }
<SYMBOLIC_OPERATOR> {DOT_DOT}             { riseFromParenLevel(0); return makeToken(DOT_DOT); }
<SYMBOLIC_OPERATOR> {EQUALS}              { riseFromParenLevel(0); return makeToken(EQUALS); }
<SYMBOLIC_OPERATOR> {UNDERSCORE}          { riseFromParenLevel(0); return makeToken(UNDERSCORE); }
<SYMBOLIC_OPERATOR> {MINUS}               { riseFromParenLevel(0); return makeToken(MINUS); }
<SYMBOLIC_OPERATOR> {COMMA}               { riseFromParenLevel(0); return makeToken(COMMA); }
<SYMBOLIC_OPERATOR> {SYMBOLIC_OP}         { riseFromParenLevel(0); return makeToken(SYMBOLIC_OP); }
<SYMBOLIC_OPERATOR> {BAD_SYMBOLIC_OP}     { riseFromParenLevel(0); return makeToken(BAD_SYMBOLIC_OP); }

<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {TAB}        { return makeToken(BAD_TAB); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {WHITESPACE} { return makeToken(WHITESPACE); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {NEW_LINE}   { yybegin(YYINITIAL); return makeToken(NEW_LINE); }

<LINE, ADJACENT_TYAPP> "(*IF-FSHARP"    { return makeToken(BLOCK_COMMENT); }
<LINE, ADJACENT_TYAPP> "ENDIF-FSHARP*)" { return makeToken(BLOCK_COMMENT); }
<LINE, ADJACENT_TYAPP> "(*F#"           { return makeToken(BLOCK_COMMENT); }
<LINE, ADJACENT_TYAPP> "F#*)"           { return makeToken(BLOCK_COMMENT); }
<LINE, ADJACENT_TYAPP> {LINE_COMMENT}   { return makeToken(LINE_COMMENT); }
<LINE, ADJACENT_TYAPP> {SHEBANG}        { return makeToken(SHEBANG); }
<LINE, ADJACENT_TYAPP> {HASH}"light"    { return makeToken(PP_LIGHT); }

<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> ("(*")                  { initBlockComment(); initTokenLength(); increaseTokenLength(yylength()); clear(); break; }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {PP_COMPILER_DIRECTIVE} { yypushback(yylength()); yybegin(BAD_PPSHARP); clear(); break; }

<INIT_ADJACENT_TYAPP> "(*IF-FSHARP"    { yybegin(LINE); return makeToken(BLOCK_COMMENT); }
<INIT_ADJACENT_TYAPP> "ENDIF-FSHARP*)" { yybegin(LINE); return makeToken(BLOCK_COMMENT); }
<INIT_ADJACENT_TYAPP> "(*F#"           { yybegin(LINE); return makeToken(BLOCK_COMMENT); }
<INIT_ADJACENT_TYAPP> "F#*)"           { yybegin(LINE); return makeToken(BLOCK_COMMENT); }
<INIT_ADJACENT_TYAPP> {LINE_COMMENT}   { yybegin(LINE); return makeToken(LINE_COMMENT); }
<INIT_ADJACENT_TYAPP> {SHEBANG}        { yybegin(LINE); return makeToken(SHEBANG); }
<INIT_ADJACENT_TYAPP> {HASH}"light"    { yybegin(LINE); return makeToken(PP_LIGHT); }

<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {PP_BAD_COMPILER_DIRECTIVE} {
  // TODO: delete this rule and use the following rule instead after fixing the bug: https://github.com/Microsoft/visualfsharp/pull/5498
  // <LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {PP_BAD_COMPILER_DIRECTIVE} { yypushback(yylength() - 1); return makeToken(HASH); }
  yypushback(yylength()); yybegin(BAD_PPSHARP); clear(); break; }

<LINE, ADJACENT_TYAPP> {LQUOTE_TYPED}   { return makeToken(LQUOTE_TYPED); }
<LINE, ADJACENT_TYAPP> {RQUOTE_TYPED}   { return makeToken(RQUOTE_TYPED); }
<LINE, ADJACENT_TYAPP> {LQUOTE_UNTYPED} { return makeToken(LQUOTE_UNTYPED); }
<LINE, ADJACENT_TYAPP> {RQUOTE_UNTYPED} { return makeToken(RQUOTE_UNTYPED); }

<LINE, INIT_ADJACENT_TYAPP> {LQUOTE_TYPED}   { yybegin(LINE); return makeToken(LQUOTE_TYPED); }
<LINE, INIT_ADJACENT_TYAPP> {RQUOTE_TYPED}   { yybegin(LINE); return makeToken(RQUOTE_TYPED); }
<LINE, INIT_ADJACENT_TYAPP> {LQUOTE_UNTYPED} { yybegin(LINE); return makeToken(LQUOTE_UNTYPED); }
<LINE, INIT_ADJACENT_TYAPP> {RQUOTE_UNTYPED} { yybegin(LINE); return makeToken(RQUOTE_UNTYPED); }

<LINE, ADJACENT_TYAPP> "@>."|"@@>." {
  // Rule for smash RQUOTE_DOT.
  // https://github.com/Microsoft/visualfsharp/blob/173513e/src/fsharp/LexFilter.fs#L2148
  yypushback(yylength()); initSmash(SMASH_RQUOTE_DOT_FROM_LINE, SMASH_RQUOTE_DOT); clear(); break; }
<INIT_ADJACENT_TYAPP> "@>."|"@@>."  { yypushback(yylength()); yybegin(SMASH_RQUOTE_DOT_FROM_LINE); clear(); break; }
