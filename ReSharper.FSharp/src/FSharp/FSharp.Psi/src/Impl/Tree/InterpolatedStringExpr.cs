using JetBrains.DocumentModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Injections;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class InterpolatedStringExpr
  {
    public bool IsTrivial()
    {
      var tokenType = Literals.SingleItem?.GetTokenType();
      return tokenType == FSharpTokenType.REGULAR_INTERPOLATED_STRING ||
             tokenType == FSharpTokenType.VERBATIM_INTERPOLATED_STRING ||
             tokenType == FSharpTokenType.TRIPLE_QUOTE_INTERPOLATED_STRING ||
             tokenType == FSharpTokenType.RAW_INTERPOLATED_STRING;
    }

    public DocumentRange GetDollarSignRange()
    {
      var literal = Literals.FirstOrDefault();
      if (literal == null)
        return DocumentRange.InvalidRange;

      var startOffset = literal.GetDocumentStartOffset();

      var text = literal.GetText();
      return text[0] == '$'
        ? startOffset.ExtendRight(+1)
        : startOffset.Shift(+1).ExtendRight(+1);
    }

    public int DollarCount
    {
      get
      {
        var child = FirstChild;
        if (child == null)
          return 1;

        var tokenType = child.GetTokenType();
        if (tokenType == FSharpTokenType.RAW_INTERPOLATED_STRING ||
            tokenType == FSharpTokenType.RAW_INTERPOLATED_STRING_START)
        {
          var text = child.GetText();
          var dollarCount = 0;
          while (text[dollarCount] == '$')
            dollarCount++;

          return dollarCount;
        }

        return 1;
      }
    }

    bool IInjectionHostNode.IsValidHost => true;
  }
}
