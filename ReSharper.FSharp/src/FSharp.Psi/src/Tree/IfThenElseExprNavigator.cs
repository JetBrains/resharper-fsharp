using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial class IfThenElseExprNavigator
  {
    [CanBeNull]
    public static IIfThenElseExpr GetByExpression([CanBeNull] ISynExpr param) =>
      GetByConditionExpr(param) ?? GetByBranchExpression(param);

    [CanBeNull]
    public static IIfThenElseExpr GetByBranchExpression([CanBeNull] ISynExpr param) =>
      GetByThenExpr(param) ?? GetByElseExpr(param);
  }
}
