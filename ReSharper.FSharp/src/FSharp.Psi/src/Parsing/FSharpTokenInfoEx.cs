using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Parsing;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
{
  public class FSharpTokenInfoEx
  {
    private const int PublicTag = 42;
    private const int PrivateTag = 43;
    private const int InternalTag = 44;
    private const int HashTag = 88;
    private const int OpenTag = 101;
    private const int TypeTag = 108;
    private const int NewTag = 118;
    private const int ModuleTag = 148;
    private const int NamespaceTag = 149;
    private const int LQuoteTag = 153;
    private const int RQuoteTag = 154;

    private static readonly Dictionary<int, TokenNodeType> TokenTypeByTokenTag =
      new Dictionary<int, TokenNodeType>
      {
        {PublicTag, FSharpTokenType.PUBLIC},
        {PrivateTag, FSharpTokenType.PRIVATE},
        {InternalTag, FSharpTokenType.INTERNAL},
        {HashTag, FSharpTokenType.HASH},
        {OpenTag, FSharpTokenType.OPEN},
        {TypeTag, FSharpTokenType.TYPE},
        {NewTag, FSharpTokenType.NEW},
        {ModuleTag, FSharpTokenType.MODULE},
        {NamespaceTag, FSharpTokenType.NAMESPACE},
        {LQuoteTag, FSharpTokenType.LQUOTE},
        {RQuoteTag, FSharpTokenType.RQUOTE},

        {FSharpTokenTag.LPAREN, FSharpTokenType.LPAREN},
        {FSharpTokenTag.RPAREN, FSharpTokenType.RPAREN},
        {FSharpTokenTag.LBRACE, FSharpTokenType.LBRACE},
        {FSharpTokenTag.RBRACE, FSharpTokenType.RBRACE},
        {FSharpTokenTag.LBRACK, FSharpTokenType.LBRACK},
        {FSharpTokenTag.RBRACK, FSharpTokenType.RBRACK},
        {FSharpTokenTag.DOT, FSharpTokenType.DOT},
        {FSharpTokenTag.GREATER_RBRACK, FSharpTokenType.GREATER_RBRACK},
        {FSharpTokenTag.LBRACK_LESS, FSharpTokenType.LBRACK_LESS},
        {FSharpTokenTag.LBRACK_BAR, FSharpTokenType.LBRACK_BAR},
        {FSharpTokenTag.BAR_RBRACK, FSharpTokenType.BAR_RBRACK},

        {FSharpTokenTag.GREATER,FSharpTokenType.GREATER},
        {FSharpTokenTag.LESS,FSharpTokenType.LESS},
        {FSharpTokenTag.BAR, FSharpTokenType.BAR},
      };
    
    
    public static TokenNodeType GetTokenType(FSharpTokenInfo token)
    {
      // todo: add this to FCS
      if (token.ColorClass == FSharpTokenColorKind.InactiveCode) return FSharpTokenType.DEAD_CODE;
      if (token.ColorClass == FSharpTokenColorKind.PreprocessorKeyword) return FSharpTokenType.OTHER_KEYWORD;
      var tokenTag = token.Tag;
      if (TokenTypeByTokenTag.TryGetValue(tokenTag, out var tokenType))
        return tokenType;
      
      switch (token.CharClass)
      {
        case FSharpTokenCharKind.Keyword:
          return FSharpTokenType.OTHER_KEYWORD;

        case FSharpTokenCharKind.Identifier:
          return FSharpTokenType.IDENTIFIER;

        case FSharpTokenCharKind.String:
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
              : FSharpTokenType.LQUOTE;

          if (tokenTag == RQuoteTag)
            return token.FullMatchedLength == 2
              ? FSharpTokenType.RQUOTE_TYPED
              : FSharpTokenType.RQUOTE;

          return FSharpTokenType.TEXT;

        default:
          return FSharpTokenType.TEXT;
      }
    }
  }
}