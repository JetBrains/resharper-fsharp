using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LetOrUseBangExpr
  {
    public bool IsUse => LetOrUseToken?.GetTokenType() == FSharpTokenType.USE_BANG;
  }
}
