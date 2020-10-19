using System.Collections;
using JetBrains.Diagnostics;
using JetBrains.Util;
using JetBrains.Text;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using static JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing.FSharpTokenType;

%%

%unicode

%init{
   currTokenType = null;
%init}

%{

%}

%eofval{
  if(yy_lexical_state == IN_BLOCK_COMMENT || yy_lexical_state == IN_BLOCK_COMMENT_FROM_LINE)
  {
    return fillBlockComment(UNFINISHED_BLOCK_COMMENT);
  }
  else
    return makeToken(null);
%eofval}

%namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing.Lexing
%class FSharpLexerGenerated
%public
%implements IIncrementalLexer
%function _locateToken
%virtual
%type TokenNodeType

%include PsiTasks/Unicode.lex

// Unfortunately, this rule cannot be shared with the frontend
OP_CHAR=([!%&*+\-./<=>@^|~\?])

%include FSharpRules.lex

<LINE, ADJACENT_TYAPP> {LET_BANG}            { return makeToken(LET_BANG); }
<LINE, ADJACENT_TYAPP> {USE_BANG}            { return makeToken(USE_BANG); }
<LINE, ADJACENT_TYAPP> {DO_BANG}             { return makeToken(DO_BANG); }
<LINE, ADJACENT_TYAPP> {YIELD_BANG}          { return makeToken(YIELD_BANG); }
<LINE, ADJACENT_TYAPP> {RETURN_BANG}         { return makeToken(RETURN_BANG); }
<LINE, ADJACENT_TYAPP> {MATCH_BANG}          { return makeToken(MATCH_BANG); }
<LINE, ADJACENT_TYAPP> {AND_BANG}            { return makeToken(AND_BANG); }
<LINE, ADJACENT_TYAPP> {BAR}                 { return makeToken(BAR); }
<LINE, ADJACENT_TYAPP> {LARROW}              { return makeToken(LARROW); }
<LINE, ADJACENT_TYAPP> {LPAREN}              { return makeToken(LPAREN); }
<LINE, ADJACENT_TYAPP> {RPAREN}              { return makeToken(RPAREN); }
<LINE, ADJACENT_TYAPP> {LBRACK}              { return makeToken(LBRACK); }
<LINE, ADJACENT_TYAPP> {RBRACK}              { return makeToken(RBRACK); }
<LINE, ADJACENT_TYAPP> {LBRACK_LESS}         { return makeToken(LBRACK_LESS); }
<LINE, ADJACENT_TYAPP> {GREATER_RBRACK}      { return makeToken(GREATER_RBRACK); }
<LINE, ADJACENT_TYAPP> {LBRACK_BAR}          { return makeToken(LBRACK_BAR); }
<LINE, ADJACENT_TYAPP> {LESS}                { return makeToken(LESS); }
<LINE, ADJACENT_TYAPP> {GREATER}             { return makeToken(GREATER); }
<LINE, ADJACENT_TYAPP> {BAR_RBRACK}          { return makeToken(BAR_RBRACK); }
<LINE, ADJACENT_TYAPP> {BAR_RBRACE}          { return makeToken(BAR_RBRACE); }
<LINE, ADJACENT_TYAPP> {LBRACE}              { return makeToken(LBRACE); }
<LINE, ADJACENT_TYAPP> {RBRACE}              { return makeToken(RBRACE); }
<LINE, ADJACENT_TYAPP> {GREATER_BAR_RBRACK}  { return makeToken(GREATER_BAR_RBRACK); }
<LINE, ADJACENT_TYAPP> {COLON_QMARK_GREATER} { return makeToken(COLON_QMARK_GREATER); }
<LINE, ADJACENT_TYAPP> {COLON_QMARK}         { return makeToken(COLON_QMARK); }
<LINE, ADJACENT_TYAPP> {COLON_COLON}         { return makeToken(COLON_COLON); }
<LINE, ADJACENT_TYAPP> {COLON_EQUALS}        { return makeToken(COLON_EQUALS); }
<LINE, ADJACENT_TYAPP> {SEMICOLON_SEMICOLON} { return makeToken(SEMICOLON_SEMICOLON); }
<LINE, ADJACENT_TYAPP> {SEMICOLON}           { return makeToken(SEMICOLON); }
<LINE, ADJACENT_TYAPP> {QMARK}               { return makeToken(QMARK); }
<LINE, ADJACENT_TYAPP> {QMARK_QMARK}         { return makeToken(QMARK_QMARK); }
<LINE, ADJACENT_TYAPP> {LPAREN_STAR_RPAREN}  { return makeToken(LPAREN_STAR_RPAREN); }
<LINE, ADJACENT_TYAPP> {PLUS}                { return makeToken(PLUS); }
<LINE, ADJACENT_TYAPP> {DOLLAR}              { return makeToken(DOLLAR); }
<LINE, ADJACENT_TYAPP> {PERCENT}             { return makeToken(PERCENT); }
<LINE, ADJACENT_TYAPP> {PERCENT_PERCENT}     { return makeToken(PERCENT_PERCENT); }
<LINE, ADJACENT_TYAPP> {AMP}                 { return makeToken(AMP); }
<LINE, ADJACENT_TYAPP> {AMP_AMP}             { return makeToken(AMP_AMP); }

