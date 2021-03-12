using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  public static class FSharpMethodInvocationUtil
  {
    [CanBeNull]
    public static IFSharpArgumentsOwner GetArgumentsOwner([CanBeNull] this IFSharpExpression expr)
    {
      var tupleExpr = TupleExprNavigator.GetByExpression(expr.IgnoreParentParens());
      var exprContext = tupleExpr ?? expr;
      return FSharpArgumentOwnerNavigator.GetByArgumentExpression(exprContext.IgnoreParentParens());
    }

    public static bool CanBeArgument([CanBeNull] this IFSharpExpression expr) =>
      expr.GetArgumentsOwner() != null;
  }
}
