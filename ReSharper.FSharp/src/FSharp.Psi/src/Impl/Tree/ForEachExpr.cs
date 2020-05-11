using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ForEachExpr
  {
    public bool IsYieldExpression =>
      BodySeparator?.GetTokenType() == FSharpTokenType.RARROW;
  }
}
