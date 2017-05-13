using JetBrains.ReSharper.Psi.Parsing;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Parsing
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

      if (token.ColorClass == FSharpTokenColorKind.InactiveCode) return FSharpTokenType.DEAD_CODE;
      if (token.ColorClass == FSharpTokenColorKind.PreprocessorKeyword) return FSharpTokenType.OTHER_KEYWORD;

      switch (token.CharClass)
      {
        case FSharpTokenCharKind.Keyword:
          switch (token.Tag)
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
          if (token.Tag == FSharpTokenTag.GREATER)
            return FSharpTokenType.GREATER;
          if (token.Tag == FSharpTokenTag.LESS)
            return FSharpTokenType.LESS;
          return FSharpTokenType.OPERATOR;

        case FSharpTokenCharKind.LineComment:
          return FSharpTokenType.LINE_COMMENT;
        case FSharpTokenCharKind.Comment:
          return FSharpTokenType.COMMENT;

        case FSharpTokenCharKind.WhiteSpace:
          return FSharpTokenType.WHITESPACE;

        case FSharpTokenCharKind.Delimiter:
          if (token.Tag == FSharpTokenTag.LPAREN)
            return FSharpTokenType.LPAREN;
          if (token.Tag == FSharpTokenTag.RPAREN)
            return FSharpTokenType.RPAREN;
          if (token.Tag == FSharpTokenTag.LBRACE)
            return FSharpTokenType.LBRACE;
          if (token.Tag == FSharpTokenTag.RBRACE)
            return FSharpTokenType.RBRACE;
          if (token.Tag == FSharpTokenTag.LBRACK)
            return FSharpTokenType.LBRACK;
          if (token.Tag == FSharpTokenTag.RBRACK)
            return FSharpTokenType.RBRACK;
          if (token.Tag == FSharpTokenTag.DOT)
            return FSharpTokenType.DOT;
          if (token.Tag == FSharpTokenTag.GREATER_RBRACK)
            return FSharpTokenType.GREATER_RBRACK;
          if (token.Tag == FSharpTokenTag.LBRACK_LESS)
            return FSharpTokenType.LBRACK_LESS;
          if (token.Tag == FSharpTokenTag.LBRACK_BAR)
            return FSharpTokenType.LBRACK_BAR;
          if (token.Tag == FSharpTokenTag.BAR_RBRACK)
            return FSharpTokenType.BAR_RBRACK;
          return FSharpTokenType.TEXT;

        default:
          return FSharpTokenType.TEXT;
      }
    }
  }
}