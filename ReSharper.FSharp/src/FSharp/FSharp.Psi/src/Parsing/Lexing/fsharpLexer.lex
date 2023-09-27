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
   myCurrentTokenType = null;
%init}

%{
%}

%eofval{
  if (yy_lexical_state == IN_BLOCK_COMMENT || yy_lexical_state == IN_BLOCK_COMMENT_FROM_LINE)
    return FillBlockComment();
  else
    return MakeToken(null);
%eofval}

%namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing.Lexing
%class FSharpLexerGenerated
%public
%implements IIncrementalLexer
%function _locateToken
%virtual
%type TokenNodeType

%include Unicode.lex

// Unfortunately, this rule cannot be shared with the frontend
OP_CHAR=([!%&*+\-./<=>@^|~\?])

%include FSharpRules.lex
