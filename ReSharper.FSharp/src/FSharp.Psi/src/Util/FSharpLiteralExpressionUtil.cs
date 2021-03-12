using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  public static class FSharpLiteralExpressionUtil
  {
    public static bool IsStringLiteralExpression([NotNull] this ILiteralExpr literalExpr)
    {
      var tokenType = literalExpr.Literal?.GetTokenType();
      return tokenType != null && tokenType.IsStringLiteral;
    }
  }
}
