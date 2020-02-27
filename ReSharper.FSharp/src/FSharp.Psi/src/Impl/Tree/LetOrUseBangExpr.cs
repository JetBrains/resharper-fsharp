using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LetOrUseBangExpr
  {
    public bool IsUse => LetOrUseToken?.GetTokenType() == FSharpTokenType.USE_BANG;
  }
}
