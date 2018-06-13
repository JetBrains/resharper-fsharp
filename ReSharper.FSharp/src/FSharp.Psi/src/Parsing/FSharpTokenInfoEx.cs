using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Parsing;
using Microsoft.FSharp.Compiler;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
{
  
  public static class FSharpTokenInfoEx
  {
    // todo: check lquote/rquote separately for typed vs untyped
    private const int LQuoteTag = Parser.token.Tags.LQUOTE;
    private const int RQuoteTag = Parser.token.Tags.RQUOTE;

    public static TokenNodeType GetTokenType(this FSharpToken token)
    {
      var tokenTag = token.Token.Tag;
      if (ourTokenTypesByTags.TryGetValue(tokenTag, out var tokenType))
        return tokenType;

      if (tokenTag == Parser.token.Tags.PLUS_MINUS_OP)
      {
        var plusToken = (Parser.token.PLUS_MINUS_OP) token.Token;
        if (plusToken.Item == "+")
          return FSharpTokenType.PLUS;
      }
      
      var tokenInfo = token.TokenInfo;
      if (tokenInfo.Item1 == FSharpTokenColorKind.PreprocessorKeyword)
        return FSharpTokenType.OTHER_KEYWORD;

      switch (tokenInfo.Item2)
      {
        case FSharpTokenCharKind.Keyword:
          return FSharpTokenType.OTHER_KEYWORD;

        case FSharpTokenCharKind.Identifier:
          return FSharpTokenType.IDENTIFIER;

        case FSharpTokenCharKind.String:
          if (tokenTag == Parser.token.Tags.CHAR)
            return FSharpTokenType.CHAR;
          return FSharpTokenType.STRING;

        case FSharpTokenCharKind.Literal:
          return FSharpTokenType.LITERAL;

        case FSharpTokenCharKind.Operator:
          return FSharpTokenType.OPERATOR;

        case FSharpTokenCharKind.LineComment:
          return FSharpTokenType.LINE_COMMENT;
        case FSharpTokenCharKind.Comment:
          return FSharpTokenType.COMMENT;

        case FSharpTokenCharKind.WhiteSpace:
          return FSharpTokenType.WHITESPACE;

        case FSharpTokenCharKind.Delimiter:
          if (tokenTag == LQuoteTag)
            return token.FullMatchedLength == 2
              ? FSharpTokenType.LQUOTE_TYPED
              : FSharpTokenType.LQUOTE_UNTYPED;

          if (tokenTag == RQuoteTag)
            return token.FullMatchedLength == 2
              ? FSharpTokenType.RQUOTE_TYPED
              : FSharpTokenType.RQUOTE_UNTYPED;

          return FSharpTokenType.TEXT;

        default:
          return FSharpTokenType.TEXT;
      }
    }

    private static readonly Dictionary<int, TokenNodeType> ourTokenTypesByTags =
      new Dictionary<int, TokenNodeType>
      {
        {Parser.token.Tags.ABSTRACT, FSharpTokenType.ABSTRACT},
        {Parser.token.Tags.AND, FSharpTokenType.AND},
        {Parser.token.Tags.AS, FSharpTokenType.AS},
        {Parser.token.Tags.ASR, FSharpTokenType.ASR},
        {Parser.token.Tags.ASSERT, FSharpTokenType.ASSERT},
        {Parser.token.Tags.BASE, FSharpTokenType.BASE},
        {Parser.token.Tags.BEGIN, FSharpTokenType.BEGIN},
        {Parser.token.Tags.CLASS, FSharpTokenType.CLASS},
        {Parser.token.Tags.DEFAULT, FSharpTokenType.DEFAULT},
        {Parser.token.Tags.DELEGATE, FSharpTokenType.DELEGATE},
        {Parser.token.Tags.DO, FSharpTokenType.DO},
        {Parser.token.Tags.DO_BANG, FSharpTokenType.DO_BANG},
        {Parser.token.Tags.DONE, FSharpTokenType.DONE},
        {Parser.token.Tags.DOWNCAST, FSharpTokenType.DOWNCAST},
        {Parser.token.Tags.DOWNTO, FSharpTokenType.DOWNTO},
        {Parser.token.Tags.ELIF, FSharpTokenType.ELIF},
        {Parser.token.Tags.ELSE, FSharpTokenType.ELSE},
        {Parser.token.Tags.END, FSharpTokenType.END},
        {Parser.token.Tags.EXCEPTION, FSharpTokenType.EXCEPTION},
        {Parser.token.Tags.EXTERN, FSharpTokenType.EXTERN},
        {Parser.token.Tags.FALSE, FSharpTokenType.FALSE},
        {Parser.token.Tags.FINALLY, FSharpTokenType.FINALLY},
        {Parser.token.Tags.FIXED, FSharpTokenType.FIXED},
        {Parser.token.Tags.FOR, FSharpTokenType.FOR},
        {Parser.token.Tags.FUN, FSharpTokenType.FUN},
        {Parser.token.Tags.FUNCTION, FSharpTokenType.FUNCTION},
        {Parser.token.Tags.GLOBAL, FSharpTokenType.GLOBAL},
        {Parser.token.Tags.IF, FSharpTokenType.IF},
        {Parser.token.Tags.IN, FSharpTokenType.IN},
        {Parser.token.Tags.INHERIT, FSharpTokenType.INHERIT},
        {Parser.token.Tags.INLINE, FSharpTokenType.INLINE},
        {Parser.token.Tags.INTERFACE, FSharpTokenType.INTERFACE},
        {Parser.token.Tags.INTERNAL, FSharpTokenType.INTERNAL},
        {Parser.token.Tags.LAZY, FSharpTokenType.LAZY},
        {Parser.token.Tags.MATCH, FSharpTokenType.MATCH},
        {Parser.token.Tags.MATCH_BANG, FSharpTokenType.MATCH_BANG},
        {Parser.token.Tags.MEMBER, FSharpTokenType.MEMBER},
        {Parser.token.Tags.MODULE, FSharpTokenType.MODULE},
        {Parser.token.Tags.MUTABLE, FSharpTokenType.MUTABLE},
        {Parser.token.Tags.NAMESPACE, FSharpTokenType.NAMESPACE},
        {Parser.token.Tags.NEW, FSharpTokenType.NEW},
        {Parser.token.Tags.NULL, FSharpTokenType.NULL},
        {Parser.token.Tags.OF, FSharpTokenType.OF},
        {Parser.token.Tags.OPEN, FSharpTokenType.OPEN},
        {Parser.token.Tags.OR, FSharpTokenType.OR},
        {Parser.token.Tags.OVERRIDE, FSharpTokenType.OVERRIDE},
        {Parser.token.Tags.PRIVATE, FSharpTokenType.PRIVATE},
        {Parser.token.Tags.PUBLIC, FSharpTokenType.PUBLIC},
        {Parser.token.Tags.REC, FSharpTokenType.REC},
        {Parser.token.Tags.SIG, FSharpTokenType.SIG}, // ml compatibility?
        {Parser.token.Tags.STATIC, FSharpTokenType.STATIC},
        {Parser.token.Tags.STRUCT, FSharpTokenType.STRUCT},
        {Parser.token.Tags.THEN, FSharpTokenType.THEN},
        {Parser.token.Tags.TO, FSharpTokenType.TO},
        {Parser.token.Tags.TRUE, FSharpTokenType.TRUE},
        {Parser.token.Tags.TRY, FSharpTokenType.TRY},
        {Parser.token.Tags.TYPE, FSharpTokenType.TYPE},
        {Parser.token.Tags.UPCAST, FSharpTokenType.UPCAST},
        {Parser.token.Tags.VAL, FSharpTokenType.VAL},
        {Parser.token.Tags.VOID, FSharpTokenType.VOID},
        {Parser.token.Tags.WHEN, FSharpTokenType.WHEN},
        {Parser.token.Tags.WHILE, FSharpTokenType.WHILE},
        {Parser.token.Tags.WITH, FSharpTokenType.WITH},

        {Parser.token.Tags.LPAREN, FSharpTokenType.LPAREN},
        {Parser.token.Tags.RPAREN, FSharpTokenType.RPAREN},
        {Parser.token.Tags.LBRACE, FSharpTokenType.LBRACE},
        {Parser.token.Tags.RBRACE, FSharpTokenType.RBRACE},
        {Parser.token.Tags.LBRACK, FSharpTokenType.LBRACK},
        {Parser.token.Tags.RBRACK, FSharpTokenType.RBRACK},
        {Parser.token.Tags.GREATER_RBRACK, FSharpTokenType.GREATER_RBRACK},
        {Parser.token.Tags.LBRACK_LESS, FSharpTokenType.LBRACK_LESS},
        {Parser.token.Tags.LBRACK_BAR, FSharpTokenType.LBRACK_BAR},
        {Parser.token.Tags.BAR_RBRACK, FSharpTokenType.BAR_RBRACK},

        {Parser.token.Tags.WHITESPACE, FSharpTokenType.WHITESPACE},
        {Parser.token.Tags.INACTIVECODE, FSharpTokenType.DEAD_CODE},
        {Parser.token.Tags.COMMENT, FSharpTokenType.COMMENT},
        {Parser.token.Tags.LINE_COMMENT, FSharpTokenType.LINE_COMMENT},
        {Parser.token.Tags.BYTEARRAY, FSharpTokenType.BYTEARRAY},

        {Parser.token.Tags.IDENT, FSharpTokenType.IDENTIFIER},
        {Parser.token.Tags.UNDERSCORE, FSharpTokenType.IDENTIFIER},

        {Parser.token.Tags.BAR, FSharpTokenType.BAR},
        {Parser.token.Tags.DOT, FSharpTokenType.DOT},
        {Parser.token.Tags.LESS, FSharpTokenType.LESS},
        {Parser.token.Tags.GREATER, FSharpTokenType.GREATER},
        {Parser.token.Tags.HASH, FSharpTokenType.HASH},
        {Parser.token.Tags.RARROW, FSharpTokenType.RARROW},
        {Parser.token.Tags.COMMA, FSharpTokenType.COMMA},
        {Parser.token.Tags.SEMICOLON, FSharpTokenType.SEMICOLON},
      };
  }
}