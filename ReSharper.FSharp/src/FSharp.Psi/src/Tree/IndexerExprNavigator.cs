using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial class IndexerExprNavigator
  {
    [CanBeNull]
    public static IIndexerExpr GetByExpression([CanBeNull] ISynExpr param) =>
      (IIndexerExpr) ItemIndexerExprNavigator.GetByExpression(param) ??
      NamedIndexerExprNavigator.GetByExpression(param as IReferenceExpr);
  }
}
