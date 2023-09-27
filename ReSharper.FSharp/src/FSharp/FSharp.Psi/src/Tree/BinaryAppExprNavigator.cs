using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial class BinaryAppExprNavigator
  {
    [CanBeNull]
    public static IBinaryAppExpr GetByArgument([CanBeNull] IFSharpExpression param) =>
      GetByLeftArgument(param) ??
      GetByRightArgument(param);
  }
}
