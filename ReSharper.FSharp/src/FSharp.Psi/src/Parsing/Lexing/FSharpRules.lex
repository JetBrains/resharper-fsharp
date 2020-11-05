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
%state INIT_ADJACENT_TYPE_APP
%state ADJACENT_TYPE_APP
%state SYMBOLIC_OPERATOR
%state GREATER_OP
%state GREATER_OP_SYMBOLIC_OP
%state PRE_LESS_OP
%state LINE

%state PPSHARP
%state BAD_PPSHARP
%state PPSYMBOL
%state PPDIRECTIVE

WHITESPACE=" "+
TAB=(\t+)
ANYWHITE=({WHITESPACE}|{TAB})

NEW_LINE=(\n|\r\n)
LINE_COMMENT=(\/\/([^\n\r])*)
SHEBANG=("#!"([^\n\r])*)
LETTER_CHAR=({UNICODE_LU}|{UNICODE_LL}|{UNICODE_LT}|{UNICODE_LM}|{UNICODE_LO}|{UNICODE_NL})
DIGIT={UNICODE_ND}
IDENT_START_CHAR=({LETTER_CHAR}|"_")
CONNECTING_CHAR={UNICODE_PC}
COMBINING_CHAR=({UNICODE_MN}|{UNICODE_MC})
FORMATTING_CHAR={UNICODE_CF}
IDENT_CHAR=({LETTER_CHAR}|{CONNECTING_CHAR}|{COMBINING_CHAR}|{FORMATTING_CHAR}|{DIGIT}|['_])
TAIL_IDENT=({IDENT_CHAR}+)
IDENT_TEXT={IDENT_START_CHAR}{TAIL_IDENT}?
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
CHARACTER_LITERAL=\'{CHARACTER}\'
UNFINISHED_STRING=\"{STRING_CHAR}*
STRING={UNFINISHED_STRING}\"

VERBATIM_STRING_CHAR=({SIMPLE_STRING_CHAR}|{NON_ESCAPE_CHARS}|\"\"|\\)
UNFINISHED_VERBATIM_STRING=(@\"({VERBATIM_STRING_CHAR})*)
VERBATIM_STRING={UNFINISHED_VERBATIM_STRING}\"

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
IEEE32=(({FLOAT}|{INT})[Ff]|{XINT}lf)
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

<YYINITIAL> {PP_DIRECTIVE} { PushBack(yylength()); yybegin(PPDIRECTIVE); Clear(); break; }
<YYINITIAL> [^]            { PushBack(1); yybegin(LINE); Clear(); break; }

<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> {LESS}                        { DeepIntoParenLevel(); return MakeToken(LESS); }
<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> {LPAREN}                      { DeepIntoParenLevel(); return MakeToken(LPAREN); }
<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> {LBRACK}                      { DeepIntoParenLevel(); return MakeToken(LBRACK); }
<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> {LBRACK_LESS}                 { DeepIntoBrackLevel(); return MakeToken(LBRACK_LESS); }
<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> {GREATER}+                    { RiseFromParenLevel(yylength()); PushBack(yylength()); yybegin(GREATER_OP); Clear(); break; }
<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> "</"                          { DeepIntoParenLevel(); PushBack(2); yybegin(SMASH_ADJACENT_LESS_OP); Clear(); break;}
<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> {RPAREN}                      { RiseFromParenLevel(1); return MakeToken(RPAREN); }
<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> {RBRACK}                      { RiseFromParenLevel(1); return MakeToken(RBRACK); }
<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> {GREATER_RBRACK}              { PushBack(yylength()); CheckGreatRBrack(SMASH_ADJACENT_GREATER_RBRACK, SMASH_ADJACENT_GREATER_RBRACK_FIN); Clear(); break; }
<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> {GREATER_BAR_RBRACK}          { PushBack(yylength()); InitSmashAdjacent(SMASH_ADJACENT_GREATER_BAR_RBRACK, SMASH_ADJACENT_GREATER_BAR_RBRACK_FIN); Clear(); break; }
<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> {GREATER}+{BAD_SYMBOLIC_OP}   { PushBack(yylength()); yybegin(ADJACENT_TYPE_CLOSE_OP); Clear(); break; }

<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> "default"  { return InitIdent(); }
<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> "struct"   { return InitIdent(); }
<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> "null"     { return InitIdent(); }
<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> "delegate" { return InitIdent(); }
<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> "and"      { return InitIdent(); }
<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> "when"     { return InitIdent(); }
<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> "new"      { return InitIdent(); }
<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> "global"   { return InitIdent(); }
<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> "const"    { return InitIdent(); }
<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> "true"     { return InitIdent(); }
<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> "false"    { return InitIdent(); }
<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> "^"        { return MakeToken(SYMBOLIC_OP); }
<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> "^-"       { return MakeToken(SYMBOLIC_OP); }
<INIT_ADJACENT_TYPE_APP, ADJACENT_TYPE_APP> "/"        { return MakeToken(SYMBOLIC_OP); }

<INIT_ADJACENT_TYPE_APP> {IDENT} { return IdentInInitTypeApp(); }
<ADJACENT_TYPE_APP> {IDENT}      { return IdentInTypeApp(); }

<ADJACENT_TYPE_CLOSE_OP> {GREATER}+ { AdjacentTypeCloseOp(); Clear(); break; }

<GREATER_OP, GREATER_OP_SYMBOLIC_OP> {GREATER} { return MakeToken(GREATER); }
<GREATER_OP, GREATER_OP_SYMBOLIC_OP> [^]       { PushBack(1); ExitGreaterOp(); Clear(); break; }

<SYMBOLIC_OPERATOR> {LQUOTE_TYPED}        { RiseFromParenLevel(0); return MakeToken(LQUOTE_TYPED); }
<SYMBOLIC_OPERATOR> {RQUOTE_TYPED}        { RiseFromParenLevel(0); return MakeToken(RQUOTE_TYPED); }
<SYMBOLIC_OPERATOR> {LQUOTE_UNTYPED}      { RiseFromParenLevel(0); return MakeToken(LQUOTE_UNTYPED); }
<SYMBOLIC_OPERATOR> {RQUOTE_UNTYPED}      { RiseFromParenLevel(0); return MakeToken(RQUOTE_UNTYPED); }
<SYMBOLIC_OPERATOR> "@>."|"@@>."          { PushBack(yylength()); yybegin(SMASH_RQUOTE_DOT); Clear(); break; }
<SYMBOLIC_OPERATOR> {BAR}                 { RiseFromParenLevel(0); return MakeToken(BAR); }
<SYMBOLIC_OPERATOR> {LARROW}              { RiseFromParenLevel(0); return MakeToken(LARROW); }
<SYMBOLIC_OPERATOR> {LPAREN}              { RiseFromParenLevel(0); return MakeToken(LPAREN); }
<SYMBOLIC_OPERATOR> {RPAREN}              { RiseFromParenLevel(0); return MakeToken(RPAREN); }
<SYMBOLIC_OPERATOR> {LBRACK}              { RiseFromParenLevel(0); return MakeToken(LBRACK); }
<SYMBOLIC_OPERATOR> {RBRACK}              { RiseFromParenLevel(0); return MakeToken(RBRACK); }
<SYMBOLIC_OPERATOR> {LBRACK_LESS}         { RiseFromParenLevel(0); return MakeToken(LBRACK_LESS); }
<SYMBOLIC_OPERATOR> {GREATER_RBRACK}      { RiseFromParenLevel(0); return MakeToken(GREATER_RBRACK); }
<SYMBOLIC_OPERATOR> {LBRACK_BAR}          { RiseFromParenLevel(0); return MakeToken(LBRACK_BAR); }
<SYMBOLIC_OPERATOR> {LBRACE_BAR}          { RiseFromParenLevel(0); return MakeToken(LBRACE_BAR); }
<SYMBOLIC_OPERATOR> {LESS}                { RiseFromParenLevel(0); return MakeToken(LESS); }
<SYMBOLIC_OPERATOR> {GREATER}             { RiseFromParenLevel(0); return MakeToken(GREATER); }
<SYMBOLIC_OPERATOR> {BAR_RBRACK}          { RiseFromParenLevel(0); return MakeToken(BAR_RBRACK); }
<SYMBOLIC_OPERATOR> {BAR_RBRACE}          { RiseFromParenLevel(0); return MakeToken(BAR_RBRACE); }
<SYMBOLIC_OPERATOR> {LBRACE}              { RiseFromParenLevel(0); return MakeToken(LBRACE); }
<SYMBOLIC_OPERATOR> {RBRACE}              { RiseFromParenLevel(0); return MakeToken(RBRACE); }
<SYMBOLIC_OPERATOR> {GREATER_BAR_RBRACK}  { RiseFromParenLevel(0); return MakeToken(GREATER_BAR_RBRACK); }
<SYMBOLIC_OPERATOR> {COLON_QMARK_GREATER} { RiseFromParenLevel(0); return MakeToken(COLON_QMARK_GREATER); }
<SYMBOLIC_OPERATOR> {COLON_QMARK}         { RiseFromParenLevel(0); return MakeToken(COLON_QMARK); }
<SYMBOLIC_OPERATOR> {COLON_COLON}         { RiseFromParenLevel(0); return MakeToken(COLON_COLON); }
<SYMBOLIC_OPERATOR> {COLON_EQUALS}        { RiseFromParenLevel(0); return MakeToken(COLON_EQUALS); }
<SYMBOLIC_OPERATOR> {SEMICOLON_SEMICOLON} { RiseFromParenLevel(0); return MakeToken(SEMICOLON_SEMICOLON); }
<SYMBOLIC_OPERATOR> {SEMICOLON}           { RiseFromParenLevel(0); return MakeToken(SEMICOLON); }
<SYMBOLIC_OPERATOR> {QMARK}               { RiseFromParenLevel(0); return MakeToken(QMARK); }
<SYMBOLIC_OPERATOR> {QMARK_QMARK}         { RiseFromParenLevel(0); return MakeToken(QMARK_QMARK); }
<SYMBOLIC_OPERATOR> {LPAREN_STAR_RPAREN}  { RiseFromParenLevel(0); return MakeToken(LPAREN_STAR_RPAREN); }
<SYMBOLIC_OPERATOR> {PLUS}                { RiseFromParenLevel(0); return MakeToken(PLUS); }
<SYMBOLIC_OPERATOR> {DOLLAR}              { RiseFromParenLevel(0); return MakeToken(DOLLAR); }
<SYMBOLIC_OPERATOR> {PERCENT}             { RiseFromParenLevel(0); return MakeToken(PERCENT); }
<SYMBOLIC_OPERATOR> {PERCENT_PERCENT}     { RiseFromParenLevel(0); return MakeToken(PERCENT_PERCENT); }
<SYMBOLIC_OPERATOR> {AMP}                 { RiseFromParenLevel(0); return MakeToken(AMP); }
<SYMBOLIC_OPERATOR> {AMP_AMP}             { RiseFromParenLevel(0); return MakeToken(AMP_AMP); }
<SYMBOLIC_OPERATOR> {RARROW}              { RiseFromParenLevel(0); return MakeToken(RARROW); }
<SYMBOLIC_OPERATOR> {DOT}                 { RiseFromParenLevel(0); return MakeToken(DOT); }
<SYMBOLIC_OPERATOR> {COLON}               { RiseFromParenLevel(0); return MakeToken(COLON); }
<SYMBOLIC_OPERATOR> {STAR}                { RiseFromParenLevel(0); return MakeToken(STAR); }
<SYMBOLIC_OPERATOR> {QUOTE}               { RiseFromParenLevel(0); return MakeToken(QUOTE); }
<SYMBOLIC_OPERATOR> {COLON_GREATER}       { RiseFromParenLevel(0); return MakeToken(COLON_GREATER); }
<SYMBOLIC_OPERATOR> {DOT_DOT}             { RiseFromParenLevel(0); return MakeToken(DOT_DOT); }
<SYMBOLIC_OPERATOR> {EQUALS}              { RiseFromParenLevel(0); return MakeToken(EQUALS); }
<SYMBOLIC_OPERATOR> {UNDERSCORE}          { RiseFromParenLevel(0); return MakeToken(UNDERSCORE); }
<SYMBOLIC_OPERATOR> {MINUS}               { RiseFromParenLevel(0); return MakeToken(MINUS); }
<SYMBOLIC_OPERATOR> {COMMA}               { RiseFromParenLevel(0); return MakeToken(COMMA); }
<SYMBOLIC_OPERATOR> {SYMBOLIC_OP}         { RiseFromParenLevel(0); return MakeToken(SYMBOLIC_OP); }
<SYMBOLIC_OPERATOR> {BAD_SYMBOLIC_OP}     { RiseFromParenLevel(0); return MakeToken(BAD_SYMBOLIC_OP); }

<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {TAB}        { return MakeToken(BAD_TAB); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {WHITESPACE} { return MakeToken(WHITESPACE); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {NEW_LINE}   { yybegin(YYINITIAL); return MakeToken(NEW_LINE); }

<LINE, ADJACENT_TYPE_APP> "(*IF-FSHARP"    { return MakeToken(BLOCK_COMMENT); }
<LINE, ADJACENT_TYPE_APP> "ENDIF-FSHARP*)" { return MakeToken(BLOCK_COMMENT); }
<LINE, ADJACENT_TYPE_APP> "(*F#"           { return MakeToken(BLOCK_COMMENT); }
<LINE, ADJACENT_TYPE_APP> "F#*)"           { return MakeToken(BLOCK_COMMENT); }
<LINE, ADJACENT_TYPE_APP> {LINE_COMMENT}   { return MakeToken(LINE_COMMENT); }
<LINE, ADJACENT_TYPE_APP> {SHEBANG}        { return MakeToken(SHEBANG); }
<LINE, ADJACENT_TYPE_APP> {HASH}"light"    { return MakeToken(PP_LIGHT); }

<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> ("(*")                  { InitBlockComment(); InitTokenLength(); IncreaseTokenLength(yylength()); Clear(); break; }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {PP_COMPILER_DIRECTIVE} { PushBack(yylength()); yybegin(BAD_PPSHARP); Clear(); break; }

<INIT_ADJACENT_TYPE_APP> "(*IF-FSHARP"    { yybegin(LINE); return MakeToken(BLOCK_COMMENT); }
<INIT_ADJACENT_TYPE_APP> "ENDIF-FSHARP*)" { yybegin(LINE); return MakeToken(BLOCK_COMMENT); }
<INIT_ADJACENT_TYPE_APP> "(*F#"           { yybegin(LINE); return MakeToken(BLOCK_COMMENT); }
<INIT_ADJACENT_TYPE_APP> "F#*)"           { yybegin(LINE); return MakeToken(BLOCK_COMMENT); }
<INIT_ADJACENT_TYPE_APP> {LINE_COMMENT}   { yybegin(LINE); return MakeToken(LINE_COMMENT); }
<INIT_ADJACENT_TYPE_APP> {SHEBANG}        { yybegin(LINE); return MakeToken(SHEBANG); }
<INIT_ADJACENT_TYPE_APP> {HASH}"light"    { yybegin(LINE); return MakeToken(PP_LIGHT); }

<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {PP_BAD_COMPILER_DIRECTIVE} {
  // TODO: delete this rule and use the following rule instead after fixing the bug: https://github.com/Microsoft/visualfsharp/pull/5498
  // <LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {PP_BAD_COMPILER_DIRECTIVE} { PushBack(yylength() - 1); return MakeToken(HASH); }
  PushBack(yylength()); yybegin(BAD_PPSHARP); Clear(); break; }

<LINE, ADJACENT_TYPE_APP> {LQUOTE_TYPED}   { return MakeToken(LQUOTE_TYPED); }
<LINE, ADJACENT_TYPE_APP> {RQUOTE_TYPED}   { return MakeToken(RQUOTE_TYPED); }
<LINE, ADJACENT_TYPE_APP> {LQUOTE_UNTYPED} { return MakeToken(LQUOTE_UNTYPED); }
<LINE, ADJACENT_TYPE_APP> {RQUOTE_UNTYPED} { return MakeToken(RQUOTE_UNTYPED); }

<LINE, INIT_ADJACENT_TYPE_APP> {LQUOTE_TYPED}   { yybegin(LINE); return MakeToken(LQUOTE_TYPED); }
<LINE, INIT_ADJACENT_TYPE_APP> {RQUOTE_TYPED}   { yybegin(LINE); return MakeToken(RQUOTE_TYPED); }
<LINE, INIT_ADJACENT_TYPE_APP> {LQUOTE_UNTYPED} { yybegin(LINE); return MakeToken(LQUOTE_UNTYPED); }
<LINE, INIT_ADJACENT_TYPE_APP> {RQUOTE_UNTYPED} { yybegin(LINE); return MakeToken(RQUOTE_UNTYPED); }

<LINE, ADJACENT_TYPE_APP> "@>."|"@@>." {
  // Rule for smash RQUOTE_DOT.
  // https://github.com/Microsoft/visualfsharp/blob/173513e/src/fsharp/LexFilter.fs#L2148
  PushBack(yylength()); InitSmash(SMASH_RQUOTE_DOT_FROM_LINE, SMASH_RQUOTE_DOT); Clear(); break; }
<INIT_ADJACENT_TYPE_APP> "@>."|"@@>."  { PushBack(yylength()); yybegin(SMASH_RQUOTE_DOT_FROM_LINE); Clear(); break; }

<LINE, ADJACENT_TYPE_APP> {LET_BANG}            { return MakeToken(LET_BANG); }
<LINE, ADJACENT_TYPE_APP> {USE_BANG}            { return MakeToken(USE_BANG); }
<LINE, ADJACENT_TYPE_APP> {DO_BANG}             { return MakeToken(DO_BANG); }
<LINE, ADJACENT_TYPE_APP> {YIELD_BANG}          { return MakeToken(YIELD_BANG); }
<LINE, ADJACENT_TYPE_APP> {RETURN_BANG}         { return MakeToken(RETURN_BANG); }
<LINE, ADJACENT_TYPE_APP> {MATCH_BANG}          { return MakeToken(MATCH_BANG); }
<LINE, ADJACENT_TYPE_APP> {AND_BANG}            { return MakeToken(AND_BANG); }
<LINE, ADJACENT_TYPE_APP> {BAR}                 { return MakeToken(BAR); }
<LINE, ADJACENT_TYPE_APP> {LARROW}              { return MakeToken(LARROW); }
<LINE, ADJACENT_TYPE_APP> {LPAREN}              { return MakeToken(LPAREN); }
<LINE, ADJACENT_TYPE_APP> {RPAREN}              { return MakeToken(RPAREN); }
<LINE, ADJACENT_TYPE_APP> {LBRACK}              { return MakeToken(LBRACK); }
<LINE, ADJACENT_TYPE_APP> {RBRACK}              { return MakeToken(RBRACK); }
<LINE, ADJACENT_TYPE_APP> {LBRACK_LESS}         { return MakeToken(LBRACK_LESS); }
<LINE, ADJACENT_TYPE_APP> {GREATER_RBRACK}      { return MakeToken(GREATER_RBRACK); }
<LINE, ADJACENT_TYPE_APP> {LBRACK_BAR}          { return MakeToken(LBRACK_BAR); }
<LINE, ADJACENT_TYPE_APP> {LBRACE_BAR}          { return MakeToken(LBRACE_BAR); }
<LINE, ADJACENT_TYPE_APP> {LESS}                { return MakeToken(LESS); }
<LINE, ADJACENT_TYPE_APP> {GREATER}             { return MakeToken(GREATER); }
<LINE, ADJACENT_TYPE_APP> {BAR_RBRACK}          { return MakeToken(BAR_RBRACK); }
<LINE, ADJACENT_TYPE_APP> {BAR_RBRACE}          { return MakeToken(BAR_RBRACE); }
<LINE, ADJACENT_TYPE_APP> {LBRACE}              { return MakeToken(LBRACE); }
<LINE, ADJACENT_TYPE_APP> {RBRACE}              { return MakeToken(RBRACE); }
<LINE, ADJACENT_TYPE_APP> {GREATER_BAR_RBRACK}  { return MakeToken(GREATER_BAR_RBRACK); }
<LINE, ADJACENT_TYPE_APP> {COLON_QMARK_GREATER} { return MakeToken(COLON_QMARK_GREATER); }
<LINE, ADJACENT_TYPE_APP> {COLON_QMARK}         { return MakeToken(COLON_QMARK); }
<LINE, ADJACENT_TYPE_APP> {COLON_COLON}         { return MakeToken(COLON_COLON); }
<LINE, ADJACENT_TYPE_APP> {COLON_EQUALS}        { return MakeToken(COLON_EQUALS); }
<LINE, ADJACENT_TYPE_APP> {SEMICOLON_SEMICOLON} { return MakeToken(SEMICOLON_SEMICOLON); }
<LINE, ADJACENT_TYPE_APP> {SEMICOLON}           { return MakeToken(SEMICOLON); }
<LINE, ADJACENT_TYPE_APP> {QMARK}               { return MakeToken(QMARK); }
<LINE, ADJACENT_TYPE_APP> {QMARK_QMARK}         { return MakeToken(QMARK_QMARK); }
<LINE, ADJACENT_TYPE_APP> {LPAREN_STAR_RPAREN}  { return MakeToken(LPAREN_STAR_RPAREN); }
<LINE, ADJACENT_TYPE_APP> {PLUS}                { return MakeToken(PLUS); }
<LINE, ADJACENT_TYPE_APP> {DOLLAR}              { return MakeToken(DOLLAR); }
<LINE, ADJACENT_TYPE_APP> {PERCENT}             { return MakeToken(PERCENT); }
<LINE, ADJACENT_TYPE_APP> {PERCENT_PERCENT}     { return MakeToken(PERCENT_PERCENT); }
<LINE, ADJACENT_TYPE_APP> {AMP}                 { return MakeToken(AMP); }
<LINE, ADJACENT_TYPE_APP> {AMP_AMP}             { return MakeToken(AMP_AMP); }

<INIT_ADJACENT_TYPE_APP> {LET_BANG}            { yybegin(LINE); return MakeToken(LET_BANG); }
<INIT_ADJACENT_TYPE_APP> {USE_BANG}            { yybegin(LINE); return MakeToken(USE_BANG); }
<INIT_ADJACENT_TYPE_APP> {DO_BANG}             { yybegin(LINE); return MakeToken(DO_BANG); }
<INIT_ADJACENT_TYPE_APP> {YIELD_BANG}          { yybegin(LINE); return MakeToken(YIELD_BANG); }
<INIT_ADJACENT_TYPE_APP> {RETURN_BANG}         { yybegin(LINE); return MakeToken(RETURN_BANG); }
<INIT_ADJACENT_TYPE_APP> {MATCH_BANG}          { yybegin(LINE); return MakeToken(MATCH_BANG); }
<INIT_ADJACENT_TYPE_APP> {AND_BANG}            { yybegin(LINE); return MakeToken(AND_BANG); }
<INIT_ADJACENT_TYPE_APP> {BAR}                 { yybegin(LINE); return MakeToken(BAR); }
<INIT_ADJACENT_TYPE_APP> {LARROW}              { yybegin(LINE); return MakeToken(LARROW); }
<INIT_ADJACENT_TYPE_APP> {LBRACK_BAR}          { yybegin(LINE); return MakeToken(LBRACK_BAR); }
<INIT_ADJACENT_TYPE_APP> {BAR_RBRACK}          { yybegin(LINE); return MakeToken(BAR_RBRACK); }
<INIT_ADJACENT_TYPE_APP> {LBRACE_BAR}          { yybegin(LINE); return MakeToken(LBRACE_BAR); }
<INIT_ADJACENT_TYPE_APP> {BAR_RBRACE}          { yybegin(LINE); return MakeToken(BAR_RBRACE); }
<INIT_ADJACENT_TYPE_APP> {LBRACE}              { yybegin(LINE); return MakeToken(LBRACE); }
<INIT_ADJACENT_TYPE_APP> {RBRACE}              { yybegin(LINE); return MakeToken(RBRACE); }
<INIT_ADJACENT_TYPE_APP> {COLON_QMARK_GREATER} { yybegin(LINE); return MakeToken(COLON_QMARK_GREATER); }
<INIT_ADJACENT_TYPE_APP> {COLON_QMARK}         { yybegin(LINE); return MakeToken(COLON_QMARK); }
<INIT_ADJACENT_TYPE_APP> {COLON_COLON}         { yybegin(LINE); return MakeToken(COLON_COLON); }
<INIT_ADJACENT_TYPE_APP> {COLON_EQUALS}        { yybegin(LINE); return MakeToken(COLON_EQUALS); }
<INIT_ADJACENT_TYPE_APP> {SEMICOLON_SEMICOLON} { yybegin(LINE); return MakeToken(SEMICOLON_SEMICOLON); }
<INIT_ADJACENT_TYPE_APP> {SEMICOLON}           { yybegin(LINE); return MakeToken(SEMICOLON); }
<INIT_ADJACENT_TYPE_APP> {QMARK}               { yybegin(LINE); return MakeToken(QMARK); }
<INIT_ADJACENT_TYPE_APP> {QMARK_QMARK}         { yybegin(LINE); return MakeToken(QMARK_QMARK); }
<INIT_ADJACENT_TYPE_APP> {LPAREN_STAR_RPAREN}  { yybegin(LINE); return MakeToken(LPAREN_STAR_RPAREN); }
<INIT_ADJACENT_TYPE_APP> {PLUS}                { yybegin(LINE); return MakeToken(PLUS); }
<INIT_ADJACENT_TYPE_APP> {DOLLAR}              { yybegin(LINE); return MakeToken(DOLLAR); }
<INIT_ADJACENT_TYPE_APP> {PERCENT}             { yybegin(LINE); return MakeToken(PERCENT); }
<INIT_ADJACENT_TYPE_APP> {PERCENT_PERCENT}     { yybegin(LINE); return MakeToken(PERCENT_PERCENT); }
<INIT_ADJACENT_TYPE_APP> {AMP}                 { yybegin(LINE); return MakeToken(AMP); }
<INIT_ADJACENT_TYPE_APP> {AMP_AMP}             { yybegin(LINE); return MakeToken(AMP_AMP); }

<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {LBRACE_BAR}    { return MakeToken(LBRACE_BAR); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {BAR_RBRACE}    { return MakeToken(BAR_RBRACE); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {SEMICOLON}     { return MakeToken(SEMICOLON); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {RARROW}        { return MakeToken(RARROW); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {DOT}           { return MakeToken(DOT); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {COLON}         { return MakeToken(COLON); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {STAR}          { return MakeToken(STAR); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {QUOTE}         { return MakeToken(QUOTE); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {HASH}          { return MakeToken(HASH); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {COLON_GREATER} { return MakeToken(COLON_GREATER); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {DOT_DOT}       { return MakeToken(DOT_DOT); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {EQUALS}        { return MakeToken(EQUALS); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {UNDERSCORE}    { return MakeToken(UNDERSCORE); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {MINUS}         { return MakeToken(MINUS); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {COMMA}         { return MakeToken(COMMA); }

<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {KEYWORD_STRING_SOURCE_DIRECTORY} { return MakeToken(KEYWORD_STRING_SOURCE_DIRECTORY); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {KEYWORD_STRING_SOURCE_FILE}      { return MakeToken(KEYWORD_STRING_SOURCE_FILE); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {KEYWORD_STRING_LINE}             { return MakeToken(KEYWORD_STRING_LINE); }

<LINE, ADJACENT_TYPE_APP> {RESERVED_IDENT_FORMATS} { return MakeToken(RESERVED_IDENT_FORMATS); }

<LINE> "delegate"{LESS_OP}                 {
  // Rule for smashing type apply.
  // https://github.com/Microsoft/visualfsharp/blob/173513e/src/fsharp/LexFilter.fs#L2131
  InitAdjacentTypeApp(); return MakeToken(DELEGATE); }
<LINE> {IDENT}{LESS_OP}                    { InitAdjacentTypeApp(); return IdentInTypeApp(); }
<LINE> {IEEE32}{LESS_OP}                   { InitAdjacentTypeApp(); return MakeToken(IEEE32); }
<LINE> {IEEE64}{LESS_OP}                   { InitAdjacentTypeApp(); return MakeToken(IEEE64); }
<LINE> {DECIMAL}{LESS_OP}                  { InitAdjacentTypeApp(); return MakeToken(DECIMAL); }
<LINE> {BYTE}{LESS_OP}                     { InitAdjacentTypeApp(); return MakeToken(BYTE); }
<LINE> {INT16}{LESS_OP}                    { InitAdjacentTypeApp(); return MakeToken(INT16); }
<LINE> ({XINT}|{INT}){LESS_OP}             { InitAdjacentTypeApp(); return MakeToken(INT32); }
<LINE> {INT32}{LESS_OP}                    { InitAdjacentTypeApp(); return MakeToken(INT32); }
<LINE> {INT64}{LESS_OP}                    { InitAdjacentTypeApp(); return MakeToken(INT64); }
<LINE> {SBYTE}{LESS_OP}                    { InitAdjacentTypeApp(); return MakeToken(SBYTE); }
<LINE> {UINT16}{LESS_OP}                   { InitAdjacentTypeApp(); return MakeToken(UINT16); }
<LINE> {UINT32}{LESS_OP}                   { InitAdjacentTypeApp(); return MakeToken(UINT32); }
<LINE> {UINT64}{LESS_OP}                   { InitAdjacentTypeApp(); return MakeToken(UINT64); }
<LINE> {BIGNUM}{LESS_OP}                   { InitAdjacentTypeApp(); return MakeToken(BIGNUM); }
<LINE> {NATIVEINT}{LESS_OP}                { InitAdjacentTypeApp(); return MakeToken(NATIVEINT); }
<LINE> {RESERVED_LITERAL_FORMATS}{LESS_OP} { InitAdjacentTypeApp(); return MakeToken(RESERVED_LITERAL_FORMATS); }

<LINE> "delegate"{LESS_OP}{BAD_SYMBOLIC_OP}                 { PushBack(yylength()); yybegin(PRE_LESS_OP); Clear(); break; }
<LINE> {IDENT}{LESS_OP}{BAD_SYMBOLIC_OP}                    { PushBack(yylength()); yybegin(PRE_LESS_OP); Clear(); break; }
<LINE> {IEEE32}{LESS_OP}{BAD_SYMBOLIC_OP}                   { PushBack(yylength()); yybegin(PRE_LESS_OP); Clear(); break; }
<LINE> {IEEE64}{LESS_OP}{BAD_SYMBOLIC_OP}                   { PushBack(yylength()); yybegin(PRE_LESS_OP); Clear(); break; }
<LINE> {DECIMAL}{LESS_OP}{BAD_SYMBOLIC_OP}                  { PushBack(yylength()); yybegin(PRE_LESS_OP); Clear(); break; }
<LINE> {BYTE}{LESS_OP}{BAD_SYMBOLIC_OP}                     { PushBack(yylength()); yybegin(PRE_LESS_OP); Clear(); break; }
<LINE> {INT16}{LESS_OP}{BAD_SYMBOLIC_OP}                    { PushBack(yylength()); yybegin(PRE_LESS_OP); Clear(); break; }
<LINE> ({XINT}|{INT}){LESS_OP}{BAD_SYMBOLIC_OP}             { PushBack(yylength()); yybegin(PRE_LESS_OP); Clear(); break; }
<LINE> {INT32}{LESS_OP}{BAD_SYMBOLIC_OP}                    { PushBack(yylength()); yybegin(PRE_LESS_OP); Clear(); break; }
<LINE> {INT64}{LESS_OP}{BAD_SYMBOLIC_OP}                    { PushBack(yylength()); yybegin(PRE_LESS_OP); Clear(); break; }
<LINE> {SBYTE}{LESS_OP}{BAD_SYMBOLIC_OP}                    { PushBack(yylength()); yybegin(PRE_LESS_OP); Clear(); break; }
<LINE> {UINT16}{LESS_OP}{BAD_SYMBOLIC_OP}                   { PushBack(yylength()); yybegin(PRE_LESS_OP); Clear(); break; }
<LINE> {UINT32}{LESS_OP}{BAD_SYMBOLIC_OP}                   { PushBack(yylength()); yybegin(PRE_LESS_OP); Clear(); break; }
<LINE> {UINT64}{LESS_OP}{BAD_SYMBOLIC_OP}                   { PushBack(yylength()); yybegin(PRE_LESS_OP); Clear(); break; }
<LINE> {BIGNUM}{LESS_OP}{BAD_SYMBOLIC_OP}                   { PushBack(yylength()); yybegin(PRE_LESS_OP); Clear(); break; }
<LINE> {NATIVEINT}{LESS_OP}{BAD_SYMBOLIC_OP}                { PushBack(yylength()); yybegin(PRE_LESS_OP); Clear(); break; }
<LINE> {RESERVED_LITERAL_FORMATS}{LESS_OP}{BAD_SYMBOLIC_OP} { PushBack(yylength()); yybegin(PRE_LESS_OP); Clear(); break; }

<PRE_LESS_OP> "delegate"                 { yybegin(LINE); return MakeToken(DELEGATE); }
<PRE_LESS_OP> {IDENT}                    { yybegin(LINE); return IdentInTypeApp(); }
<PRE_LESS_OP> {IEEE32}                   { yybegin(LINE); return MakeToken(IEEE32); }
<PRE_LESS_OP> {IEEE64}                   { yybegin(LINE); return MakeToken(IEEE64); }
<PRE_LESS_OP> {DECIMAL}                  { yybegin(LINE); return MakeToken(DECIMAL); }
<PRE_LESS_OP> {BYTE}                     { yybegin(LINE); return MakeToken(BYTE); }
<PRE_LESS_OP> {INT16}                    { yybegin(LINE); return MakeToken(INT16); }
<PRE_LESS_OP> {XINT}|{INT}               { yybegin(LINE); return MakeToken(INT32); }
<PRE_LESS_OP> {INT32}                    { yybegin(LINE); return MakeToken(INT32); }
<PRE_LESS_OP> {INT64}                    { yybegin(LINE); return MakeToken(INT64); }
<PRE_LESS_OP> {SBYTE}                    { yybegin(LINE); return MakeToken(SBYTE); }
<PRE_LESS_OP> {UINT16}                   { yybegin(LINE); return MakeToken(UINT16); }
<PRE_LESS_OP> {UINT32}                   { yybegin(LINE); return MakeToken(UINT32); }
<PRE_LESS_OP> {UINT64}                   { yybegin(LINE); return MakeToken(UINT64); }
<PRE_LESS_OP> {BIGNUM}                   { yybegin(LINE); return MakeToken(BIGNUM); }
<PRE_LESS_OP> {NATIVEINT}                { yybegin(LINE); return MakeToken(NATIVEINT); }
<PRE_LESS_OP> {RESERVED_LITERAL_FORMATS} { yybegin(LINE); return MakeToken(RESERVED_LITERAL_FORMATS); }

<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {IDENT}      { return InitIdent(); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {SBYTE}      { return MakeToken(SBYTE); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {BYTE}       { return MakeToken(BYTE); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {INT16}      { return MakeToken(INT16); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {UINT16}     { return MakeToken(UINT16); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {XINT}|{INT} { return MakeToken(INT32); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {INT32}      { return MakeToken(INT32); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {UINT32}     { return MakeToken(UINT32); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {NATIVEINT}  { return MakeToken(NATIVEINT); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {UNATIVEINT} { return MakeToken(UNATIVEINT); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {INT64}      { return MakeToken(INT64); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {UINT64}     { return MakeToken(UINT64); }

<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {INT}\.\. {
  // Rule for smashing INT_DOT_DOT.
  // https://github.com/Microsoft/visualfsharp/blob/173513e/src/fsharp/LexFilter.fs#L2142
  PushBack(yylength()); InitSmash(SMASH_INT_DOT_DOT_FROM_LINE, SMASH_INT_DOT_DOT); Clear(); break; }

<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {IEEE32}  { return MakeToken(IEEE32); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {IEEE64}  { return MakeToken(IEEE64); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {BIGNUM}  { return MakeToken(BIGNUM); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {DECIMAL} { return MakeToken(DECIMAL); }

<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {CHARACTER_LITERAL}               { return MakeToken(CHARACTER_LITERAL); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {STRING}                          { return MakeToken(STRING); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {UNFINISHED_STRING}               { return MakeToken(UNFINISHED_STRING); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {VERBATIM_STRING}                 { return MakeToken(VERBATIM_STRING); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {UNFINISHED_VERBATIM_STRING}      { return MakeToken(UNFINISHED_VERBATIM_STRING); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {TRIPLE_QUOTED_STRING}            { return MakeToken(TRIPLE_QUOTED_STRING); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {UNFINISHED_TRIPLE_QUOTED_STRING} { return MakeToken(UNFINISHED_TRIPLE_QUOTED_STRING); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {BYTEARRAY}                       { return MakeToken(BYTEARRAY); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {VERBATIM_BYTEARRAY}              { return MakeToken(VERBATIM_BYTEARRAY); }
<LINE, ADJACENT_TYPE_APP, INIT_ADJACENT_TYPE_APP> {BYTECHAR}                        { return MakeToken(BYTECHAR); }

<LINE, ADJACENT_TYPE_APP> {RESERVED_SYMBOLIC_SEQUENCE} { return MakeToken(RESERVED_SYMBOLIC_SEQUENCE); }
<LINE, ADJACENT_TYPE_APP> {RESERVED_LITERAL_FORMATS}   { return MakeToken(RESERVED_LITERAL_FORMATS); }
<LINE, ADJACENT_TYPE_APP> {SYMBOLIC_OP}                { return MakeToken(SYMBOLIC_OP); }
<LINE, ADJACENT_TYPE_APP> {BAD_SYMBOLIC_OP}            { return MakeToken(BAD_SYMBOLIC_OP); }

<INIT_ADJACENT_TYPE_APP> {RESERVED_SYMBOLIC_SEQUENCE} { yybegin(LINE); return MakeToken(RESERVED_SYMBOLIC_SEQUENCE); }
<INIT_ADJACENT_TYPE_APP> {RESERVED_LITERAL_FORMATS}   { yybegin(LINE); return MakeToken(RESERVED_LITERAL_FORMATS); }
<INIT_ADJACENT_TYPE_APP> {SYMBOLIC_OP}                { yybegin(LINE); return MakeToken(SYMBOLIC_OP); }
<INIT_ADJACENT_TYPE_APP> {BAD_SYMBOLIC_OP}            { yybegin(LINE); return MakeToken(BAD_SYMBOLIC_OP); }

<SMASH_INT_DOT_DOT, SMASH_INT_DOT_DOT_FROM_LINE> {INT} { return MakeToken(INT32); }
<SMASH_INT_DOT_DOT, SMASH_INT_DOT_DOT_FROM_LINE> \.\.  { ExitSmash(SMASH_INT_DOT_DOT_FROM_LINE); return MakeToken(DOT_DOT); }

<SMASH_RQUOTE_DOT, SMASH_RQUOTE_DOT_FROM_LINE> "@>"  { return MakeToken(RQUOTE_TYPED); }
<SMASH_RQUOTE_DOT, SMASH_RQUOTE_DOT_FROM_LINE> "@@>" { return MakeToken(RQUOTE_UNTYPED); }
<SMASH_RQUOTE_DOT, SMASH_RQUOTE_DOT_FROM_LINE> \.    { ExitSmash(SMASH_RQUOTE_DOT_FROM_LINE); return MakeToken(DOT); }

<STRING_IN_COMMENT> {STRING}                          { yybegin(IN_BLOCK_COMMENT); IncreaseTokenLength(yylength()); Clear(); break; }
<STRING_IN_COMMENT> {VERBATIM_STRING}                 { yybegin(IN_BLOCK_COMMENT); IncreaseTokenLength(yylength()); Clear(); break; }
<STRING_IN_COMMENT> {TRIPLE_QUOTED_STRING}            { yybegin(IN_BLOCK_COMMENT); IncreaseTokenLength(yylength()); Clear(); break; }
<STRING_IN_COMMENT> {UNFINISHED_STRING}               { return FillBlockComment(UNFINISHED_STRING_IN_COMMENT); }
<STRING_IN_COMMENT> {UNFINISHED_VERBATIM_STRING}      { return FillBlockComment(UNFINISHED_VERBATIM_STRING_IN_COMMENT); }
<STRING_IN_COMMENT> {UNFINISHED_TRIPLE_QUOTED_STRING} { return FillBlockComment(UNFINISHED_TRIPLE_QUOTED_STRING_IN_COMMENT); }

<IN_BLOCK_COMMENT, IN_BLOCK_COMMENT_FROM_LINE> "(*"      { myNestedCommentLevel++; IncreaseTokenLength(yylength()); Clear(); break; }
<IN_BLOCK_COMMENT, IN_BLOCK_COMMENT_FROM_LINE> "*)"      { if (--myNestedCommentLevel == 0) return FillBlockComment(BLOCK_COMMENT); IncreaseTokenLength(yylength()); Clear(); break; }
<IN_BLOCK_COMMENT, IN_BLOCK_COMMENT_FROM_LINE> \"        { PushBack(yylength()); yybegin(STRING_IN_COMMENT); Clear(); break; }
<IN_BLOCK_COMMENT, IN_BLOCK_COMMENT_FROM_LINE> @\"       { PushBack(yylength()); yybegin(STRING_IN_COMMENT); Clear(); break; }
<IN_BLOCK_COMMENT, IN_BLOCK_COMMENT_FROM_LINE> \"\"\"    { PushBack(yylength()); yybegin(STRING_IN_COMMENT); Clear(); break; }
<IN_BLOCK_COMMENT, IN_BLOCK_COMMENT_FROM_LINE> [^(\"@*]+ { IncreaseTokenLength(yylength()); Clear(); break; }
<IN_BLOCK_COMMENT, IN_BLOCK_COMMENT_FROM_LINE> [^]|"(*)" { IncreaseTokenLength(yylength()); Clear(); break; }
<IN_BLOCK_COMMENT, IN_BLOCK_COMMENT_FROM_LINE> <<EOF>>   { return FillBlockComment(UNFINISHED_BLOCK_COMMENT); }

<SMASH_ADJACENT_LESS_OP> {LESS} { return MakeToken(LESS); }
<SMASH_ADJACENT_LESS_OP> "/"    { RiseFromParenLevel(0); return MakeToken(SYMBOLIC_OP); }

<SMASH_ADJACENT_GREATER_BAR_RBRACK, SMASH_ADJACENT_GREATER_BAR_RBRACK_FIN> {GREATER}    { return MakeToken(GREATER); }
<SMASH_ADJACENT_GREATER_BAR_RBRACK, SMASH_ADJACENT_GREATER_BAR_RBRACK_FIN> {BAR_RBRACK} { ExitSmash(SMASH_ADJACENT_GREATER_BAR_RBRACK_FIN); return MakeToken(BAR_RBRACK); }

<SMASH_ADJACENT_GREATER_RBRACK, SMASH_ADJACENT_GREATER_RBRACK_FIN> {GREATER} { return MakeToken(GREATER); }
<SMASH_ADJACENT_GREATER_RBRACK, SMASH_ADJACENT_GREATER_RBRACK_FIN> {RBRACK}  { ExitSmash(SMASH_ADJACENT_GREATER_RBRACK_FIN); return MakeToken(RBRACK); }

<PPSHARP, PPSYMBOL, PPDIRECTIVE> {ANYWHITE} {
  // No need rule for ADJACENT_PREFIX rule.
  // https://github.com/Microsoft/visualfsharp/blob/173513e/src/fsharp/LexFilter.fs#L2154
  return MakeToken (WHITESPACE); }

<PPDIRECTIVE> {HASH}("l"|"load")                 { yybegin(LINE); return MakeToken(PP_LOAD); }
<PPDIRECTIVE> {HASH}("r"|"reference")            { yybegin(LINE); return MakeToken(PP_REFERENCE); }
<PPDIRECTIVE> {HASH}("line"|({ANYWHITE})*[0-9]+) { yybegin(LINE); return MakeToken(PP_LINE); }
<PPDIRECTIVE> {HASH}"help"                       { yybegin(LINE); return MakeToken(PP_HELP); }
<PPDIRECTIVE> {HASH}"quit"                       { yybegin(LINE); return MakeToken(PP_QUIT); }
<PPDIRECTIVE> {HASH}("light"|"indent")           { yybegin(LINE); return MakeToken(PP_LIGHT); }
<PPDIRECTIVE> {HASH}"time"                       { yybegin(LINE); return MakeToken(PP_TIME); }
<PPDIRECTIVE> {HASH}"I"                          { yybegin(LINE); return MakeToken(PP_I); }
<PPDIRECTIVE> {HASH}"nowarn"                     { yybegin(LINE); return MakeToken(PP_NOWARN); }

<PPDIRECTIVE> {HASH}"if"                         { PushBack(yylength()); yybegin(PPSHARP); Clear(); break; }
<PPDIRECTIVE> {HASH}"else"                       { PushBack(yylength()); yybegin(PPSHARP); Clear(); break; }
<PPDIRECTIVE> {HASH}"endif"                      { PushBack(yylength()); yybegin(PPSHARP); Clear(); break; }

<PPDIRECTIVE> {HASH}"if"{TAIL_IDENT}             {
  // TODO: delete this line after fixing the bug: https://github.com/Microsoft/visualfsharp/pull/5498
  PushBack(yylength()); yybegin(PPSHARP); Clear(); break; }
<PPDIRECTIVE> {HASH}"else"{TAIL_IDENT}           {
  // TODO: delete this line after fixing the bug: https://github.com/Microsoft/visualfsharp/pull/5498
  PushBack(yylength()); yybegin(PPSHARP); Clear(); break; }
<PPDIRECTIVE> {HASH}"endif"{TAIL_IDENT}          {
  // TODO: delete this line after fixing the bug: https://github.com/Microsoft/visualfsharp/pull/5498
  PushBack(yylength()); yybegin(PPSHARP); Clear(); break; }

<PPDIRECTIVE> {HASH}{IDENT}                      { yybegin(LINE); return MakeToken(PP_DIRECTIVE); }

<PPSHARP> {HASH}"if"    { yybegin(PPSYMBOL); return MakeToken(PP_IF_SECTION); }
<PPSHARP> {HASH}"else"  { yybegin(PPSYMBOL); return MakeToken(PP_ELSE_SECTION); }
<PPSHARP> {HASH}"endif" { yybegin(PPSYMBOL); return MakeToken(PP_ENDIF); }

<BAD_PPSHARP> {HASH}"if"    { yybegin(PPSYMBOL); return MakeToken(PP_DIRECTIVE); }
<BAD_PPSHARP> {HASH}"else"  { yybegin(PPSYMBOL); return MakeToken(PP_DIRECTIVE); }
<BAD_PPSHARP> {HASH}"endif" { yybegin(PPSYMBOL); return MakeToken(PP_DIRECTIVE); }

<PPSHARP, BAD_PPSHARP> {HASH}"if"{TAIL_IDENT}    {
  // TODO: delete this line after fixing the bug: https://github.com/Microsoft/visualfsharp/pull/5498
  PushBack(yylength() - 3); yybegin(LINE); return MakeToken(PP_DIRECTIVE); }
<PPSHARP, BAD_PPSHARP> {HASH}"else"{TAIL_IDENT}  {
  // TODO: delete this line after fixing the bug: https://github.com/Microsoft/visualfsharp/pull/5498
  PushBack(yylength() - 5); yybegin(PPSYMBOL); return MakeToken(PP_DIRECTIVE); }
<PPSHARP, BAD_PPSHARP> {HASH}"endif"{TAIL_IDENT} {
  // TODO: delete this line after fixing the bug: https://github.com/Microsoft/visualfsharp/pull/5498
  PushBack(yylength() - 6); yybegin(PPSYMBOL); return MakeToken(PP_DIRECTIVE); }

<PPSYMBOL> "||"                    { return MakeToken(PP_OR); }
<PPSYMBOL> "&&"                    { return MakeToken(PP_AND); }
<PPSYMBOL> "!"                     { return MakeToken(PP_NOT); }
<PPSYMBOL> "("                     { return MakeToken(PP_LPAR); }
<PPSYMBOL> ")"                     { return MakeToken(PP_RPAR); }
<PPSYMBOL> {PP_CONDITIONAL_SYMBOL} { return MakeToken(PP_CONDITIONAL_SYMBOL); }

<PPSHARP, PPSYMBOL> {LINE_COMMENT} { return MakeToken(LINE_COMMENT); }
<PPSHARP, PPSYMBOL> {NEW_LINE}     { yybegin(YYINITIAL); return MakeToken(NEW_LINE); }

<PPSYMBOL> . { return MakeToken(PP_BAD_CHARACTER); }

<IN_BLOCK_COMMENT,
 IN_BLOCK_COMMENT_FROM_LINE,
 STRING_IN_COMMENT,
 SMASH_INT_DOT_DOT,
 SMASH_INT_DOT_DOT_FROM_LINE,
 SMASH_RQUOTE_DOT,
 SMASH_RQUOTE_DOT_FROM_LINE,
 SMASH_ADJACENT_LESS_OP,
 SMASH_ADJACENT_GREATER_BAR_RBRACK,
 SMASH_ADJACENT_GREATER_RBRACK,
 SMASH_ADJACENT_GREATER_BAR_RBRACK_FIN,
 SMASH_ADJACENT_GREATER_RBRACK_FIN,
 ADJACENT_TYPE_CLOSE_OP,
 INIT_ADJACENT_TYPE_APP,
 ADJACENT_TYPE_APP,
 SYMBOLIC_OPERATOR,
 GREATER_OP,
 GREATER_OP_SYMBOLIC_OP,
 PRE_LESS_OP,
 LINE> [^] { return MakeToken(BAD_CHARACTER); }
