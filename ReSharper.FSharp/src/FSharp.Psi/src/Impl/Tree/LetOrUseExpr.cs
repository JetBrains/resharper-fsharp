using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LetOrUseExpr
  {
    public bool IsRecursive => RecKeyword != null;

    public bool IsUse => 
      BindingKeyword?.GetTokenType() is var tokenType && 
      (tokenType == FSharpTokenType.USE || tokenType == FSharpTokenType.USE_BANG);

    public void SetIsRecursive(bool value)
    {
      if (!value)
        throw new System.NotImplementedException();

      BindingKeyword.NotNull().AddTokenAfter(FSharpTokenType.REC);
    }
  }
}
