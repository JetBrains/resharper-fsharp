using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial class IndexerExprNavigator
  {
    [CanBeNull]
    public static IIndexerExpr GetByQualifierIgnoreIndexers([CanBeNull] IFSharpExpression param) =>
      GetByQualifier(param) is { } indexer
        ? GetByQualifierIgnoreIndexers(indexer) ?? indexer
        : null;
  }
}
