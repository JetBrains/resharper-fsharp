using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class YieldOrReturnExpr
  {
    public bool IsComputed
    {
      get
      {
        var tokenType = YieldKeyword?.GetTokenType();
        return tokenType == FSharpTokenType.YIELD_BANG || tokenType == FSharpTokenType.RETURN_BANG;
      }

    }
  }
}
