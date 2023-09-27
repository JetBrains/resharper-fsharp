using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial class ConditionOwnerExprNavigator
  {
    [CanBeNull]
    public static IConditionOwnerExpr GetByExpr([CanBeNull] IFSharpExpression param) =>
      (IConditionOwnerExpr) IfThenElseExprNavigator.GetByExpression(param) ??
      WhileExprNavigator.GetByExpression(param);
  }
}
