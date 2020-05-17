using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial class ForLikeExprNavigator
  {
    [CanBeNull]
    public static IForLikeExpr GetByInExpression([CanBeNull] IFSharpExpression param) =>
      ForExprNavigator.GetByIdentExpression(param) ??
      (IForLikeExpr) ForExprNavigator.GetByToExpression(param) ??
      ForEachExprNavigator.GetByInExpression(param);
  }
}