<INIT_ADJACENT_TYAPP> {LET_BANG}            { yybegin(LINE); return makeToken(LET_BANG); }
<INIT_ADJACENT_TYAPP> {USE_BANG}            { yybegin(LINE); return makeToken(USE_BANG); }
<INIT_ADJACENT_TYAPP> {DO_BANG}             { yybegin(LINE); return makeToken(DO_BANG); }
<INIT_ADJACENT_TYAPP> {YIELD_BANG}          { yybegin(LINE); return makeToken(YIELD_BANG); }
<INIT_ADJACENT_TYAPP> {RETURN_BANG}         { yybegin(LINE); return makeToken(RETURN_BANG); }
<INIT_ADJACENT_TYAPP> {MATCH_BANG}          { yybegin(LINE); return makeToken(MATCH_BANG); }
<INIT_ADJACENT_TYAPP> {AND_BANG}            { yybegin(LINE); return makeToken(AND_BANG); }
<INIT_ADJACENT_TYAPP> {BAR}                 { yybegin(LINE); return makeToken(BAR); }
<INIT_ADJACENT_TYAPP> {LARROW}              { yybegin(LINE); return makeToken(LARROW); }
<INIT_ADJACENT_TYAPP> {LBRACK_BAR}          { yybegin(LINE); return makeToken(LBRACK_BAR); }
<INIT_ADJACENT_TYAPP> {BAR_RBRACK}          { yybegin(LINE); return makeToken(BAR_RBRACK); }
<INIT_ADJACENT_TYAPP> {LBRACE}              { yybegin(LINE); return makeToken(LBRACE); }
<INIT_ADJACENT_TYAPP> {RBRACE}              { yybegin(LINE); return makeToken(RBRACE); }
<INIT_ADJACENT_TYAPP> {COLON_QMARK_GREATER} { yybegin(LINE); return makeToken(COLON_QMARK_GREATER); }
<INIT_ADJACENT_TYAPP> {COLON_QMARK}         { yybegin(LINE); return makeToken(COLON_QMARK); }
<INIT_ADJACENT_TYAPP> {COLON_COLON}         { yybegin(LINE); return makeToken(COLON_COLON); }
<INIT_ADJACENT_TYAPP> {COLON_EQUALS}        { yybegin(LINE); return makeToken(COLON_EQUALS); }
<INIT_ADJACENT_TYAPP> {SEMICOLON_SEMICOLON} { yybegin(LINE); return makeToken(SEMICOLON_SEMICOLON); }
<INIT_ADJACENT_TYAPP> {QMARK}               { yybegin(LINE); return makeToken(QMARK); }
<INIT_ADJACENT_TYAPP> {QMARK_QMARK}         { yybegin(LINE); return makeToken(QMARK_QMARK); }
<INIT_ADJACENT_TYAPP> {LPAREN_STAR_RPAREN}  { yybegin(LINE); return makeToken(LPAREN_STAR_RPAREN); }
<INIT_ADJACENT_TYAPP> {PLUS}                { yybegin(LINE); return makeToken(PLUS); }
<INIT_ADJACENT_TYAPP> {DOLLAR}              { yybegin(LINE); return makeToken(DOLLAR); }
<INIT_ADJACENT_TYAPP> {PERCENT}             { yybegin(LINE); return makeToken(PERCENT); }
<INIT_ADJACENT_TYAPP> {PERCENT_PERCENT}     { yybegin(LINE); return makeToken(PERCENT_PERCENT); }
<INIT_ADJACENT_TYAPP> {AMP}                 { yybegin(LINE); return makeToken(AMP); }
<INIT_ADJACENT_TYAPP> {AMP_AMP}             { yybegin(LINE); return makeToken(AMP_AMP); }

