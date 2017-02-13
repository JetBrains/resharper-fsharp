using JetBrains.ReSharper.Feature.Services.Comment;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.FSharp;
using JetBrains.ReSharper.Psi.FSharp.Parsing;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Feature.Services.FSharp.Comment
{
  [Language(typeof(FSharpLanguage))]
  public class FSharpLineCommentActionProvider : SimpleLineCommentActionProvider
  {
    public override string StartLineCommentMarker => "//";

    protected override bool IsNewLine(TokenNodeType tokenType)
    {
      return tokenType == FSharpTokenType.NEW_LINE;
    }

    protected override bool IsEndOfLineComment(TokenNodeType tokenType, string tokenText)
    {
      if (tokenType != FSharpTokenType.COMMENT)
        return false;

      // exactly 2 slashes
      if (tokenText.Length == 2 || tokenText[2] != '/')
        return true;

      // comment with 4 or more slashes
      if (tokenText.Length > 3 && tokenText[3] == '/')
        return true;

      // doc comment
      return false;
    }

    protected override bool IsWhitespace(TokenNodeType tokenType)
    {
      return tokenType == FSharpTokenType.WHITESPACE;
    }
  }
}