using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial class AppExprNavigator
  {
    [CanBeNull]
    public static IAppExpr GetByArgument([CanBeNull] IFSharpExpression param) =>
      (IAppExpr) BinaryAppExprNavigator.GetByArgument(param) ??
      PrefixAppExprNavigator.GetByArgumentExpression(param);

    [CanBeNull]
    public static IAppExpr GetByRightArgument([CanBeNull] IFSharpExpression param) =>
      (IAppExpr) BinaryAppExprNavigator.GetByRightArgument(param) ??
      PrefixAppExprNavigator.GetByArgumentExpression(param);
  }
}
