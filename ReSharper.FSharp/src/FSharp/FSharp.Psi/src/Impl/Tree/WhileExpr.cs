using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class WhileExpr
  {
    public bool IsComputed => WhileKeyword?.GetTokenType() == FSharpTokenType.WHILE_BANG;
  }
}
