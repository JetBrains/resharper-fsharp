using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial class PrefixAppExprNavigator
  {
    [CanBeNull]
    public static IPrefixAppExpr GetByExpression([CanBeNull] IFSharpExpression param) => // todo: rename: get by any expression
      GetByFunctionExpression(param) ??
      GetByArgumentExpression(param);
  }
}
