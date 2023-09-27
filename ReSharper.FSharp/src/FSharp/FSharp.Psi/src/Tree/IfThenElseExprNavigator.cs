using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial class IfThenElseExprNavigator
  {
    [CanBeNull]
    public static IIfThenElseExpr GetByExpression([CanBeNull] IFSharpExpression param) =>
      GetByConditionExpr(param) ?? GetByBranchExpression(param);

    [CanBeNull]
    public static IIfThenElseExpr GetByBranchExpression([CanBeNull] IFSharpExpression param) =>
      GetByThenExpr(param) ?? GetByElseExpr(param);
  }
  
  public partial class IfExprNavigator
  {
    [CanBeNull]
    public static IIfExpr GetByBranchExpression([CanBeNull] IFSharpExpression param) =>
      GetByThenExpr(param) ?? GetByElseExpr(param);
  }
}
