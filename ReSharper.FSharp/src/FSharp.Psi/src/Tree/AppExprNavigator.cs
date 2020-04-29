using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial class AppExprNavigator
  {
    [CanBeNull]
    public static IAppExpr GetByArgument([CanBeNull] ISynExpr param) =>
      (IAppExpr) BinaryAppExprNavigator.GetByArgument(param) ??
      PrefixAppExprNavigator.GetByArgumentExpression(param);

    [CanBeNull]
    public static IAppExpr GetByRightArgument([CanBeNull] ISynExpr param) =>
      (IAppExpr) BinaryAppExprNavigator.GetByRightArgument(param) ??
      PrefixAppExprNavigator.GetByArgumentExpression(param);
  }
}
