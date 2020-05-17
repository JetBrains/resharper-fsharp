using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial class WhileExprNavigator
  {
    [CanBeNull]
    public static IWhileExpr GetByExpression([CanBeNull] IFSharpExpression param) =>
      GetByConditionExpr(param) ?? GetByDoExpression(param);
  }
}
