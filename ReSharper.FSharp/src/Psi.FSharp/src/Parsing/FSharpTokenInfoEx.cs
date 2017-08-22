using JetBrains.ReSharper.Psi.Parsing;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing
{
  public class FSharpTokenInfoEx
  {
    public static TokenNodeType GetTokenType(FSharpTokenInfo token)
    {
      // todo: add this to FCS
      const int PublicTag = 42;
      const int PriateTag = 43;
      const int InternalTag = 44;
      const int NewTag = 118;
      const int ModuleTag = 148;
      const int NamespaceTag = 149;
      const int LQuoteTag = 153;
      const int RQuoteTag = 154;

      if (token.ColorClass == FSharpTokenColorKind.InactiveCode) return FSharpTokenType.DEAD_CODE;
      if (token.ColorClass == FSharpTokenColorKind.PreprocessorKeyword) return FSharpTokenType.OTHER_KEYWORD;
      var tokenTag = token.Tag;
      switch (token.CharClass)
      {
        case FSharpTokenCharKind.Keyword:
          switch (tokenTag)
          {
            case PublicTag:
              return FSharpTokenType.PUBLIC;
            case PriateTag:
              return FSharpTokenType.PRIVATE;
            case InternalTag:
              return FSharpTokenType.INTERNAL;
            case NewTag:
              return FSharpTokenType.NEW;
            case ModuleTag:
              return FSharpTokenType.MODULE;
            case NamespaceTag:
              return FSharpTokenType.NAMESPACE;
            default:
              return FSharpTokenType.OTHER_KEYWORD;
          }

        case FSharpTokenCharKind.Identifier:
          return FSharpTokenType.IDENTIFIER;

        case FSharpTokenCharKind.String:
          return FSharpTokenType.STRING;

        case FSharpTokenCharKind.Literal:
          return FSharpTokenType.LITERAL;

        case FSharpTokenCharKind.Operator:
          if (tokenTag == FSharpTokenTag.GREATER)
            return FSharpTokenType.GREATER;
          if (tokenTag == FSharpTokenTag.LESS)
            return FSharpTokenType.LESS;
          return FSharpTokenType.OPERATOR;

        case FSharpTokenCharKind.LineComment:
          return FSharpTokenType.LINE_COMMENT;
        case FSharpTokenCharKind.Comment:
          return FSharpTokenType.COMMENT;

        case FSharpTokenCharKind.WhiteSpace:
          return FSharpTokenType.WHITESPACE;

        case FSharpTokenCharKind.Delimiter:
          if (tokenTag == FSharpTokenTag.LPAREN)
            return FSharpTokenType.LPAREN;
          if (tokenTag == FSharpTokenTag.RPAREN)
            return FSharpTokenType.RPAREN;
          if (tokenTag == FSharpTokenTag.LBRACE)
            return FSharpTokenType.LBRACE;
          if (tokenTag == FSharpTokenTag.RBRACE)
            return FSharpTokenType.RBRACE;
          if (tokenTag == FSharpTokenTag.LBRACK)
            return FSharpTokenType.LBRACK;
          if (tokenTag == FSharpTokenTag.RBRACK)
            return FSharpTokenType.RBRACK;
          if (tokenTag == FSharpTokenTag.DOT)
            return FSharpTokenType.DOT;
          if (tokenTag == FSharpTokenTag.GREATER_RBRACK)
            return FSharpTokenType.GREATER_RBRACK;
          if (tokenTag == FSharpTokenTag.LBRACK_LESS)
            return FSharpTokenType.LBRACK_LESS;
          if (tokenTag == FSharpTokenTag.LBRACK_BAR)
            return FSharpTokenType.LBRACK_BAR;
          if (tokenTag == FSharpTokenTag.BAR_RBRACK)
            return FSharpTokenType.BAR_RBRACK;

          if (tokenTag == LQuoteTag)
            return token.FullMatchedLength == 2
              ? FSharpTokenType.LQUOTE_TYPED
              : FSharpTokenType.LQUOTE;

          if (tokenTag == RQuoteTag)
            return token.FullMatchedLength == 2
              ? FSharpTokenType.RQUOTE_TYPED
              : FSharpTokenType.RQUOTE;

          if (tokenTag == FSharpTokenTag.BAR)
            return FSharpTokenType.BAR;

          return FSharpTokenType.TEXT;

        default:
          return FSharpTokenType.TEXT;
      }
    }
  }
}