using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LetOrUseExpr
  {
    public bool IsRecursive => RecKeyword != null;

    public bool IsUse => 
      LetOrUseToken?.GetTokenType() is var tokenType && 
      (tokenType == FSharpTokenType.USE || tokenType == FSharpTokenType.USE_BANG);

    public void SetIsRecursive(bool value)
    {
      if (!value)
        throw new System.NotImplementedException();

      LetOrUseToken.NotNull().AddTokenAfter(FSharpTokenType.REC);
    }
  }
}