<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {LBRACE_BAR}    { return makeToken(LBRACE_BAR); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {BAR_RBRACE}    { return makeToken(BAR_RBRACE); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {SEMICOLON}     { return makeToken(SEMICOLON); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {RARROW}        { return makeToken(RARROW); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {DOT}           { return makeToken(DOT); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {COLON}         { return makeToken(COLON); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {STAR}          { return makeToken(STAR); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {QUOTE}         { return makeToken(QUOTE); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {HASH}          { return makeToken(HASH); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {COLON_GREATER} { return makeToken(COLON_GREATER); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {DOT_DOT}       { return makeToken(DOT_DOT); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {EQUALS}        { return makeToken(EQUALS); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {UNDERSCORE}    { return makeToken(UNDERSCORE); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {MINUS}         { return makeToken(MINUS); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {COMMA}         { return makeToken(COMMA); }

<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {KEYWORD_STRING_SOURCE_DIRECTORY} { return makeToken(KEYWORD_STRING_SOURCE_DIRECTORY); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {KEYWORD_STRING_SOURCE_FILE}      { return makeToken(KEYWORD_STRING_SOURCE_FILE); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {KEYWORD_STRING_LINE}             { return makeToken(KEYWORD_STRING_LINE); }

<LINE, ADJACENT_TYAPP> {RESERVED_IDENT_FORMATS} { return makeToken(RESERVED_IDENT_FORMATS); }

<LINE> "delegate"{LESS_OP}                 { initAdjacentTypeApp(); return makeToken(DELEGATE); }
<LINE> {IDENT}{LESS_OP}                    { initAdjacentTypeApp(); return identInTypeApp(); }
<LINE> {IEEE32}{LESS_OP}                   { initAdjacentTypeApp(); return makeToken(IEEE32); }
<LINE> {IEEE64}{LESS_OP}                   { initAdjacentTypeApp(); return makeToken(IEEE64); }
<LINE> {DECIMAL}{LESS_OP}                  { initAdjacentTypeApp(); return makeToken(DECIMAL); }
<LINE> {BYTE}{LESS_OP}                     { initAdjacentTypeApp(); return makeToken(BYTE); }
<LINE> {INT16}{LESS_OP}                    { initAdjacentTypeApp(); return makeToken(INT16); }
<LINE> ({XINT}|{INT}){LESS_OP}             { initAdjacentTypeApp(); return makeToken(INT32); }
<LINE> {INT32}{LESS_OP}                    { initAdjacentTypeApp(); return makeToken(INT32); }
<LINE> {INT64}{LESS_OP}                    { initAdjacentTypeApp(); return makeToken(INT64); }
<LINE> {SBYTE}{LESS_OP}                    { initAdjacentTypeApp(); return makeToken(SBYTE); }
<LINE> {UINT16}{LESS_OP}                   { initAdjacentTypeApp(); return makeToken(UINT16); }
<LINE> {UINT32}{LESS_OP}                   { initAdjacentTypeApp(); return makeToken(UINT32); }
<LINE> {UINT64}{LESS_OP}                   { initAdjacentTypeApp(); return makeToken(UINT64); }
<LINE> {BIGNUM}{LESS_OP}                   { initAdjacentTypeApp(); return makeToken(BIGNUM); }
<LINE> {NATIVEINT}{LESS_OP}                { initAdjacentTypeApp(); return makeToken(NATIVEINT); }
<LINE> {RESERVED_LITERAL_FORMATS}{LESS_OP} { initAdjacentTypeApp(); return makeToken(RESERVED_LITERAL_FORMATS); }

<LINE> "delegate"{LESS_OP}{BAD_SYMBOLIC_OP}                 { yypushback(yylength()); yybegin(PRE_LESS_OP); clear(); break; }
<LINE> {IDENT}{LESS_OP}{BAD_SYMBOLIC_OP}                    { yypushback(yylength()); yybegin(PRE_LESS_OP); clear(); break; }
<LINE> {IEEE32}{LESS_OP}{BAD_SYMBOLIC_OP}                   { yypushback(yylength()); yybegin(PRE_LESS_OP); clear(); break; }
<LINE> {IEEE64}{LESS_OP}{BAD_SYMBOLIC_OP}                   { yypushback(yylength()); yybegin(PRE_LESS_OP); clear(); break; }
<LINE> {DECIMAL}{LESS_OP}{BAD_SYMBOLIC_OP}                  { yypushback(yylength()); yybegin(PRE_LESS_OP); clear(); break; }
<LINE> {BYTE}{LESS_OP}{BAD_SYMBOLIC_OP}                     { yypushback(yylength()); yybegin(PRE_LESS_OP); clear(); break; }
<LINE> {INT16}{LESS_OP}{BAD_SYMBOLIC_OP}                    { yypushback(yylength()); yybegin(PRE_LESS_OP); clear(); break; }
<LINE> ({XINT}|{INT}){LESS_OP}{BAD_SYMBOLIC_OP}             { yypushback(yylength()); yybegin(PRE_LESS_OP); clear(); break; }
<LINE> {INT32}{LESS_OP}{BAD_SYMBOLIC_OP}                    { yypushback(yylength()); yybegin(PRE_LESS_OP); clear(); break; }
<LINE> {INT64}{LESS_OP}{BAD_SYMBOLIC_OP}                    { yypushback(yylength()); yybegin(PRE_LESS_OP); clear(); break; }
<LINE> {SBYTE}{LESS_OP}{BAD_SYMBOLIC_OP}                    { yypushback(yylength()); yybegin(PRE_LESS_OP); clear(); break; }
<LINE> {UINT16}{LESS_OP}{BAD_SYMBOLIC_OP}                   { yypushback(yylength()); yybegin(PRE_LESS_OP); clear(); break; }
<LINE> {UINT32}{LESS_OP}{BAD_SYMBOLIC_OP}                   { yypushback(yylength()); yybegin(PRE_LESS_OP); clear(); break; }
<LINE> {UINT64}{LESS_OP}{BAD_SYMBOLIC_OP}                   { yypushback(yylength()); yybegin(PRE_LESS_OP); clear(); break; }
<LINE> {BIGNUM}{LESS_OP}{BAD_SYMBOLIC_OP}                   { yypushback(yylength()); yybegin(PRE_LESS_OP); clear(); break; }
<LINE> {NATIVEINT}{LESS_OP}{BAD_SYMBOLIC_OP}                { yypushback(yylength()); yybegin(PRE_LESS_OP); clear(); break; }
<LINE> {RESERVED_LITERAL_FORMATS}{LESS_OP}{BAD_SYMBOLIC_OP} { yypushback(yylength()); yybegin(PRE_LESS_OP); clear(); break; }

<PRE_LESS_OP> "delegate"                 { yybegin(LINE); return makeToken(DELEGATE); }
<PRE_LESS_OP> {IDENT}                    { yybegin(LINE); return identInTypeApp(); }
<PRE_LESS_OP> {IEEE32}                   { yybegin(LINE); return makeToken(IEEE32); }
<PRE_LESS_OP> {IEEE64}                   { yybegin(LINE); return makeToken(IEEE64); }
<PRE_LESS_OP> {DECIMAL}                  { yybegin(LINE); return makeToken(DECIMAL); }
<PRE_LESS_OP> {BYTE}                     { yybegin(LINE); return makeToken(BYTE); }
<PRE_LESS_OP> {INT16}                    { yybegin(LINE); return makeToken(INT16); }
<PRE_LESS_OP> {XINT}|{INT}               { yybegin(LINE); return makeToken(INT32); }
<PRE_LESS_OP> {INT32}                    { yybegin(LINE); return makeToken(INT32); }
<PRE_LESS_OP> {INT64}                    { yybegin(LINE); return makeToken(INT64); }
<PRE_LESS_OP> {SBYTE}                    { yybegin(LINE); return makeToken(SBYTE); }
<PRE_LESS_OP> {UINT16}                   { yybegin(LINE); return makeToken(UINT16); }
<PRE_LESS_OP> {UINT32}                   { yybegin(LINE); return makeToken(UINT32); }
<PRE_LESS_OP> {UINT64}                   { yybegin(LINE); return makeToken(UINT64); }
<PRE_LESS_OP> {BIGNUM}                   { yybegin(LINE); return makeToken(BIGNUM); }
<PRE_LESS_OP> {NATIVEINT}                { yybegin(LINE); return makeToken(NATIVEINT); }
<PRE_LESS_OP> {RESERVED_LITERAL_FORMATS} { yybegin(LINE); return makeToken(RESERVED_LITERAL_FORMATS); }

<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {IDENT}      { return initIdent(); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {SBYTE}      { return makeToken(SBYTE); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {BYTE}       { return makeToken(BYTE); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {INT16}      { return makeToken(INT16); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {UINT16}     { return makeToken(UINT16); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {XINT}|{INT} { return makeToken(INT32); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {INT32}      { return makeToken(INT32); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {UINT32}     { return makeToken(UINT32); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {NATIVEINT}  { return makeToken(NATIVEINT); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {UNATIVEINT} { return makeToken(UNATIVEINT); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {INT64}      { return makeToken(INT64); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {UINT64}     { return makeToken(UINT64); }

<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {INT}\.\. { yypushback(yylength()); initSmash(SMASH_INT_DOT_DOT_FROM_LINE, SMASH_INT_DOT_DOT); clear(); break; }

<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {IEEE32}  { return makeToken(IEEE32); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {IEEE64}  { return makeToken(IEEE64); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {BIGNUM}  { return makeToken(BIGNUM); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {DECIMAL} { return makeToken(DECIMAL); }

<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {CHARACTER_LITERAL}               { return makeToken(CHARACTER_LITERAL); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {STRING}                          { return makeToken(STRING); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {UNFINISHED_STRING}               { return makeToken(UNFINISHED_STRING); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {VERBATIM_STRING}                 { return makeToken(VERBATIM_STRING); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {UNFINISHED_VERBATIM_STRING}      { return makeToken(UNFINISHED_VERBATIM_STRING); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {TRIPLE_QUOTED_STRING}            { return makeToken(TRIPLE_QUOTED_STRING); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {UNFINISHED_TRIPLE_QUOTED_STRING} { return makeToken(UNFINISHED_TRIPLE_QUOTED_STRING); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {BYTEARRAY}                       { return makeToken(BYTEARRAY); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {VERBATIM_BYTEARRAY}              { return makeToken(VERBATIM_BYTEARRAY); }
<LINE, ADJACENT_TYAPP, INIT_ADJACENT_TYAPP> {BYTECHAR}                        { return makeToken(BYTECHAR); }

<LINE, ADJACENT_TYAPP> {RESERVED_SYMBOLIC_SEQUENCE} { return makeToken(RESERVED_SYMBOLIC_SEQUENCE); }
<LINE, ADJACENT_TYAPP> {RESERVED_LITERAL_FORMATS}   { return makeToken(RESERVED_LITERAL_FORMATS); }
<LINE, ADJACENT_TYAPP> {SYMBOLIC_OP}                { return makeToken(SYMBOLIC_OP); }
<LINE, ADJACENT_TYAPP> {BAD_SYMBOLIC_OP}            { return makeToken(BAD_SYMBOLIC_OP); }

<INIT_ADJACENT_TYAPP> {RESERVED_SYMBOLIC_SEQUENCE} { yybegin(LINE); return makeToken(RESERVED_SYMBOLIC_SEQUENCE); }
<INIT_ADJACENT_TYAPP> {RESERVED_LITERAL_FORMATS}   { yybegin(LINE); return makeToken(RESERVED_LITERAL_FORMATS); }
<INIT_ADJACENT_TYAPP> {SYMBOLIC_OP}                { yybegin(LINE); return makeToken(SYMBOLIC_OP); }
<INIT_ADJACENT_TYAPP> {BAD_SYMBOLIC_OP}            { yybegin(LINE); return makeToken(BAD_SYMBOLIC_OP); }

<SMASH_INT_DOT_DOT, SMASH_INT_DOT_DOT_FROM_LINE> {INT} { return makeToken(INT32); }
<SMASH_INT_DOT_DOT, SMASH_INT_DOT_DOT_FROM_LINE> \.\.  { exitSmash(SMASH_INT_DOT_DOT_FROM_LINE); return makeToken(DOT_DOT); }

<SMASH_RQUOTE_DOT, SMASH_RQUOTE_DOT_FROM_LINE> "@>"  { return makeToken(RQUOTE_TYPED); }
<SMASH_RQUOTE_DOT, SMASH_RQUOTE_DOT_FROM_LINE> "@@>" { return makeToken(RQUOTE_UNTYPED); }
<SMASH_RQUOTE_DOT, SMASH_RQUOTE_DOT_FROM_LINE> \.    { exitSmash(SMASH_RQUOTE_DOT_FROM_LINE); return makeToken(DOT); }

<STRING_IN_COMMENT> {STRING}                          { yybegin(IN_BLOCK_COMMENT); increaseTokenLength(yylength()); clear(); break; }
<STRING_IN_COMMENT> {VERBATIM_STRING}                 { yybegin(IN_BLOCK_COMMENT); increaseTokenLength(yylength()); clear(); break; }
<STRING_IN_COMMENT> {TRIPLE_QUOTED_STRING}            { yybegin(IN_BLOCK_COMMENT); increaseTokenLength(yylength()); clear(); break; }
<STRING_IN_COMMENT> {UNFINISHED_STRING}               { return fillBlockComment(UNFINISHED_STRING_IN_COMMENT); }
<STRING_IN_COMMENT> {UNFINISHED_VERBATIM_STRING}      { return fillBlockComment(UNFINISHED_VERBATIM_STRING_IN_COMMENT); }
<STRING_IN_COMMENT> {UNFINISHED_TRIPLE_QUOTED_STRING} { return fillBlockComment(UNFINISHED_TRIPLE_QUOTED_STRING_IN_COMMENT); }

<IN_BLOCK_COMMENT, IN_BLOCK_COMMENT_FROM_LINE> "(*"      { zzNestedCommentLevel++; increaseTokenLength(yylength()); clear(); break; }
<IN_BLOCK_COMMENT, IN_BLOCK_COMMENT_FROM_LINE> "*)"      { if (--zzNestedCommentLevel == 0) return fillBlockComment(BLOCK_COMMENT); increaseTokenLength(yylength()); clear(); break; }
<IN_BLOCK_COMMENT, IN_BLOCK_COMMENT_FROM_LINE> \"        { yypushback(yylength()); yybegin(STRING_IN_COMMENT); clear(); break; }
<IN_BLOCK_COMMENT, IN_BLOCK_COMMENT_FROM_LINE> @\"       { yypushback(yylength()); yybegin(STRING_IN_COMMENT); clear(); break; }
<IN_BLOCK_COMMENT, IN_BLOCK_COMMENT_FROM_LINE> \"\"\"    { yypushback(yylength()); yybegin(STRING_IN_COMMENT); clear(); break; }
<IN_BLOCK_COMMENT, IN_BLOCK_COMMENT_FROM_LINE> [^(\"@*]+ { increaseTokenLength(yylength()); clear(); break; }
<IN_BLOCK_COMMENT, IN_BLOCK_COMMENT_FROM_LINE> [^]|"(*)" { increaseTokenLength(yylength()); clear(); break; }
<IN_BLOCK_COMMENT, IN_BLOCK_COMMENT_FROM_LINE> <<EOF>>   { return fillBlockComment(UNFINISHED_BLOCK_COMMENT); }

<SMASH_ADJACENT_LESS_OP> {LESS} { return makeToken(LESS); }
<SMASH_ADJACENT_LESS_OP> "/"    { riseFromParenLevel(0); return makeToken(SYMBOLIC_OP); }

<SMASH_ADJACENT_GREATER_BAR_RBRACK, SMASH_ADJACENT_GREATER_BAR_RBRACK_FIN> {GREATER}    { return makeToken(GREATER); }
<SMASH_ADJACENT_GREATER_BAR_RBRACK, SMASH_ADJACENT_GREATER_BAR_RBRACK_FIN> {BAR_RBRACK} { exitSmash(SMASH_ADJACENT_GREATER_BAR_RBRACK_FIN); return makeToken(BAR_RBRACK); }

<SMASH_ADJACENT_GREATER_RBRACK, SMASH_ADJACENT_GREATER_RBRACK_FIN> {GREATER} { return makeToken(GREATER); }
<SMASH_ADJACENT_GREATER_RBRACK, SMASH_ADJACENT_GREATER_RBRACK_FIN> {RBRACK}  { exitSmash(SMASH_ADJACENT_GREATER_RBRACK_FIN); return makeToken(RBRACK); }

<PPSHARP, PPSYMBOL, PPDIRECTIVE> {ANYWHITE} { return makeToken (WHITESPACE); }

<PPDIRECTIVE> {HASH}("l"|"load")                 { yybegin(LINE); return makeToken(PP_LOAD); }
<PPDIRECTIVE> {HASH}("r"|"reference")            { yybegin(LINE); return makeToken(PP_REFERENCE); }
<PPDIRECTIVE> {HASH}("line"|({ANYWHITE})*[0-9]+) { yybegin(LINE); return makeToken(PP_LINE); }
<PPDIRECTIVE> {HASH}"help"                       { yybegin(LINE); return makeToken(PP_HELP); }
<PPDIRECTIVE> {HASH}"quit"                       { yybegin(LINE); return makeToken(PP_QUIT); }
<PPDIRECTIVE> {HASH}("light"|"indent")           { yybegin(LINE); return makeToken(PP_LIGHT); }
<PPDIRECTIVE> {HASH}"time"                       { yybegin(LINE); return makeToken(PP_TIME); }
<PPDIRECTIVE> {HASH}"I"                          { yybegin(LINE); return makeToken(PP_I); }
<PPDIRECTIVE> {HASH}"nowarn"                     { yybegin(LINE); return makeToken(PP_NOWARN); }

<PPDIRECTIVE> {HASH}"if"                         { yypushback(yylength()); yybegin(PPSHARP); clear(); break; }
<PPDIRECTIVE> {HASH}"else"                       { yypushback(yylength()); yybegin(PPSHARP); clear(); break; }
<PPDIRECTIVE> {HASH}"endif"                      { yypushback(yylength()); yybegin(PPSHARP); clear(); break; }

<PPDIRECTIVE> {HASH}"if"{TAIL_IDENT}             { yypushback(yylength()); yybegin(PPSHARP); clear(); break; }
<PPDIRECTIVE> {HASH}"else"{TAIL_IDENT}           { yypushback(yylength()); yybegin(PPSHARP); clear(); break; }
<PPDIRECTIVE> {HASH}"endif"{TAIL_IDENT}          { yypushback(yylength()); yybegin(PPSHARP); clear(); break; }

<PPDIRECTIVE> {HASH}{IDENT}                      { yybegin(LINE); return makeToken(PP_DIRECTIVE); }

<PPSHARP> {HASH}"if"    { yybegin(PPSYMBOL); return makeToken(PP_IF_SECTION); }
<PPSHARP> {HASH}"else"  { yybegin(PPSYMBOL); return makeToken(PP_ELSE_SECTION); }
<PPSHARP> {HASH}"endif" { yybegin(PPSYMBOL); return makeToken(PP_ENDIF); }

<BAD_PPSHARP> {HASH}"if"    { yybegin(PPSYMBOL); return makeToken(PP_DIRECTIVE); }
<BAD_PPSHARP> {HASH}"else"  { yybegin(PPSYMBOL); return makeToken(PP_DIRECTIVE); }
<BAD_PPSHARP> {HASH}"endif" { yybegin(PPSYMBOL); return makeToken(PP_DIRECTIVE); }

<PPSHARP, BAD_PPSHARP> {HASH}"if"{TAIL_IDENT}    { yypushback(yylength() - 3); yybegin(LINE); return makeToken(PP_DIRECTIVE); }
<PPSHARP, BAD_PPSHARP> {HASH}"else"{TAIL_IDENT}  { yypushback(yylength() - 5); yybegin(PPSYMBOL); return makeToken(PP_DIRECTIVE); }
<PPSHARP, BAD_PPSHARP> {HASH}"endif"{TAIL_IDENT} { yypushback(yylength() - 6); yybegin(PPSYMBOL); return makeToken(PP_DIRECTIVE); }

<PPSYMBOL> "||"                    { return makeToken(PP_OR); }
<PPSYMBOL> "&&"                    { return makeToken(PP_AND); }
<PPSYMBOL> "!"                     { return makeToken(PP_NOT); }
<PPSYMBOL> "("                     { return makeToken(PP_LPAR); }
<PPSYMBOL> ")"                     { return makeToken(PP_RPAR); }
<PPSYMBOL> {PP_CONDITIONAL_SYMBOL} { return makeToken(PP_CONDITIONAL_SYMBOL); }

<PPSHARP, PPSYMBOL> {LINE_COMMENT} { return makeToken(LINE_COMMENT); }
<PPSHARP, PPSYMBOL> {NEW_LINE}     { yybegin(YYINITIAL); return makeToken(NEW_LINE); }

<PPSYMBOL> . { return makeToken(PP_BAD_CHARACTER); }

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
 INIT_ADJACENT_TYAPP,
 ADJACENT_TYAPP,
 SYMBOLIC_OPERATOR,
 GREATER_OP,
 GREATER_OP_SYMBOLIC_OP,
 PRE_LESS_OP,
 LINE> [^] { return makeToken(BAD_CHARACTER); }
