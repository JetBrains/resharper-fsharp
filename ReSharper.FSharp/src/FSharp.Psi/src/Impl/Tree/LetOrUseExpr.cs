using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LetOrUseExpr
  {
    public bool IsRecursive => RecKeyword != null;
    public bool IsUse => LetOrUseToken?.GetTokenType() == FSharpTokenType.USE;
  }
}
