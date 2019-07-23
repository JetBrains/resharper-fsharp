using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LetOrUseExpr
  {
    public bool IsRecursive => RecKeyword != null;
    public bool IsUse => LetOrUseToken?.GetTokenType() == FSharpTokenType.USE;

    public void SetIsRecursive(bool value)
    {
      if (!value)
        throw new System.NotImplementedException();

      using (var _ = WriteLockCookie.Create(IsPhysical()))
        LetOrUseToken.NotNull().AddModifierToken(FSharpTokenType.REC);
    }
  }
}
