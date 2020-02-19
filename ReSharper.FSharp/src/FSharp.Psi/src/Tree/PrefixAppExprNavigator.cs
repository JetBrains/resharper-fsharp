using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial class PrefixAppExprNavigator
  {
    [CanBeNull]
    public static IPrefixAppExpr GetByExpression([CanBeNull] ISynExpr param) =>
      GetByFunctionExpression(param) ??
      GetByArgumentExpression(param);
  }
}
